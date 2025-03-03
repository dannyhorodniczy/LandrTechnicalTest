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
    private readonly IGeoIP2DatabaseReader _dbReader;

    public GeolocationController(IGeoIP2DatabaseReader dbReader, ILogger<GeolocationController> logger)
    {
        _dbReader = dbReader;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetGeolocation()
    {
        if (this.Request.HttpContext.Connection.RemoteIpAddress == null)
        {
            const string noRemoteIpMessage = "Unable to determine the remote IP address.";
            _logger.LogInformation(noRemoteIpMessage);
            return BadRequest(noRemoteIpMessage);
        }

        try
        {
            var countryResponse = _dbReader.Country(this.Request.HttpContext.Connection.RemoteIpAddress);
            var response = new GetGeolocationResponse(
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                country: countryResponse.Country);
            return Ok(response);
        }
        catch (AddressNotFoundException e)
        {
            _logger.LogError(e, "Address not found.");
            var response = new GetGeolocationResponse(
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                errorMessage: e.Message);
            return NotFound(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error occurred.");
            var response = new GetGeolocationResponse(
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                errorMessage: e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    [HttpPost]
    public IActionResult GetGeolocations(GetGeolocationsRequest request)
    {
        int ipAddressCount = request.IpAddresses.Count();
        if (ipAddressCount == 0)
        {
            const string noIpAddressesMessage = "No IP addresses provided.";
            _logger.LogInformation(noIpAddressesMessage);
            return BadRequest(noIpAddressesMessage);
        }

        var geolocations = new List<GetGeolocationResponse>(ipAddressCount);

        foreach (var ipAddress in request.IpAddresses)
        {
            try
            {
                var countryResponse = _dbReader.Country(ipAddress);
                var geolocation = new GetGeolocationResponse(ipAddress, country: countryResponse.Country);
                geolocations.Add(geolocation);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unknown error occurred.");
                var errorResponse = new GetGeolocationResponse(ipAddress, errorMessage: e.Message);
                geolocations.Add(errorResponse);
            }
        }

        var response = new GetGeolocationsResponse(geolocations);
        return Ok(response);
    }
}