using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Monody.AI.Tools;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Xunit;

namespace Monody.AI.Tools.Tests;

/// <summary>
/// Exercises the filter through a real Kernel, since the point of it is what happens on the
/// invocation path - argument binding and exception propagation - rather than in isolation.
/// </summary>
public class ToolCallRecoveryFilterTests
{
    private static Kernel BuildKernel()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);

        var plugins = services.AddKernel().Plugins;
        plugins.AddFromType<CurrentTimePlugin>();
        plugins.AddFromType<TwoFieldDemoPlugin>();
        plugins.AddFromType<NoRequiredFieldsDemoPlugin>();
        plugins.AddFromType<EnumAndStringDemoPlugin>();

        services.AddSingleton<IFunctionInvocationFilter, ToolCallRecoveryFilter>();

        return services.BuildServiceProvider().GetRequiredService<Kernel>();
    }

    [Fact]
    public async Task CoercesABareStringIntoTheRequestObject()
    {
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("CurrentTimePlugin", "current_time");

        // What the model sends when it skips the request wrapper entirely.
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

    [Fact]
    public async Task DoesNotReWrapAnArgumentThatIsAlreadyValidJson()
    {
        // The reported bug: the model sent {"request": "{\"TimeZone\":\"America/New_York\"}"} -
        // the request argument was itself the serialized object as a string, and the old
        // coercion logic stuffed that whole string into the TimeZone field because it only
        // checked "is this a string", not "is this already the object, just JSON-encoded".
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("CurrentTimePlugin", "current_time");

        var result = await kernel.InvokeAsync(function, new KernelArguments
        {
            ["request"] = "{\"TimeZone\":\"America/New_York\"}"
        });

        Assert.Equal("America/New_York", result.GetValue<CurrentTimeToolResponse>().TimeZone);
    }

    [Fact]
    public async Task RecoversFromAnInvalidValueInsteadOfCrashingTheCompletion()
    {
        // ResolveTimeZone's own refusal is an ArgumentException aimed at the model. Without the
        // filter it propagates out of GetChatMessageContentsAsync and fails the whole /slop ask
        // command; with it, the message becomes the tool's result so the model can retry.
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("CurrentTimePlugin", "current_time");

        var result = await kernel.InvokeAsync(function, new KernelArguments { ["request"] = "Raleigh, NC" });

        var message = result.GetValue<string>();
        Assert.Contains("Raleigh, NC", message);
        Assert.Contains("America/New_York", message);
    }

    [Fact]
    public async Task RecoversFromABareStringOnARequestWithMoreThanOneField()
    {
        // The other reported bug: remember has two required fields (Category, Content), so a
        // bare string can never be safely guessed into place - there is no coercion map entry
        // for it, and there should not be one. The filter still has to stop this from crashing
        // the completion; it just can't fix the call, only report why.
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("TwoFieldDemoPlugin", "two_field_demo");

        var result = await kernel.InvokeAsync(function, new KernelArguments { ["request"] = "just a bare string" });

        var message = result.GetValue<string>();
        Assert.Contains("TwoFieldDemoRequest", message);
    }

    [Fact]
    public async Task RecoversFromTheToolsOwnGuardClauses()
    {
        // ArgumentNullException derives from ArgumentException, so a plugin's own
        // ArgumentNullException.ThrowIfNull / ArgumentException.ThrowIfNullOrWhiteSpace guards -
        // used throughout the other plugins for missing required fields - are covered the same
        // way, without each plugin needing to catch and translate its own exceptions.
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("TwoFieldDemoPlugin", "two_field_demo");

        var result = await kernel.InvokeAsync(function, new KernelArguments
        {
            ["request"] = new TwoFieldDemoRequest { First = "", Second = "b" }
        });

        Assert.Contains("First", result.GetValue<string>());
    }

    [Fact]
    public async Task SelfDeserializesAWellFormedJsonArgumentContainingAnEnum()
    {
        // The reported bug: remember's RememberToolRequest {Category, Content} has an enum
        // property. Semantic Kernel's own fallback string-to-object conversion cannot bind that
        // shape - it throws ArgumentException("Object of type 'System.String' cannot be converted
        // to type '...'") even though the JSON is well-formed, because the argument arrives as a
        // string rather than already deserialized. The old IsJsonObject check left such strings
        // alone, trusting Semantic Kernel to convert them, so the exception propagated to this
        // filter's catch block, and the model read the resulting message as a success ("I've saved
        // that you live in Raleigh NC") even though the plugin method never ran. This proves the
        // filter now deserializes the argument itself instead of relying on that fallback.
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("EnumAndStringDemoPlugin", "enum_and_string_demo");

        var result = await kernel.InvokeAsync(function, new KernelArguments
        {
            ["request"] = "{\"Category\":\"Location\",\"Content\":\"Lives in Raleigh, NC\"}"
        });

        Assert.Equal("Location:Lives in Raleigh, NC", result.GetValue<string>());
    }

    [Fact]
    public async Task SelfDeserializationIsCaseInsensitiveForEnumValues()
    {
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("EnumAndStringDemoPlugin", "enum_and_string_demo");

        var result = await kernel.InvokeAsync(function, new KernelArguments
        {
            ["request"] = "{\"category\":\"location\",\"content\":\"lowercase everything\"}"
        });

        Assert.Equal("Location:lowercase everything", result.GetValue<string>());
    }

    [Fact]
    public async Task RecoversWhenTheModelOmitsARequestThatHasNothingRequired()
    {
        // The reported bug: recall's request has one property, and it isn't required (there is
        // nothing to pass - recall always returns everything). A well-formed call can legally
        // supply no "request" argument at all, but Semantic Kernel still treats the parameter
        // itself as non-optional and throws KernelException("Missing argument for function
        // parameter 'request'") - a type this filter's earlier ArgumentException catch did not
        // match, since KernelException wraps an ArgumentException rather than being one.
        var kernel = BuildKernel();
        var function = kernel.Plugins.GetFunction("NoRequiredFieldsDemoPlugin", "no_required_fields_demo");

        var result = await kernel.InvokeAsync(function, new KernelArguments());

        Assert.Contains("request", result.GetValue<string>());
    }

    /// <summary>A minimal stand-in for a plugin whose request has more than one required field.</summary>
    private sealed class TwoFieldDemoRequest
    {
        public string First { get; set; }

        public string Second { get; set; }
    }

    private sealed class TwoFieldDemoPlugin
    {
        [KernelFunction("two_field_demo")]
        [Description("test-only plugin for exercising the recovery filter")]
        public string Run(TwoFieldDemoRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.First);

            return $"{request.First}:{request.Second}";
        }
    }

    /// <summary>Mirrors RecallToolRequest: a request whose one property is not required.</summary>
    private sealed class NoRequiredFieldsDemoRequest
    {
        public string Unused { get; set; }
    }

    private sealed class NoRequiredFieldsDemoPlugin
    {
        [KernelFunction("no_required_fields_demo")]
        [Description("test-only plugin for exercising the recovery filter")]
        public string Run(NoRequiredFieldsDemoRequest request) => "ok";
    }

    private enum DemoCategory
    {
        Name,
        Location,
        TimeZone,
        Preference
    }

    /// <summary>Mirrors RememberToolRequest: a string field alongside an enum field.</summary>
    private sealed class EnumAndStringDemoRequest
    {
        public DemoCategory Category { get; set; }

        public string Content { get; set; }
    }

    private sealed class EnumAndStringDemoPlugin
    {
        [KernelFunction("enum_and_string_demo")]
        [Description("test-only plugin for exercising the recovery filter")]
        public string Run(EnumAndStringDemoRequest request) => $"{request.Category}:{request.Content}";
    }
}
