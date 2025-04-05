namespace Unity.Tools
{
    public interface IProgressContext
    {
        void Status(string message);
        void SetMaxValue(double value);
        void Increment(double value);
        public void StartTask();
        public void StopTask();
    }
}