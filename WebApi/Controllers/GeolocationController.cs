using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GeolocationController : ControllerBase
{
    private readonly ILogger<GeolocationController> _logger;
    private readonly string _databasePath = "GeoLite2-Country_20250227/GeoLite2-Country.mmdb";

    public GeolocationController(ILogger<GeolocationController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetGeolocation()
    {
        if (this.Request.HttpContext.Connection.RemoteIpAddress == null)
        {
            const string noRemoteIpMessage = "Unable to determine the remote IP address.";
            _logger.LogInformation(noRemoteIpMessage);
            return NotFound(noRemoteIpMessage);
        }

        try
        {
            // TODO: determine best practice on how often to create a new DatabaseReader
            // per request?
            // per lifetime of the controller?
            using var reader = new DatabaseReader(_databasePath);
            var ipAddress = this.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4();
            var response = reader.Country(ipAddress);

            // TODO: double check that the country code cannot possibly be null
            return Ok(new GetGeolocationResponse(ipAddress.ToString(), isoCode: response.Country.IsoCode!));
        }
        catch (AddressNotFoundException e)
        {
            _logger.LogError(e, "Address not found.");
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error occurred.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    public IActionResult GetGeolocations(GetGeolocationsRequest request)
    {
        using var reader = new DatabaseReader(_databasePath);
        var response = new List<GetGeolocationResponse>(request.ipAddresses.Count());

        foreach (var ipAddress in request.ipAddresses)
        {
            try
            {
                var country = reader.Country(ipAddress);
                // TODO: double check that the country code cannot possibly be null
                response.Add(new GetGeolocationResponse(ipAddress, isoCode: country.Country.IsoCode!));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unknown error occurred.");
                response.Add(new GetGeolocationResponse(ipAddress, errorMessage: e.Message));
            }
        }

        return Ok(new GetGeolocationsResponse(response));
    }
}

public record GetGeolocationsRequest(IEnumerable<string> ipAddresses);

public record GetGeolocationsResponse(IEnumerable<GetGeolocationResponse> geolocations);

public record GetGeolocationResponse(string ipAddress, string? isoCode = null, string? errorMessage = null);
