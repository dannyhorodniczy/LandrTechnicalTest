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
            return NotFound("Unable to determine the remote IP address.");
        }

        try
        {
            // TODO: determine best practice on how often to create a new DatabaseReader
            // per request?
            // per lifetime of the controller?
            using var reader = new DatabaseReader(_databasePath);
            //var ipAddress = this.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4();
            var ipAddress = "206.172.131.27";
            var response = reader.Country(ipAddress);
            // TODOL double check that the country code cannot possibly be null
            return Ok(new GetGeolocationResponse(ipAddress.ToString(), response.Country.IsoCode!));
        }
        catch (AddressNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception)
        {
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
                response.Add(new GetGeolocationResponse(ipAddress, country.Country.IsoCode!));
            }
            catch (AddressNotFoundException e)
            {
                response.Add(new GetGeolocationResponse(ipAddress, e.Message));
            }
            catch (Exception)
            {
                response.Add(new GetGeolocationResponse(ipAddress, "Some shit went down. Ain't got no idea."));
            }
        }

        return Ok(new GetGeolocationsResponse(response));
    }
}

public record GetGeolocationsRequest(IEnumerable<string> ipAddresses);

public record GetGeolocationsResponse(IEnumerable<GetGeolocationResponse> geolocations);

public record GetGeolocationResponse(string ipAddress, string isoCode);
