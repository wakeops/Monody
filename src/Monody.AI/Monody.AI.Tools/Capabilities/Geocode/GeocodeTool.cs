using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Tools.ToolHandler;
using Monody.Services.Geocode;

namespace Monody.AI.Tools.Capabilities.Geocode;

internal class GeocodeTool : ToolHandler<GeocodeToolRequest, GeocodeToolResponse>
{
    private readonly GeocodeService _geocodeService;

    public GeocodeTool(GeocodeService geocodeService)
    {
        _geocodeService = geocodeService;
    }

    public override string Name => "geocode_location";

    public override string Description => "Return the geocode, including latitude and longitude, for a given location";

    protected override async Task<GeocodeToolResponse> HandleAsync(GeocodeToolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentNullException(nameof(request.Location));
        }

        var response = await _geocodeService.GetGeocodeForLocationStringAsync(request.Location);

        return new GeocodeToolResponse
        {
            Response = JsonSerializer.Serialize(response)
        };
    }
}
