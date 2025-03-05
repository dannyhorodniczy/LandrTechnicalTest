using FluentAssertions;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebApi;
using Xunit;

namespace Tests;

public class GeolocationControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GeolocationControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddSingleton<IStartupFilter, CustomStartupFilter>()));
    }

    [Theory]
    [InlineData("206.172.131.27", "CA")]
    [InlineData("1.32.195.2", "SG")]
    [InlineData("79.170.232.99", "FR")]
    [InlineData("103.100.236.88", "CN")]
    [InlineData("154.93.33.1", "RU")]
    [InlineData("180.87.172.22", "IN")]
    public async Task GivenAValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(
        string ipAddress,
        string expectedIsoCode)
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress(ipAddress);
        var client = _factory.CreateClient();

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocation = await response.Content.ReadFromJsonAsync<GetGeolocationResponse>();
        geolocation!.country.IsoCode.Should().BeEquivalentTo(expectedIsoCode);
    }

    [Theory]
    [InlineData("17.32.195.2", "US")]
    public async Task GivenXForwardedForValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(
        string ipAddress,
        string expectedIsoCode)
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress("0.0.0.0");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocation = await response.Content.ReadFromJsonAsync<GetGeolocationResponse>();
        geolocation!.country.IsoCode.Should().BeEquivalentTo(expectedIsoCode);
    }

    [Theory]
    [InlineData("60.32.195.2", "JP")]
    public async Task GivenForwardedValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(
        string ipAddress,
        string expectedIsoCode)
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress("0.0.0.0");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Forwarded", ipAddress);

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocation = await response.Content.ReadFromJsonAsync<GetGeolocationResponse>();
        geolocation!.country.IsoCode.Should().BeEquivalentTo(expectedIsoCode);
    }

    [Fact]
    public async Task GivenNullIpAddress_WhenGetGeolocation_ThenReturns400Async()
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress(null);
        var client = _factory.CreateClient();

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenIpAddressDoesNotExistInDatabase_WhenGetGeolocation_ThenReturns404Async()
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress("127.0.0.1");
        var client = _factory.CreateClient();

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenTheDatabaseCallThrows_WhenGetGeolocation_ThenReturns500()
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress("206.172.131.27");
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IGeoIP2DatabaseReader>(new FakeDatabaseReader());
            });
        })
        .CreateClient();

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
    }

    [Theory]
    [InlineData(
        new string[] { "206.172.131.27", "1.32.195.2", "79.170.232.99", "103.100.236.88", "154.93.33.1", "180.87.172.22" },
        new string[] { "CA", "SG", "FR", "CN", "RU", "IN" })]
    public async Task GivenValidIpAddresses_WhenGetGeolocations_ThenGeolocationsReturned(string[] ipAddresses, string[] expectedIsoCodes)
    {
        // Given
        var client = _factory.CreateClient();
        var request = new GetGeolocationsRequest(ipAddresses);
        var jsonContent = JsonContent.Create(request);

        // When
        var response = await client.PostAsync("/Geolocation", jsonContent);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocationsResponse = await response.Content.ReadFromJsonAsync<GetGeolocationsResponse>();
        var geolocations = geolocationsResponse!.geolocations.ToArray();

        for (int i = 0; i < expectedIsoCodes.Length; i++)
        {
            geolocations[i].ipAddress.Should().BeEquivalentTo(ipAddresses[i]);
            geolocations[i].country!.IsoCode.Should().BeEquivalentTo(expectedIsoCodes[i]);
        }
    }

    [Fact]
    public async Task GivenInvalidIpAddresses_WhenGetGeolocations_ThenErrorsReturned()
    {
        // Given
        string[] ipAddresses = new string[] { "127.0.0.1", "not_an_ip_address" };
        var client = _factory.CreateClient();
        var request = new GetGeolocationsRequest(ipAddresses);
        var jsonContent = JsonContent.Create(request);

        // When
        var response = await client.PostAsync("/Geolocation", jsonContent);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetailsResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetailsResponse.Should().NotBeNull();
    }

    [Theory]
    [InlineData(
        new string[] { "127.0.0.1", "206.172.131.27", "not_an_ip_address" },
        new string[] { "206.172.131.27" },
        new string[] { "CA" })]
    public async Task GivenValidAndInvalidIpAddresses_WhenGetGeolocations_ThenReturns207(
        string[] ipAddresses,
        string[] expectedIpAddresses,
        string[] expectedIsoCodes)
    {
        // Given
        var client = _factory.CreateClient();
        var request = new GetGeolocationsRequest(ipAddresses);
        var jsonContent = JsonContent.Create(request);

        // When
        var response = await client.PostAsync("/Geolocation", jsonContent);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);

        var geolocationsResponse = await response.Content.ReadFromJsonAsync<PartialContentGetGeolocationsResponse>();
        var geolocations = geolocationsResponse!.geolocations.ToArray();

        for (int i = 0; i < expectedIpAddresses.Length; i++)
        {
            geolocations[i].ipAddress.Should().BeEquivalentTo(expectedIpAddresses[i]);
            geolocations[i].country!.IsoCode.Should().BeEquivalentTo(expectedIsoCodes[i]);
        }

        geolocationsResponse.problemDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenNoIpAddresses_WhenGetGeolocations_ThenReturns400()
    {
        // Given
        var client = _factory.CreateClient();
        var request = new GetGeolocationsRequest(Enumerable.Empty<string>());
        var jsonContent = JsonContent.Create(request);

        // When
        var response = await client.PostAsync("/Geolocation", jsonContent);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetailsResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetailsResponse.Should().NotBeNull();
    }
}
