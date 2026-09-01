using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Monody.Bot.Tests;

/// <summary>
/// Builds the real interaction tree from the bot assembly. This is the user-visible command
/// surface, and a duplicate name or a malformed component pattern only shows up at startup
/// otherwise.
/// </summary>
public class InteractionModuleRegistrationTests : IAsyncLifetime
{
    private DiscordSocketClient _client;
    private InteractionService _interactionService;

    public async Task InitializeAsync()
    {
        _client = new DiscordSocketClient();
        _interactionService = new InteractionService(_client);

        await _interactionService.AddModulesAsync(typeof(Modules.MonodyConstants).Assembly, new StubServiceProvider());
    }

    /// <summary>
    /// Discord.Net instantiates each module while building the tree, so the constructor
    /// dependencies have to resolve to something. Nothing is ever invoked on them - only the
    /// attributes are read - so uninitialised instances are enough, and standing up the real
    /// graph (Kernel, HTTP clients, a database) would test something else entirely.
    /// </summary>
    private sealed class StubServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _logging = new ServiceCollection().AddLogging().BuildServiceProvider();

        public object GetService(Type serviceType)
        {
            if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                return _logging.GetService(serviceType);
            }

            return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(serviceType);
        }
    }

    public Task DisposeAsync()
    {
        _interactionService.Dispose();
        return _client.DisposeAsync().AsTask();
    }

    private string[] CommandPaths() =>
        [.. _interactionService.SlashCommands.Select(c => $"/{c.Module.SlashGroupName} {c.Name}".Trim())];

    [Theory]
    [InlineData("/weather now")]
    [InlineData("/weather hourly")]
    [InlineData("/weather week")]
    [InlineData("/slop ask")]
    [InlineData("/slop image")]
    [InlineData("/slop memories")]
    public void RegistersTheExpectedCommands(string path)
    {
        Assert.Contains(path, CommandPaths());
    }

    [Fact]
    public void SplitsTheSlopGroupAcrossModulesWithoutClashing()
    {
        // memories lives in its own module but shares the "slop" group, which Discord.Net
        // merges. A duplicate command name here would throw during AddModulesAsync.
        var slop = CommandPaths().Where(p => p.StartsWith("/slop ", StringComparison.Ordinal)).ToList();

        Assert.Equal(slop.Count, slop.Distinct().Count());
        Assert.Contains("/slop memories", slop);
    }

    [Fact]
    public void RegistersTheComponentAndModalHandlers()
    {
        var components = _interactionService.ComponentCommands.Select(c => c.Name).ToList();

        Assert.Contains("monody_followup:*:*", components);
        Assert.Contains("monody_memory_delete", components);
        Assert.Contains("monody_memory_delete_all", components);
        Assert.Contains("forecast_hourly_*_(*)_*", components);

        Assert.Contains("monody_followup_modal:*:*", _interactionService.ModalCommands.Select(c => c.Name));
    }
}
