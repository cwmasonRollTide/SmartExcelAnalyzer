
using Moq;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

[ExcludeFromCodeCoverage]
public static class MockLoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string message)
    {
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == logLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

    }
}