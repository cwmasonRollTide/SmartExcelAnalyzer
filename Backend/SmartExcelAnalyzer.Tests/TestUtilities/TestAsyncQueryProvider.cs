using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

[ExcludeFromCodeCoverage]
public class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

    public object Execute(Expression expression) => _inner.Execute(expression)!;

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments().First();
        
        var executeMethod = typeof(IQueryProvider)
            .GetMethods()
            .FirstOrDefault(m => 
                m.Name == nameof(IQueryProvider.Execute) &&
                m.IsGenericMethod &&
                m.GetParameters()!.Length is 1 &&
                m!.GetParameters()![0]!.ParameterType == typeof(Expression))
            ?? throw new InvalidOperationException("Could not find Execute method on IQueryProvider");

        var genericExecute = executeMethod.MakeGenericMethod(expectedResultType);

        var executionResult = genericExecute.Invoke(this, new object[] { expression })
            ?? throw new InvalidOperationException("Query execution returned null");

        var fromResultMethod = typeof(Task)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == nameof(Task.FromResult) &&
                m.IsGenericMethod)
            ?? throw new InvalidOperationException("Could not find FromResult method on Task");

        var genericFromResult = fromResultMethod.MakeGenericMethod(expectedResultType);
        var result = genericFromResult.Invoke(null, [executionResult])
            ?? throw new InvalidOperationException("Task.FromResult returned null");

        return (TResult)result;
    }
}
