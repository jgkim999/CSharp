using Spectre.Console;

using Unity.Tools;

namespace SpectreDemo;

public class SpectreProgressContext : IProgressContext
{
    //private readonly StatusContext _ctx;
    private readonly ProgressTask _task;

    public SpectreProgressContext(ProgressTask task)
    {
        _task = task;
    }

    public void SetMaxValue(double value)
    {
        _task.MaxValue = value;
    }

    public void Status(string message)
    {
        //_ctx.Status(message);
    }

    public void Increment(double value)
    {
        _task.Increment(value);
    }

    public void StartTask()
    {
        _task.StartTask();
    }

    public void StopTask()
    {
        _task.StopTask();
        _task.Description = $"{_task.Description} {_task.ElapsedTime?.Milliseconds}";
    }
}
