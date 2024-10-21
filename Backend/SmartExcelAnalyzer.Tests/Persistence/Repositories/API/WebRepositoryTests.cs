using Moq;
using System.Net;
using System.Text;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Persistence.Repositories.API;

namespace SmartExcelAnalyzer.Tests.Persistence.Repositories.API;

public class WebRepositoryTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public WebRepositoryTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        var client = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
    }

    [Fact]
    public async Task PostAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        var expectedResponse = new { Id = 1, Name = "Test" };
        var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var repository = new WebRepository<object>(_mockHttpClientFactory.Object);

        var result = await repository.PostAsync("https://api.example.com/endpoint", new { Data = "test" });

        Assert.NotNull(result);
        var resultAsJToken = result as JObject;
        Assert.Equal(expectedResponse.Id, resultAsJToken!["Id"]!.Value<int>());
        Assert.Equal(expectedResponse.Name, resultAsJToken!["Name"]!.Value<string>());
    }

    [Fact]
    public async Task PostAsync_FailedRequest_ThrowsHttpRequestException()
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Error")
            });

        var repository = new WebRepository<object>(_mockHttpClientFactory.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            repository.PostAsync("https://api.example.com/endpoint", new { Data = "test" }));
    }

    [Fact]
    public async Task PostAsync_VerifyRequestContent()
    {
        var payload = new { Data = "test" };
        var expectedContent = JsonConvert.SerializeObject(payload);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            })
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                Assert.Equal(expectedContent, content);
            });

        var repository = new WebRepository<object>(_mockHttpClientFactory.Object);

        await repository.PostAsync("https://api.example.com/endpoint", payload);

    }
}
