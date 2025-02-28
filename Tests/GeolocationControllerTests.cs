using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Controllers;
using Xunit;

namespace Tests;

public class GeolocationControllerTests
{
    private readonly ILogger<GeolocationController> _logger;
    private readonly GeolocationController _sut;

    public GeolocationControllerTests()
    {
        _logger = new Logger<GeolocationController>(new LoggerFactory());
        _sut = new(_logger);
    }

    [Theory]
    [InlineData("206.172.131.27", "CA")]
    public void GivenAValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(string ipAddress, string expectedIsoCode)
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ipAddress);
        _sut.ControllerContext.HttpContext = httpContext;

        // When
        var result = _sut.GetGeolocation();

        // Then
        var which = result.Should().BeOfType<OkObjectResult>().Which;
        var value = which.Value.Should().BeOfType<GetGeolocationResponse>().Which;
        value.Should().BeEquivalentTo(new GetGeolocationResponse(ipAddress, isoCode: expectedIsoCode));
    }
}
