using System;

using Unity.Tools;

namespace UnityUtilGui.ViewModels;

public class AvaloniaProgressContext : IProgressContext
{
    private Action<int> _incAction;
    private Action<int> _maxAction;
    private Action<string> _statusAction;
    private Action _startTask;
    private Action _stopTask;
    
    public AvaloniaProgressContext(
        Action<int> maxAction,
        Action<int> incAction,
        Action<string> statusAction,
        Action startTask,
        Action stopTask)
    {
        _incAction = incAction;
        _maxAction = maxAction;
        _statusAction = statusAction;
        _startTask = startTask;
        _stopTask = stopTask;
    }
    
    public void Status(string message)
    {
        _statusAction?.Invoke(message);
    }

    public void SetMaxValue(double value)
    {
        _maxAction?.Invoke((int)value);
    }

    public void Increment(double value)
    {
        _incAction?.Invoke((int)value);
    }

    public void StartTask()
    {
        _startTask?.Invoke();
    }

    public void StopTask()
    {
        _stopTask?.Invoke();
    }
}