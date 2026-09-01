using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Monody.AI.Tools;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Xunit;

namespace Monody.AI.Tools.Tests;

/// <summary>
/// Exercises the filter through a real Kernel, since the point of it is what happens on the
/// invocation path rather than in isolation.
/// </summary>
public class BareStringRequestCoercionFilterTests
{
    private static Kernel BuildKernel()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new TimeZoneResolver(new StubGeocodeService()));
        services.AddKernel().Plugins.AddFromType<CurrentTimePlugin>();
        services.AddSingleton<IFunctionInvocationFilter, BareStringRequestCoercionFilter>();

        return services.BuildServiceProvider().GetRequiredService<Kernel>();
    }

    [Fact]
    public async Task CoercesABareStringIntoTheRequestObject()
    {
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("CurrentTimePlugin", "current_time");

        // What the model sends when it skips the request wrapper.
        var result = await kernel.InvokeAsync(function, new KernelArguments { ["request"] = "Asia/Tokyo" });

        Assert.Equal("Asia/Tokyo", result.GetValue<CurrentTimeToolResponse>().TimeZone);
    }

    [Fact]
    public async Task LeavesAProperRequestObjectAlone()
    {
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("CurrentTimePlugin", "current_time");

        var result = await kernel.InvokeAsync(function, new KernelArguments
        {
            ["request"] = new CurrentTimeToolRequest { TimeZone = "Asia/Tokyo" }
        });

        Assert.Equal("Asia/Tokyo", result.GetValue<CurrentTimeToolResponse>().TimeZone);
    }
}
