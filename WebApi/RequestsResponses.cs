using MaxMind.GeoIP2.Model;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace WebApi;

public record GetGeolocationResponse(string ipAddress, Country country);

public record GetGeolocationsRequest(IEnumerable<string> ipAddresses);

public record GetGeolocationsResponse(IEnumerable<GetGeolocationResponse> geolocations);

public record PartialContentGetGeolocationsResponse(IEnumerable<GetGeolocationResponse> geolocations, ProblemDetails problemDetails);