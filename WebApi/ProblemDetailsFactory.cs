using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace WebApi;

public enum ProblemType
{
    NoIpAddressProvided = 0,
    AddressNotFound = 1,
    UnknownError = 2,
    UnableToFindGeolocations = 3
}

public static class ProblemDetailsFactory
{
    public static ProblemDetails Create(
        ProblemType problemType,
        HttpRequest request,
        Exception? exception = null,
        List<object>? errors = null,
        int? statusCode = null)
    {
        return problemType switch
        {
            ProblemType.NoIpAddressProvided => new ProblemDetails
            {
                Title = "No remote IP address",
                Detail = "Unable to parse or determine the IP address(es) to be geolocalized.",
                Status = statusCode == null ? StatusCodes.Status400BadRequest : statusCode,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Instance = $"{request.Method} {request.Path}",
            },
            ProblemType.AddressNotFound => new ProblemDetails
            {
                Title = "Address not found",
                Detail = $"IpAddress: {request.HttpContext.Connection.RemoteIpAddress}, Message: {exception?.Message}",
                Status = statusCode == null ? StatusCodes.Status404NotFound : statusCode,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Instance = $"{request.Method} {request.Path}",
            },
            ProblemType.UnknownError => new ProblemDetails
            {
                Title = "Unknown error occurred.",
                Detail = $"IpAddress: {request.HttpContext.Connection.RemoteIpAddress} Message: {exception?.Message}",
                Status = statusCode == null ? StatusCodes.Status500InternalServerError : statusCode,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Instance = $"{request.Method} {request.Path}",

            },
            ProblemType.UnableToFindGeolocations => new ProblemDetails
            {
                Title = "No remote IP address",
                Detail = "Unable to find geolocation(s) for some of all of the IP address(es).",
                Status = statusCode == null ? StatusCodes.Status400BadRequest : statusCode,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Instance = $"{request.Method} {request.Path}",
                Extensions = errors == null ?
                                new Dictionary<string, object?>() :
                                new Dictionary<string, object?>() { { "errors", errors } }
            },
            _ => throw new ArgumentException(nameof(problemType))
        };
    }
}
