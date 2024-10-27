using Persistence.Database;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class MongoDatabaseWrapperTests
{
    [Fact]
    [Obsolete("This test has been deprecated in favor of more robust testing strategies. It will be removed in a future release.")]
    public void MongoDatabaseWrapper_IsObsolete()
    {
        // This test exists to acknowledge the presence of the MongoDatabaseWrapper class
        // and explain why it's not being tested.

        var wrapperType = typeof(MongoDatabaseWrapper);

        var obsoleteAttribute = (ObsoleteAttribute?)Attribute.GetCustomAttribute(wrapperType, typeof(ObsoleteAttribute));

        Assert.NotNull(obsoleteAttribute);
        Assert.Contains("This class is not used in the current implementation", obsoleteAttribute.Message);
    }
}
