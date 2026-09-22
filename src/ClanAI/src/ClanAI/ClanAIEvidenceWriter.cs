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

        public static void AppendLine(
            string path,
            string line)
        {
            if (string.IsNullOrEmpty(path))
                return;

            EnsureStarted();

            var item =
                new Item
                {
                    Path = path,
                    Text =
                        (line ?? string.Empty) +
                        Environment.NewLine
                };

            try
            {
                if (!_stopping &&
                    Queue.TryAdd(item))
                {
                    return;
                }
            }
            catch
            {
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
            }

            Thread t = _thread;

            if (t != null &&
                t.IsAlive)
            {
                try
                {
                    t.Join(2500);
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
                    new Thread(WriterLoop);

                _thread.IsBackground = true;
                _thread.Name =
                    "ClanAI.EvidenceWriter";
                _thread.Priority =
                    ThreadPriority.BelowNormal;

                _started = true;
                _thread.Start();
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

                                sinceFlush++;
                            }
                            catch
                            {
                                CloseWriter(
                                    writers,
                                    item.Path);

                                AppendSync(item);
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
                    }
                    catch
                    {
                        AppendSync(tail);
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
                }
                catch
                {
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
            }
            catch
            {
            }
        }
    }
}
