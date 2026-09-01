using System;
using System.Linq;
using System.Threading.Tasks;
using Monody.Services.Geocode;
using Monody.Services.Geocode.Models;

namespace Monody.AI.Tools.Tests;

/// <summary>
/// Stands in for HERE. Only the handful of places the tests name resolve, so a lookup the
/// resolver should not have needed shows up as a failure rather than a silent network call.
/// </summary>
internal sealed class StubGeocodeService : GeocodeService
{
    private static readonly (string Query, double Lat, double Lon)[] _known =
    [
        ("raleigh, nc", 35.7796, -78.6382),
        ("springfield, il", 39.7817, -89.6501),
        ("phoenix, az", 33.4484, -112.0740)
    ];

    public StubGeocodeService() : base(null, null, null)
    {
    }

    public override Task<LocationDetails> GetGeocodeForLocationStringAsync(string locationQuery)
    {
        var match = _known.FirstOrDefault(k => k.Query.Equals(locationQuery?.Trim(), StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match.Query is null
            ? null
            : new LocationDetails { Coordinates = new Coordinates { Latitude = match.Lat, Longitude = match.Lon } });
    }
}
