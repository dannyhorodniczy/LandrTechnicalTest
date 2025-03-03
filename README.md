# LandrTechnicalTest

## Build status
[![Build & Test](https://github.com/dannyhorodniczy/LandrTechnicalTest/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dannyhorodniczy/LandrTechnicalTest/actions/workflows/dotnet.yml)

## Description

Ensure that you have the following isntalled on your machine:
- .NET 9 SDK & runtime
- Docker Desktop

To run the application, navigate to the root of the repo and run the following commands:
```
docker build -t geolocation-service-image -f WebApi/Dockerfile .
docker container run -d --name dannys-geolocation-service -p 9090:8080 geolocation-service-image
```
Note: if port 9090 is already in use, you can change the port number in the second command, i.e, `-p my_available_port:8080`.

Using your favourite API testing method, you can now GET and post to `http://localhost:9090/Geolocation`

When POSTing, you are required to pass the IP addresses with the following JSON:
```
{
    "ipAddresses": [
        "127.0.0.1",
        "206.172.131.27"
    ]
}
```

If there is a problem obtaining geolocation data for any of your IP addresses, an error message will be provided.