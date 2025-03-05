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
        var ipAddress = DetermineRemoteIpAddress();

        if (ipAddress == null)
        {
            _logger.LogInformation("Unable to determine the remote IP address.");
            var pd = WebApi.ProblemDetailsFactory.Create(ProblemType.NoIpAddressProvided, this.Request);
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
            var pd = WebApi.ProblemDetailsFactory.Create(ProblemType.AddressNotFound, this.Request, e);
            return NotFound(pd);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error occurred.");
            var pd = WebApi.ProblemDetailsFactory.Create(ProblemType.UnknownError, this.Request, e);
            return StatusCode(StatusCodes.Status500InternalServerError, pd);
        }
    }

    [HttpPost]
    [ProducesResponseType<GetGeolocationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PartialContentGetGeolocationsResponse>(StatusCodes.Status206PartialContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetGeolocations(GetGeolocationsRequest request)
    {
        int ipAddressCount = request.ipAddresses.Count();
        if (ipAddressCount == 0)
        {
            _logger.LogInformation("No IP addresses provided.");
            var pd = WebApi.ProblemDetailsFactory.Create(ProblemType.NoIpAddressProvided, this.Request);
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

        if (geolocations.Count == 0)
        {
            var pdBadRequest = WebApi.ProblemDetailsFactory.Create(ProblemType.UnableToFindGeolocations, this.Request, errors: errors);
            return BadRequest(pdBadRequest);
        }

        var pdPartialContent = WebApi.ProblemDetailsFactory.Create(
            ProblemType.UnableToFindGeolocations,
            this.Request,
            errors: errors,
            statusCode: StatusCodes.Status206PartialContent);
        var partialContent = new PartialContentGetGeolocationsResponse(geolocations, pdPartialContent);
        return StatusCode(StatusCodes.Status206PartialContent, partialContent);
    }

    private IPAddress? DetermineRemoteIpAddress()
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

        return ipAddress;
    }
}