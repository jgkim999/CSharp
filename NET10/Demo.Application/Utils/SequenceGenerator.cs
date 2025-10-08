namespace Demo.Application.Utils;

public class SequenceGenerator
{
    private int _sendSequence = 0;
    
    public ushort GetNext()
    {
        int initial, computed, result;
        do
        {
            initial = _sendSequence;
            if (initial >= ushort.MaxValue)
                computed = 0;
            else
                computed = initial + 1;
            result = Interlocked.CompareExchange(ref _sendSequence, computed, initial);
        }
        while (result != initial);

        return (ushort)computed;
    }
}
