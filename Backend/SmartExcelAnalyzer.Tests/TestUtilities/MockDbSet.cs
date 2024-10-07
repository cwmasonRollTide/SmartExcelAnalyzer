using Microsoft.EntityFrameworkCore;
using Moq;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

public class MockDbSet<T> : Mock<DbSet<T>> where T : class
{
    private readonly List<T> _data;
    private readonly IQueryable<T> _queryable;

    public MockDbSet(List<T> data)
    {
        _data = data;
        _queryable = _data.AsQueryable();

        As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(_queryable.GetEnumerator()));

        As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(_queryable.Provider));

        As<IQueryable<T>>().Setup(m => m.Expression).Returns(_queryable.Expression);
        As<IQueryable<T>>().Setup(m => m.ElementType).Returns(_queryable.ElementType);
        As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => _queryable.GetEnumerator());

        Setup(x => x.AsQueryable()).Returns(_queryable);
        Setup(x => x.AsAsyncEnumerable()).Returns(new TestAsyncEnumerable<T>(_queryable));
    }

    public void UpdateData(List<T> newData)
    {
        _data.Clear();
        _data.AddRange(newData);
    }
}
