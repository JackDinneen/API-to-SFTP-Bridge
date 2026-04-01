namespace UnitTests.Application.Services;

using System.Net;
using API.Application.Services;
using API.Core.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.Helpers;

public class ApiConnectorServiceTests
{
    private readonly Mock<ILogger<ApiConnectorService>> _loggerMock = new();

    private ApiConnectorService CreateService(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler));
        return new ApiConnectorService(factory.Object, _loggerMock.Object);
    }

    // ---------------------------------------------------------------
    // SendRequestAsync - Basic
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendRequestAsync_SuccessfulGet_ReturnsBodyAnd200()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"result\":\"ok\"}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var config = new ApiRequestConfig { Url = "https://api.example.com/data" };
        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Body.Should().Contain("ok");
    }

    [Fact]
    public async Task SendRequestAsync_FailedRequest_ReturnsErrorMessageAndStatusCode()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"error\":\"bad request\"}", HttpStatusCode.BadRequest);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 0
        };
        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendRequestAsync_CustomHeaders_AreIncludedInRequest()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "custom-value",
                ["Authorization"] = "Bearer test-token"
            }
        };

        await service.SendRequestAsync(config);

        handler.SentRequests.Should().HaveCount(1);
        var sentRequest = handler.SentRequests[0];
        sentRequest.Headers.GetValues("X-Custom-Header").Should().Contain("custom-value");
        sentRequest.Headers.GetValues("Authorization").Should().Contain("Bearer test-token");
    }

    [Fact]
    public async Task SendRequestAsync_QueryParameters_AreAppendedToUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            QueryParameters = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value 2"
            }
        };

        await service.SendRequestAsync(config);

        handler.SentRequests.Should().HaveCount(1);
        var requestUrl = handler.SentRequests[0].RequestUri!.ToString();
        requestUrl.Should().Contain("key1=value1");
        // HttpClient may decode %20 back to space in RequestUri.ToString()
        (requestUrl.Contains("key2=value%202") || requestUrl.Contains("key2=value 2"))
            .Should().BeTrue("query parameter key2 with value 'value 2' should be present");
    }

    [Fact]
    public async Task SendRequestAsync_Timeout_ReturnsTimeoutResponse()
    {
        var slowHandler = new SlowHttpMessageHandler(TimeSpan.FromSeconds(10));
        var service = CreateService(slowHandler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            TimeoutSeconds = 1,
            MaxRetries = 0
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(408);
        result.ErrorMessage.Should().Contain("timed out");
    }

    // ---------------------------------------------------------------
    // SendRequestAsync - Retry logic
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendRequestAsync_500Response_RetriesUpToMaxRetries()
    {
        var handler = new MockHttpMessageHandler();
        // Enqueue 4 responses: 3 retries + 1 final success
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{\"result\":\"ok\"}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 3
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(4);
    }

    [Fact]
    public async Task SendRequestAsync_429Response_RetriesWithBackoff()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse("{\"result\":\"ok\"}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 3
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendRequestAsync_400Response_DoesNotRetry()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"error\":\"bad request\"}", HttpStatusCode.BadRequest);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 3
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        handler.SentRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendRequestAsync_404Response_DoesNotRetry()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"error\":\"not found\"}", HttpStatusCode.NotFound);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 3
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        handler.SentRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendRequestAsync_500ExhaustsRetries_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            MaxRetries = 3
        };

        var result = await service.SendRequestAsync(config);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        // Initial attempt + 3 retries = 4 total
        handler.SentRequests.Should().HaveCount(4);
    }

    // ---------------------------------------------------------------
    // TestConnectionAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task TestConnectionAsync_200Response_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.TestConnectionAsync("https://api.example.com/health");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_500Response_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{}", HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var result = await service.TestConnectionAsync("https://api.example.com/health");

        result.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // FetchPaginatedAsync - Offset
    // ---------------------------------------------------------------

    [Fact]
    public async Task FetchPaginatedAsync_OffsetPagination_SendsIncrementingOffset()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"data\":[1,2,3],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[4,5,6],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[],\"hasMore\":false}");
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.Offset,
                PageSize = 3,
                MaxPages = 10
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(3);

        // Verify offset params in requests
        var urls = handler.SentRequests.Select(r => r.RequestUri!.ToString()).ToList();
        urls[0].Should().Contain("offset=0");
        urls[1].Should().Contain("offset=3");
        urls[2].Should().Contain("offset=6");
    }

    // ---------------------------------------------------------------
    // FetchPaginatedAsync - Page Number
    // ---------------------------------------------------------------

    [Fact]
    public async Task FetchPaginatedAsync_PageNumberPagination_SendsIncrementingPage()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"data\":[1,2,3],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[4,5,6],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[],\"hasMore\":false}");
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.PageNumber,
                PageSize = 3,
                MaxPages = 10
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(3);

        var urls = handler.SentRequests.Select(r => r.RequestUri!.ToString()).ToList();
        urls[0].Should().Contain("page=1");
        urls[1].Should().Contain("page=2");
        urls[2].Should().Contain("page=3");
    }

    // ---------------------------------------------------------------
    // FetchPaginatedAsync - Cursor
    // ---------------------------------------------------------------

    [Fact]
    public async Task FetchPaginatedAsync_CursorPagination_ExtractsCursorFromResponse()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"data\":[1,2],\"nextCursor\":\"abc123\",\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[3,4],\"nextCursor\":\"def456\",\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[5],\"hasMore\":false}");
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.Cursor,
                PageSize = 2,
                MaxPages = 10,
                CursorParameterName = "cursor",
                CursorJsonPath = "nextCursor"
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(3);

        var urls = handler.SentRequests.Select(r => r.RequestUri!.ToString()).ToList();
        urls[0].Should().NotContain("cursor=");
        urls[1].Should().Contain("cursor=abc123");
        urls[2].Should().Contain("cursor=def456");
    }

    // ---------------------------------------------------------------
    // FetchPaginatedAsync - Stop conditions
    // ---------------------------------------------------------------

    [Fact]
    public async Task FetchPaginatedAsync_StopsOnEmptyData()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"data\":[1,2,3],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[],\"hasMore\":true}");
        // Third response should not be reached
        handler.EnqueueResponse("{\"data\":[7,8,9],\"hasMore\":true}");
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.Offset,
                PageSize = 3,
                MaxPages = 10
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(2);
    }

    [Fact]
    public async Task FetchPaginatedAsync_StopsOnHasMoreFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("{\"data\":[1,2,3],\"hasMore\":true}");
        handler.EnqueueResponse("{\"data\":[4,5],\"hasMore\":false}");
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.Offset,
                PageSize = 3,
                MaxPages = 10
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(2);
    }

    [Fact]
    public async Task FetchPaginatedAsync_RespectsMaxPagesSafetyLimit()
    {
        var handler = new MockHttpMessageHandler();
        // Enqueue enough responses to exceed max pages
        for (int i = 0; i < 5; i++)
        {
            handler.EnqueueResponse("{\"data\":[1,2,3],\"hasMore\":true}");
        }
        var service = CreateService(handler);

        var config = new ApiRequestConfig
        {
            Url = "https://api.example.com/data",
            Pagination = new PaginationConfig
            {
                Type = PaginationType.PageNumber,
                PageSize = 3,
                MaxPages = 3
            }
        };

        var result = await service.FetchPaginatedAsync(config);

        result.IsSuccess.Should().BeTrue();
        result.Pages.Should().HaveCount(3);
        handler.SentRequests.Should().HaveCount(3);
    }
}
