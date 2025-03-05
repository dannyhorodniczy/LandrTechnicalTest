using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GeolocationController : ControllerBase
{
    private readonly ILogger<GeolocationController> _logger;
    private readonly IGeoIP2DatabaseReader _dbReader;

    public GeolocationController(
        IGeoIP2DatabaseReader dbReader,
        ILogger<GeolocationController> logger)
    {
        _dbReader = dbReader;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<GetGeolocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GetGeolocationResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<GetGeolocationResponse>(StatusCodes.Status500InternalServerError)]
    public IActionResult GetGeolocation()
    {
        IPAddress? ipAddress = null;
        if (this.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            IPAddress.TryParse(forwardedFor, out ipAddress))
        {
        }
        else if (this.Request.Headers.TryGetValue("Forwarded", out var forwarded) &&
                 IPAddress.TryParse(forwarded, out ipAddress))
        {
        }
        else
        {
            ipAddress = this.Request.HttpContext.Connection.RemoteIpAddress;
        }

        if (ipAddress == null)
        {
            const string noRemoteIpMessage = "Unable to determine the remote IP address.";
            _logger.LogInformation(noRemoteIpMessage);
            var pd = new ProblemDetails
            {
                Title = "No remote IP address",
                Detail = noRemoteIpMessage,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Instance = $"{this.Request.Method} {this.Request.Path}",

            };
            return BadRequest(pd);
        }

        try
        {
            var countryResponse = _dbReader.Country(ipAddress);
            var response = new GetGeolocationResponse(
                ipAddress.ToString(),
                countryResponse.Country);
            return Ok(response);
        }
        catch (AddressNotFoundException e)
        {
            _logger.LogError(e, "Address not found.");
            var pd = new ProblemDetails
            {
                Title = "Address not found",
                Detail = $"IpAddress: {this.Request.HttpContext.Connection.RemoteIpAddress}, Message: {e.Message}",
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Instance = $"{this.Request.Method} {this.Request.Path}",

            };
            return NotFound(pd);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error occurred.");
            var pd = new ProblemDetails
            {
                Title = "Unknown error occurred.",
                Detail = $"IpAddress: {this.Request.HttpContext.Connection.RemoteIpAddress}\n{e.Message}",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Instance = $"{this.Request.Method} {this.Request.Path}",

            };

            return StatusCode(StatusCodes.Status500InternalServerError, pd);
        }
    }

    [HttpPost]
    [ProducesResponseType<GetGeolocationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MultiStatusGetGeolocationsResponse>(StatusCodes.Status207MultiStatus)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetGeolocations(GetGeolocationsRequest request)
    {
        var pd = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Instance = $"{this.Request.Method} {this.Request.Path}"
        };

        int ipAddressCount = request.ipAddresses.Count();
        if (ipAddressCount == 0)
        {
            const string noIpAddressesMessage = "No IP addresses provided.";
            pd.Title = "No remote IP address";
            pd.Detail = noIpAddressesMessage;
            _logger.LogInformation(noIpAddressesMessage);
            return BadRequest(pd);
        }

        var geolocations = new List<GetGeolocationResponse>(ipAddressCount);
        var errors = new List<object>(ipAddressCount);

        foreach (var ipAddress in request.ipAddresses)
        {
            try
            {
                var countryResponse = _dbReader.Country(ipAddress);
                var geolocation = new GetGeolocationResponse(ipAddress, country: countryResponse.Country);
                geolocations.Add(geolocation);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error for IP address {ipAddress}: {e.GetType().Name} - {e.Message}");
                errors.Add(new { ipAddress, e.GetType().Name, e.Message });
            }
        }

        if (errors.Count == 0)
        {
            var response = new GetGeolocationsResponse(geolocations);
            return Ok(response);
        }

        pd.Extensions = new Dictionary<string, object?>() { { "errors", errors } };

        if (geolocations.Count == 0)
        {
            const string unableToFindGeolocationMessage = "Unable to find geolocation for all IP addresses.";
            pd.Title = unableToFindGeolocationMessage;
            pd.Detail = unableToFindGeolocationMessage;
            return BadRequest(pd);
        }

        const string unableToFindSomeGeolocationsMessage = "Unable to find geolocation for some IP addresses.";
        pd.Title = unableToFindSomeGeolocationsMessage;
        pd.Detail = unableToFindSomeGeolocationsMessage;

        var multi = new MultiStatusGetGeolocationsResponse(geolocations, pd);
        return StatusCode(StatusCodes.Status207MultiStatus, multi);
    }
}