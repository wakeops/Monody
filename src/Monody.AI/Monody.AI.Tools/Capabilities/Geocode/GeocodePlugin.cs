using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.Services.Geocode;

namespace Monody.AI.Tools.Capabilities.Geocode;

public sealed class GeocodePlugin(GeocodeService geocodeService)
{
    [KernelFunction("geocode_location")]
    [Description("Return the geocode, including latitude and longitude, for a given location")]
    public async Task<GeocodeToolResponse> GeocodeAsync(GeocodeToolRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentNullException(nameof(request.Location));
        }

        var response = await geocodeService.GetGeocodeForLocationStringAsync(request.Location);

        return new GeocodeToolResponse
        {
            Response = JsonSerializer.Serialize(response)
        };
    }
}
