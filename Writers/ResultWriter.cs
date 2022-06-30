using System;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Writers
{
    public class ResultWriter : StoppableByForceBase, IWriter
    {
        private readonly int maxThreads;

        public ResultWriter(int maxThreads)
        {
            this.maxThreads = maxThreads;
        }

        public void WriteResult(Func<int, byte[]> consumeResult)
        {
            int i = 0;
            long blockNumber = 0;
            while (!ForcedStop)
            {
                byte[] block = consumeResult(i);

                if (block == null) break;

                Console.WriteLine($"{++blockNumber} - {BitConverter.ToString(block).Replace("-", "")}");

                i++;
                i = i == maxThreads ? 0 : i;
            }
        }
    }
}
