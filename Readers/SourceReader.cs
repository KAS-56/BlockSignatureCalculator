using System;
using System.IO;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Readers
{
    public class SourceReader : StoppableByForceBase, IReader
    {
        private readonly int blockSize;
        private readonly string sourceFile;
        private readonly int maxThreads;

        public event EventHandler DataIsOver;

        public SourceReader(int blockSize, string sourceFile, int maxThreads)
        {
            this.blockSize = blockSize;
            this.sourceFile = sourceFile;
            this.maxThreads = maxThreads;
        }

        public void ReadSource(Action<int, byte[]> produceSource)
        {
            int i = 0;
            using FileStream input = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (input.Length == 0) throw new CustomException("Source file is empty.");

            while (!ForcedStop && input.Position < input.Length)
            {
                byte[] sourceBlock = ReadSourceBlock(input);

                produceSource(i, sourceBlock);

                i++;
                i = i == maxThreads ? 0 : i;
            }

            DataIsOver?.Invoke(null, EventArgs.Empty);
        }

        private byte[] ReadSourceBlock(Stream input)
        {
            byte[] block = new byte[blockSize];
            int readBytes = input.Read(block, 0, blockSize);

            if (readBytes != blockSize && input.Position != input.Length) throw new CustomException("Unexpected count of reading bytes.");

            if (readBytes != blockSize)
            {
                Array.Resize(ref block, readBytes);
            }

            return block;
        }
    }
}
