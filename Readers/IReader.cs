using System;
using BlockSignatureCalculator.Common;

namespace BlockSignatureCalculator.Readers
{
    public interface IReader : IStoppableByForce
    {
        event EventHandler DataIsOver;
        void ReadSource(Action<int, byte[]> produceSource);
    }
}
