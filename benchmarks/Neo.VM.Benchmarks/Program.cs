using Neo.VM;
using System.Diagnostics;
using System.Reflection;

DiagnosticListener.AllListeners.Subscribe(new DiagnosticObserver());

foreach (var method in typeof(Benchmarks).GetMethods(BindingFlags.Public | BindingFlags.Static))
{
    method.CreateDelegate<Action>().Invoke();
}

public class DiagnosticObserver : IObserver<DiagnosticListener>
{
    public void OnCompleted()
        => throw new NotImplementedException();

    public void OnError(Exception error)
        => throw new NotImplementedException();

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == Neo.VM.ExecutionEngine.LoggerCategory)
        {
            value.Subscribe(new KeyValueObserver());
        }
    }
}

public class KeyValueObserver : IObserver<KeyValuePair<string, object?>>
{
    public void OnCompleted()
        => throw new NotImplementedException();

    public void OnError(Exception error)
        => throw new NotImplementedException();

    public void OnNext(KeyValuePair<string, object?> value)
    {
        var cur = Activity.Current;
        ;
    }
}
