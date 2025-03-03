using MaxMind.Db;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using System;
using System.Net;

namespace Tests;

public class FakeDatabaseReader : IGeoIP2DatabaseReader
{
    public Metadata Metadata => throw new NotImplementedException();

    public AnonymousIPResponse AnonymousIP(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public AnonymousIPResponse AnonymousIP(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public AsnResponse Asn(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public AsnResponse Asn(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public CityResponse City(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public CityResponse City(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public ConnectionTypeResponse ConnectionType(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public ConnectionTypeResponse ConnectionType(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public CountryResponse Country(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public CountryResponse Country(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public DomainResponse Domain(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public DomainResponse Domain(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public EnterpriseResponse Enterprise(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public EnterpriseResponse Enterprise(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public IspResponse Isp(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public IspResponse Isp(string ipAddress)
    {
        throw new NotImplementedException();
    }

    public bool TryAnonymousIP(IPAddress ipAddress, out AnonymousIPResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryAnonymousIP(string ipAddress, out AnonymousIPResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryAsn(IPAddress ipAddress, out AsnResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryAsn(string ipAddress, out AsnResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryCity(IPAddress ipAddress, out CityResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryCity(string ipAddress, out CityResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryConnectionType(IPAddress ipAddress, out ConnectionTypeResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryConnectionType(string ipAddress, out ConnectionTypeResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryCountry(IPAddress ipAddress, out CountryResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryCountry(string ipAddress, out CountryResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryDomain(IPAddress ipAddress, out DomainResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryDomain(string ipAddress, out DomainResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterprise(IPAddress ipAddress, out EnterpriseResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterprise(string ipAddress, out EnterpriseResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryIsp(IPAddress ipAddress, out IspResponse? response)
    {
        throw new NotImplementedException();
    }

    public bool TryIsp(string ipAddress, out IspResponse? response)
    {
        throw new NotImplementedException();
    }
}
