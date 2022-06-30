using System;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Workers
{
    public interface IWorker : IStoppableByForce
    {
        void DoWork(int threadId, Func<int, byte[]> consumeSource, Action<int, byte[]> produceResult);
    }
}
