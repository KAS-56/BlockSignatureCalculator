namespace BlockSignatureCalculator.Common
{
    public abstract class StoppableByForceBase : IStoppableByForce
    {
        protected bool ForcedStop;
        public void StopByForce() => ForcedStop = true;
    }
}
