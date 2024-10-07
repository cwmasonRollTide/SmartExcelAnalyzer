using System.Diagnostics.CodeAnalysis;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

[ExcludeFromCodeCoverage]
public class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner = inner;

    public T Current
    {
        get
        {
            return _inner.Current;
        }
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}