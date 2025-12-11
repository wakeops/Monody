using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebSearchTool(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<WebSearchToolOptions>()
            .BindConfiguration("AIOptions:Tools:WebSearch");

        services.AddSingleton<CustomSearchAPIService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebSearchToolOptions>>().Value;

            var initializer = new BaseClientService.Initializer()
            {
                ApiKey = options.GoogleApiKey
            };

            return new CustomSearchAPIService(initializer);
        });

        services.AddTransient<GoogleSearchService>();

        return services;
    }
}
