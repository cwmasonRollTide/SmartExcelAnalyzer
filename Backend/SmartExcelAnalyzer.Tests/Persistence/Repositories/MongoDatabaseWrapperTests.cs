using Persistence.Repositories;

namespace SmartExcelAnalyzer.Tests.Persistence.Repositories;

public class MongoDatabaseWrapperTests
{
    [Fact]
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
