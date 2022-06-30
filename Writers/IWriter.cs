using System;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Writers
{
    public interface IWriter : IStoppableByForce
    {
        void WriteResult(Func<int, byte[]> consumeResult);
    }
}
