using System;
using System.IO;
using System.Threading;
using BlockSignatureCalculator.Common;
using BlockSignatureCalculator.Readers;
using BlockSignatureCalculator.Workers;
using BlockSignatureCalculator.Writers;

namespace BlockSignatureCalculator
{
    public class Synchronizer
    {
        private readonly Value[] results;
        private readonly Value[] sources;
        private readonly int maxThreads;

        private readonly IReader reader;
        private readonly IWriter writer;
        private readonly IWorker worker;

        private long sourceBlocksProduced;
        private long sourceBlocksConsumed;
        private long resultBlocksConsumed;

        private bool dataIsOver;
        private bool exceptionWasHappen;

        public Synchronizer(int maxThreads, IReader reader, IWriter writer, IWorker worker)
        {
            this.maxThreads = maxThreads;
            this.reader = reader;
            this.writer = writer;
            this.worker = worker;

            results = new Value[maxThreads];
            sources = new Value[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                results[i] = new Value();
                sources[i] = new Value();
            }

            reader.DataIsOver += (_, _) => dataIsOver = true;
        }

        public bool Run(Options options)
        {
            Thread readerThread = new Thread(() => SafeIoWorker(() => reader.ReadSource(ProduceSource), reader, options)) {Name = "SourceReader"};
            readerThread.Start();

            Thread[] threads = new Thread[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                var threadId = i;
                threads[i] = new Thread(() => SafeProcessWorker(() => worker.DoWork(threadId, ConsumeSource, ProduceResult))) {Name = $"Worker #{threadId}"};
                threads[i].Start();
            }

            Thread writerThread = new Thread(() => SafeIoWorker(() => writer.WriteResult(ConsumeResult), writer, options)) {Name = "ResultWriter"};
            writerThread.Start();

            readerThread.Join();
            writerThread.Join();

            PulseAll(sources);

            for (int i = 0; i < maxThreads; i++)
            {
                threads[i].Join();
            }

            return exceptionWasHappen;
        }

        // notify all waiting threads to recheck conditions of exit
        private static void PulseAll(Value[] values)
        {
            foreach (var value in values)
            {
                lock (value)
                {
                    Monitor.Pulse(value);
                }
            }
        }

        private void SafeIoWorker(Action workerMethod, object ioWorker, Options options)
        {
            try
            {
                workerMethod();
            }
            catch (UnauthorizedAccessException e)
            {
                string message = ioWorker switch
                                 {
                                     IReader => $"Can not read '{options.SourceFile}': {e.Message}",
                                     IWriter => $"Can not write to console: {e.Message}",
                                     _ => e.Message
                                 };

                ProcessException(FormatExceptionMessage(e, message));
            }
            catch (CustomException e)
            {
                ProcessException(FormatExceptionMessage(e, $"{e.Message} The file may have been changed."));
            }
            catch (Exception e)
            {
                ProcessException(FormatExceptionMessage(e));
            }
        }

        private void SafeProcessWorker(Action workerMethod)
        {
            try
            {
                workerMethod();
            }
            catch (InvalidDataException e)
            {
                ProcessException(FormatExceptionMessage(e, $"Error while decompress data: {e.Message}. Archive may have been corrupted."));
            }
            catch (Exception e)
            {
                ProcessException(FormatExceptionMessage(e));
            }
        }

        private string FormatExceptionMessage(Exception ex, string customMessage = null)
            => $"{ex.GetType().Name}: {customMessage ?? ex.Message}{Environment.NewLine}{ex.StackTrace}";

        private void ProcessException(string message)
        {
            reader.StopByForce();
            worker.StopByForce();
            writer.StopByForce();
            Console.WriteLine(message);
            exceptionWasHappen = true;
            PulseAll(sources);
            PulseAll(results);
        }

        private void ProduceSource(int id, byte[] block)
        {
            lock (sources[id])
            {
                while (sources[id].IsReady && !exceptionWasHappen)
                {
                    Monitor.Wait(sources[id]);
                }

                if (exceptionWasHappen) return;

                sources[id].IsReady = true;
                sources[id].Val = block;

                if (!dataIsOver) Interlocked.Increment(ref sourceBlocksProduced);

                Monitor.Pulse(sources[id]);
            }
        }

        private void ProduceResult(int id, byte[] block)
        {
            lock (results[id])
            {
                while (results[id].IsReady && !exceptionWasHappen)
                {
                    Monitor.Wait(results[id]);
                }

                if (exceptionWasHappen) return;

                results[id].IsReady = true;
                results[id].Val = block;

                Monitor.Pulse(results[id]);
            }
        }

        private byte[] ConsumeSource(int id)
        {
            lock (sources[id])
            {
                while (!sources[id].IsReady && (!dataIsOver || Interlocked.Read(ref sourceBlocksProduced) > Interlocked.Read(ref sourceBlocksConsumed)) && !exceptionWasHappen)
                {
                    Monitor.Wait(sources[id]);
                }

                if (exceptionWasHappen || dataIsOver && Interlocked.Read(ref sourceBlocksProduced) == Interlocked.Read(ref sourceBlocksConsumed)) return null;

                byte[] block = sources[id].Val;

                sources[id].IsReady = false;
                sources[id].Val = null;

                Interlocked.Increment(ref sourceBlocksConsumed);

                Monitor.Pulse(sources[id]);

                return block;
            }
        }

        private byte[] ConsumeResult(int id)
        {
            lock (results[id])
            {
                while (!results[id].IsReady && (!dataIsOver || Interlocked.Read(ref sourceBlocksProduced) > Interlocked.Read(ref resultBlocksConsumed)) && !exceptionWasHappen)
                {
                    Monitor.Wait(results[id]);
                }

                if (exceptionWasHappen || dataIsOver && Interlocked.Read(ref sourceBlocksProduced) == Interlocked.Read(ref resultBlocksConsumed)) return null;

                byte[] block = results[id].Val;

                results[id].IsReady = false;
                results[id].Val = null;

                Interlocked.Increment(ref resultBlocksConsumed);

                Monitor.Pulse(results[id]);

                return block;
            }
        }
    }
}
