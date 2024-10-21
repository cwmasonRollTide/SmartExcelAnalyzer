using Xunit;
using Persistence.Repositories;

namespace SmartExcelAnalyzer.Tests.Persistence.Repositories;

public class MongoDatabaseWrapperTests
{
    [Fact]
    public void MongoDatabaseWrapper_IsObsolete()
    {
        // This test exists to acknowledge the presence of the MongoDatabaseWrapper class
        // and explain why it's not being tested.

        // Arrange
        var wrapperType = typeof(MongoDatabaseWrapper);

        // Act
        var obsoleteAttribute = (ObsoleteAttribute?)Attribute.GetCustomAttribute(wrapperType, typeof(ObsoleteAttribute));

        // Assert
        Assert.NotNull(obsoleteAttribute);
        Assert.Contains("This class is not used in the current implementation", obsoleteAttribute.Message);
        
        // Additional information for developers
        // The MongoDatabaseWrapper class is marked as obsolete and excluded from code coverage.
        // It is kept for reference purposes only and is not used in the current implementation.
        // Therefore, we are not writing extensive unit tests for this class.
    }
}
