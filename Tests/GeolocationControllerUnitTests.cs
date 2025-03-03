using FluentAssertions;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi;
using WebApi.Controllers;
using Xunit;

namespace Tests;

public class GeolocationControllerUnitTests
{
    private readonly ILogger<GeolocationController> _logger;
    private readonly GeolocationController _sut;

    public GeolocationControllerUnitTests()
    {
        _logger = new Logger<GeolocationController>(new LoggerFactory());
        _sut = new(new DatabaseReader("GeoLite2-Country_20250227/GeoLite2-Country.mmdb"), _logger);
    }

    // TODO: maybe diversify the test cases :/

    [Theory]
    [InlineData("206.172.131.27", "CA")]
    [InlineData("1.32.195.2", "SG")]
    [InlineData("79.170.232.99", "FR")]
    [InlineData("103.100.236.88", "CN")]
    [InlineData("154.93.33.1", "RU")]
    [InlineData("180.87.172.22", "IN")]
    public void GivenAValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(string ipAddress, string expectedIsoCode)
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ipAddress);
        _sut.ControllerContext.HttpContext = httpContext;

        // When
        var result = _sut.GetGeolocation();

        // Then
        result.Should()
            .BeOfType<OkObjectResult>().Which.Value
            .Should()
            .BeOfType<GetGeolocationResponse>().Which.Country!.IsoCode
            .Should()
            .BeEquivalentTo(expectedIsoCode);
    }

    [Fact]
    public void GivenNullIpAddress_WhenGetGeolocation_ThenReturns400()
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = null;
        _sut.ControllerContext.HttpContext = httpContext;

        // When
        var result = _sut.GetGeolocation();

        // Then
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GivenIpAddressDoesNotExistInDatabase_WhenGetGeolocation_ThenReturns404()
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _sut.ControllerContext.HttpContext = httpContext;

        // When
        var result = _sut.GetGeolocation();

        // Then
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
