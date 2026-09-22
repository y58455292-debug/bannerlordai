using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BannerlordInspector
{
    /// <summary>
    /// Bounded asynchronous evidence sink.
    ///
    /// The campaign thread captures already-materialized strings and enqueues them.
    /// A single background writer owns persistent StreamWriters and performs disk I/O.
    /// This prevents diagnostic file open/write/close latency from stalling campaign AI.
    ///
    /// Safety:
    /// - no TaleWorlds objects cross threads;
    /// - bounded queue prevents unbounded memory growth;
    /// - full queue falls back to synchronous append rather than dropping evidence;
    /// - normal inspector shutdown drains and flushes the queue.
    /// </summary>
    public static class EvidenceWriter
    {
        private const int Capacity = 20000;
        private const int FlushEveryItems = 128;
        private const int FlushEveryMs = 250;

        private sealed class WriteItem
        {
            public string Path;
            public string Text;
        }

        private static readonly object StartSync =
            new object();

        private static readonly BlockingCollection<WriteItem>
            Queue =
                new BlockingCollection<WriteItem>(
                    new ConcurrentQueue<WriteItem>(),
                    Capacity);

        private static readonly Encoding Utf8 =
            new UTF8Encoding(false);

        private static Thread _thread;
        private static volatile bool _started;
        private static volatile bool _stopping;

        private static long _queued;
        private static long _written;
        private static long _syncFallbacks;
        private static long _failures;

        public static long Queued
        {
            get { return Interlocked.Read(ref _queued); }
        }

        public static long Written
        {
            get { return Interlocked.Read(ref _written); }
        }

        public static long SyncFallbacks
        {
            get { return Interlocked.Read(ref _syncFallbacks); }
        }

        public static long Failures
        {
            get { return Interlocked.Read(ref _failures); }
        }

        public static void AppendLine(
            string path,
            string line)
        {
            if (string.IsNullOrEmpty(path))
                return;

            Append(
                path,
                (line ?? string.Empty) +
                Environment.NewLine);
        }

        public static void Append(
            string path,
            string text)
        {
            if (string.IsNullOrEmpty(path))
                return;

            EnsureStarted();

            WriteItem item =
                new WriteItem
                {
                    Path = path,
                    Text = text ?? string.Empty
                };

            try
            {
                if (!_stopping &&
                    Queue.TryAdd(item))
                {
                    Interlocked.Increment(
                        ref _queued);

                    return;
                }
            }
            catch
            {
            }

            Interlocked.Increment(
                ref _syncFallbacks);

            AppendSynchronously(item);
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
            }

            Thread thread = _thread;

            if (thread != null &&
                thread.IsAlive)
            {
                try
                {
                    thread.Join(3000);
                }
                catch
                {
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
                    new Thread(WriterLoop)
                    {
                        IsBackground = true,
                        Name =
                            "BannerlordInspector.EvidenceWriter",
                        Priority =
                            ThreadPriority.BelowNormal
                    };

                _started = true;
                _thread.Start();
            }
        }

        private static void WriterLoop()
        {
            var writers =
                new Dictionary<
                    string,
                    StreamWriter>(
                        StringComparer.OrdinalIgnoreCase);

            int sinceFlush = 0;
            DateTime nextFlush =
                DateTime.UtcNow.AddMilliseconds(
                    FlushEveryMs);

            try
            {
                while (!Queue.IsCompleted)
                {
                    WriteItem item = null;

                    try
                    {
                        Queue.TryTake(
                            out item,
                            50);
                    }
                    catch
                    {
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

                                Interlocked.Increment(
                                    ref _written);

                                sinceFlush++;
                            }
                            catch
                            {
                                Interlocked.Increment(
                                    ref _failures);

                                CloseWriter(
                                    writers,
                                    item.Path);

                                AppendSynchronously(
                                    item);
                            }
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

                WriteItem tail;

                while (Queue.TryTake(
                    out tail))
                {
                    StreamWriter writer =
                        GetWriter(
                            writers,
                            tail.Path);

                    if (writer == null)
                    {
                        AppendSynchronously(
                            tail);

                        continue;
                    }

                    try
                    {
                        writer.Write(
                            tail.Text);

                        Interlocked.Increment(
                            ref _written);
                    }
                    catch
                    {
                        Interlocked.Increment(
                            ref _failures);

                        AppendSynchronously(
                            tail);
                    }
                }
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
                    }
                }

                writers.Clear();
            }
        }

        private static StreamWriter GetWriter(
            Dictionary<
                string,
                StreamWriter> writers,
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
                    System.IO.Path
                        .GetDirectoryName(path);

                if (!string.IsNullOrEmpty(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                FileStream stream =
                    new FileStream(
                        path,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        64 * 1024,
                        FileOptions.SequentialScan);

                writer =
                    new StreamWriter(
                        stream,
                        Utf8,
                        64 * 1024);

                writers[path] = writer;

                return writer;
            }
            catch
            {
                Interlocked.Increment(
                    ref _failures);

                return null;
            }
        }

        private static void FlushAll(
            Dictionary<
                string,
                StreamWriter> writers)
        {
            foreach (
                StreamWriter writer
                in writers.Values)
            {
                try
                {
                    writer.Flush();
                }
                catch
                {
                    Interlocked.Increment(
                        ref _failures);
                }
            }
        }

        private static void CloseWriter(
            Dictionary<
                string,
                StreamWriter> writers,
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
            }
        }

        private static void AppendSynchronously(
            WriteItem item)
        {
            if (item == null ||
                string.IsNullOrEmpty(
                    item.Path))
            {
                return;
            }

            try
            {
                string directory =
                    System.IO.Path
                        .GetDirectoryName(
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

                Interlocked.Increment(
                    ref _written);
            }
            catch
            {
                Interlocked.Increment(
                    ref _failures);
            }
        }
    }
}
