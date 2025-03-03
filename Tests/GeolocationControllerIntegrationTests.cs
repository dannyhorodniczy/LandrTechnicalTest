using FluentAssertions;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    public async Task GivenAValidIpAddress_WhenGetGeolocation_ThenGeolocationReturned(string ipAddress, string expectedIsoCode)
    {
        // Given
        FakeRemoteIpAddressMiddleware.SetIpAddress(ipAddress);
        var client = _factory.CreateClient();

        // When
        var response = await client.GetAsync("/Geolocation");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocation = await response.Content.ReadFromJsonAsync<GetGeolocationResponse>();
        geolocation!.Country!.IsoCode.Should().BeEquivalentTo(expectedIsoCode);
        geolocation.ErrorMessage.Should().BeNull();
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
        var geolocations = geolocationsResponse!.Geolocations.ToArray();

        for (int i = 0; i < expectedIsoCodes.Length; i++)
        {
            geolocations[i].IpAddress.Should().BeEquivalentTo(ipAddresses[i]);
            geolocations[i].Country!.IsoCode.Should().BeEquivalentTo(expectedIsoCodes[i]);
            geolocations[i].ErrorMessage.Should().BeNull();
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var geolocationsResponse = await response.Content.ReadFromJsonAsync<GetGeolocationsResponse>();
        var geolocations = geolocationsResponse!.Geolocations.ToArray();

        for (int i = 0; i < ipAddresses.Length; i++)
        {
            geolocations[i].IpAddress.Should().BeEquivalentTo(ipAddresses[i]);
            geolocations[i].ErrorMessage.Should().NotBeNull();
            geolocations[i].Country.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(
        new string[] { "127.0.0.1", "206.172.131.27", "not_an_ip_address" },
        new string?[] { null, "CA", null })]
    public async Task GivenValidAndInvalidIpAddresses_WhenGetGeolocations_ThenGeolocationsAndErrorsReturned(
        string[] ipAddresses,
        string?[] expected)
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
        var geolocations = geolocationsResponse!.Geolocations.ToArray();

        for (int i = 0; i < ipAddresses.Length; i++)
        {
            geolocations[i].IpAddress.Should().BeEquivalentTo(ipAddresses[i]);

            if (expected[i] == null)
            {
                geolocations[i].ErrorMessage.Should().NotBeNull();
                geolocations[i].Country.Should().BeNull();
            }
            else
            {
                geolocations[i].Country!.IsoCode.Should().BeEquivalentTo(expected[i]);
                geolocations[i].ErrorMessage.Should().BeNull();
            }

        }
    }
}
