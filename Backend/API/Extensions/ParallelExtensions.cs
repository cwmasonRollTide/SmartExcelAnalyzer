namespace API.Extensions;

public static class ParallelExtensions
{
    public static async Task ForEachAsync<T>(
        this IEnumerable<T> source,
        ParallelOptions parallelOptions,
        Func<T, CancellationToken, Task> body
    )
    {
        await Task.WhenAll(
            source.Select(item => 
                Task.Run(
                    () => 
                    body(item, 
                         parallelOptions.CancellationToken
                    ), 
                    parallelOptions
                )
            )
        );
    }
}