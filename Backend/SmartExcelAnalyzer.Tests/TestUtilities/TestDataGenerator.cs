
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

public static class TestDataGenerator
{
    public static IEnumerable<ConcurrentDictionary<string, object>> GenerateLargeDataSet(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new ConcurrentDictionary<string, object>
            {
                ["id"] = $"id_{i}",
                ["data"] = $"data_{i}",
                ["embedding"] = Enumerable.Range(0, 10).Select(_ => (float)Random.Shared.NextDouble()).ToArray()
            };
        }
    }
}