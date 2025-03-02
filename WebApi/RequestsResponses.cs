using MaxMind.GeoIP2.Model;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApi;

public class GetGeolocationResponse(string? ipAddress = null, Country? country = null, string? errorMessage = null)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IpAddress { get; } = ipAddress;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Country? Country { get; } = country;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; } = errorMessage;
}

public class GetGeolocationsRequest(IEnumerable<string> ipAddresses)
{
    public IEnumerable<string> IpAddresses { get; } = ipAddresses;
}

public class GetGeolocationsResponse(IEnumerable<GetGeolocationResponse> geolocations)
{
    public IEnumerable<GetGeolocationResponse> Geolocations { get; } = geolocations;
}