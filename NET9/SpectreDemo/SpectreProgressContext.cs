using Spectre.Console;

using Unity.Tools;

namespace SpectreDemo;

public class SpectreProgressContext : IProgressContext
{
    //private readonly StatusContext _ctx;
    private readonly ProgressTask _task;
    private readonly string _description;
    
    public SpectreProgressContext(ProgressTask task)
    {
        _task = task;
        _description = task.Description;
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
        _task.Description = $"{_description} {_task.Value}/{_task.MaxValue}";
    }

    public void StartTask()
    {
        _task.StartTask();
    }

    public void StopTask()
    {
        _task.StopTask();
    }
}
