using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ClanAI
{
    internal static class ClanAIEvidenceWriter
    {
        private const int Capacity = 12000;
        private const int FlushEveryItems = 96;
        private const int FlushEveryMs = 250;

        private sealed class Item
        {
            public string Path;
            public string Text;
        }

        private static readonly object StartSync =
            new object();

        private static readonly BlockingCollection<Item>
            Queue =
                new BlockingCollection<Item>(
                    new ConcurrentQueue<Item>(),
                    Capacity);

        private static readonly Encoding Utf8 =
            new UTF8Encoding(false);

        private static Thread _thread;
        private static volatile bool _started;
        private static volatile bool _stopping;

        // Successful write calls do not imply fsync or exactly-once delivery.
        // Counters have process-lifetime scope; failure categories can overlap.
        private static long _requests, _queued, _written, _syncWritten;
        private static long _syncFallbacks, _failures, _queueRejected;
        private static long _openFailures, _flushFailures, _disposeFailures;
        private static long _terminalWriteFailures, _queueFailures;
        private static long _workerFailures, _startFailures, _shutdownTimeouts;
        private static long _successfulFlushes;

        public static string HealthSummary()
        {
            bool alive = _thread != null && _thread.IsAlive;
            int pending = Queue.Count;
            return "CLANAI_WRITER_HEALTH scope=process_lifetime"
                + " requests=" + Interlocked.Read(ref _requests)
                + " queued=" + Interlocked.Read(ref _queued)
                + " writtenCalls=" + Interlocked.Read(ref _written)
                + " syncWritten=" + Interlocked.Read(ref _syncWritten)
                + " syncFallbacks=" + Interlocked.Read(ref _syncFallbacks)
                + " queueRejected=" + Interlocked.Read(ref _queueRejected)
                + " failures=" + Interlocked.Read(ref _failures)
                + " openFailures=" + Interlocked.Read(ref _openFailures)
                + " flushFailures=" + Interlocked.Read(ref _flushFailures)
                + " disposeFailures=" + Interlocked.Read(ref _disposeFailures)
                + " terminalWriteFailures=" + Interlocked.Read(ref _terminalWriteFailures)
                + " queueFailures=" + Interlocked.Read(ref _queueFailures)
                + " workerFailures=" + Interlocked.Read(ref _workerFailures)
                + " startFailures=" + Interlocked.Read(ref _startFailures)
                + " shutdownTimeouts=" + Interlocked.Read(ref _shutdownTimeouts)
                + " successfulFlushes=" + Interlocked.Read(ref _successfulFlushes)
                + " pending=" + pending + " threadAlive=" + alive
                + " stopping=" + _stopping
                + " drainCompleted=" + (_stopping && !alive && pending == 0);
        }

        public static void AppendLine(
            string path,
            string line)
        {
            if (string.IsNullOrEmpty(path)) return;
            Interlocked.Increment(ref _requests);
            var item = new Item
            {
                Path = path,
                Text = (line ?? string.Empty) + Environment.NewLine
            };
            try
            {
                EnsureStarted();
                if (!_stopping && _thread != null && _thread.IsAlive && Queue.TryAdd(item))
                {
                    Interlocked.Increment(ref _queued);
                    return;
                }
                Interlocked.Increment(ref _queueRejected);
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _queueFailures);
            }
            AppendSync(item);
        }

        public static void Shutdown()
        {
            if (!_started)
                return;

            _stopping = true;

            try
            {
                Queue.CompleteAdding();
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _queueFailures);
            }

            Thread t = _thread;

            if (t != null &&
                t.IsAlive)
            {
                try
                {
                    if (!t.Join(2500)) Interlocked.Increment(ref _shutdownTimeouts);
                }
                catch
                {
                    Interlocked.Increment(ref _failures);
                    Interlocked.Increment(ref _queueFailures);
                }
            }
        }

        private static void EnsureStarted()
        {
            if (_started)
                return;

            lock (StartSync)
            {
                if (_started)
                    return;

                _thread =
                    new Thread(WriterLoop);

                _thread.IsBackground = true;
                _thread.Name =
                    "ClanAI.EvidenceWriter";
                _thread.Priority =
                    ThreadPriority.BelowNormal;

                try { _thread.Start(); _started = true; }
                catch
                {
                    _started = false;
                    _thread = null;
                    Interlocked.Increment(ref _startFailures);
                    throw; // AppendLine catches this and uses the synchronous fallback.
                }
            }
        }

        private static void WriterLoop()
        {
            var writers =
                new Dictionary<string, StreamWriter>(
                    StringComparer.OrdinalIgnoreCase);

            int sinceFlush = 0;
            DateTime nextFlush =
                DateTime.UtcNow.AddMilliseconds(
                    FlushEveryMs);

            try
            {
                while (!Queue.IsCompleted)
                {
                    Item item = null;

                    try
                    {
                        Queue.TryTake(
                            out item,
                            50);
                    }
                    catch
                    {
                        Interlocked.Increment(ref _failures);
                        Interlocked.Increment(ref _workerFailures);
                    }

                    if (item != null)
                    {
                        StreamWriter writer =
                            GetWriter(
                                writers,
                                item.Path);

                        if (writer != null)
                        {
                            try
                            {
                                writer.Write(
                                    item.Text);

                                Interlocked.Increment(ref _written);
                                sinceFlush++;
                            }
                            catch
                            {
                                Interlocked.Increment(ref _failures);
                                Interlocked.Increment(ref _workerFailures);
                                CloseWriter(
                                    writers,
                                    item.Path);

                                AppendSync(item);
                            }
                        }
                        else
                        {
                            AppendSync(item);
                        }
                    }

                    DateTime now =
                        DateTime.UtcNow;

                    if (sinceFlush >=
                            FlushEveryItems ||
                        now >= nextFlush)
                    {
                        FlushAll(writers);

                        sinceFlush = 0;
                        nextFlush =
                            now.AddMilliseconds(
                                FlushEveryMs);
                    }
                }

                Item tail;

                while (Queue.TryTake(
                    out tail))
                {
                    StreamWriter writer =
                        GetWriter(
                            writers,
                            tail.Path);

                    if (writer == null)
                    {
                        AppendSync(tail);
                        continue;
                    }

                    try
                    {
                        writer.Write(
                            tail.Text);
                        Interlocked.Increment(ref _written);
                    }
                    catch
                    {
                        Interlocked.Increment(ref _failures);
                        Interlocked.Increment(ref _workerFailures);
                        AppendSync(tail);
                    }
                }
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _workerFailures);
            }
            finally
            {
                FlushAll(writers);

                foreach (
                    StreamWriter writer
                    in writers.Values)
                {
                    try
                    {
                        writer.Dispose();
                    }
                    catch
                    {
                        Interlocked.Increment(ref _failures);
                        Interlocked.Increment(ref _workerFailures);
                    }
                }
            }
        }

        private static StreamWriter GetWriter(
            Dictionary<string, StreamWriter> writers,
            string path)
        {
            StreamWriter writer;

            if (writers.TryGetValue(
                    path,
                    out writer))
            {
                return writer;
            }

            try
            {
                string directory =
                    Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                var stream =
                    new FileStream(
                        path,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        32 * 1024,
                        FileOptions.SequentialScan);

                writer =
                    new StreamWriter(
                        stream,
                        Utf8,
                        32 * 1024);

                writers[path] = writer;

                return writer;
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _openFailures);
                return null;
            }
        }

        private static void FlushAll(
            Dictionary<string, StreamWriter> writers)
        {
            foreach (
                StreamWriter writer
                in writers.Values)
            {
                try
                {
                    writer.Flush();
                    Interlocked.Increment(ref _successfulFlushes);
                }
                catch
                {
                    Interlocked.Increment(ref _failures);
                    Interlocked.Increment(ref _flushFailures);
                }
            }
        }

        private static void CloseWriter(
            Dictionary<string, StreamWriter> writers,
            string path)
        {
            StreamWriter writer;

            if (!writers.TryGetValue(
                    path,
                    out writer))
            {
                return;
            }

            writers.Remove(path);

            try
            {
                writer.Dispose();
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _disposeFailures);
            }
        }

        private static void AppendSync(
            Item item)
        {
            if (item == null ||
                string.IsNullOrEmpty(
                    item.Path))
            {
                return;
            }

            Interlocked.Increment(ref _syncFallbacks);
            try
            {
                string directory =
                    Path.GetDirectoryName(
                        item.Path);

                if (!string.IsNullOrEmpty(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.AppendAllText(
                    item.Path,
                    item.Text ?? string.Empty,
                    Utf8);
                Interlocked.Increment(ref _written);
                Interlocked.Increment(ref _syncWritten);
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Interlocked.Increment(ref _terminalWriteFailures);
            }
        }
    }
}
