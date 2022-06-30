using System;
using System.Security.Cryptography;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Workers
{
    public class Worker : StoppableByForceBase, IWorker
    {
        public void DoWork(int threadId, Func<int, byte[]> consumeSource, Action<int, byte[]> produceResult)
        {
            while (!ForcedStop)
            {
                byte[] block = consumeSource(threadId);

                if (block == null) break;

                byte[] processedBlock = ProcessBlock(block);

                produceResult(threadId, processedBlock);
            }
        }

        private static byte[] ProcessBlock(byte[] block)
        {
            using SHA256 hasher = SHA256.Create();
            return hasher.ComputeHash(block);
        }
    }
}
