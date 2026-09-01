using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
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
    public void GivesEachTopLevelGroupExactlyOneModule()
    {
        // Registration sends one payload per module, and Discord keys top-level commands by
        // name, so two modules sharing a [Group] means the second silently replaces the first.
        // The interaction tree merges them and looks fine, which is why this has to be checked
        // on the grouping rather than on the commands.
        var duplicated = _interactionService.Modules
            .Where(m => !string.IsNullOrEmpty(m.SlashGroupName))
            .GroupBy(m => m.SlashGroupName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(
            duplicated.Count == 0,
            $"These groups are declared by more than one module, so only the last registers: {string.Join(", ", duplicated)}");
    }

    [Fact]
    public void DeclaresUserInstallOnEveryCommand()
    {
        // Discord defaults integration_types to guild-install when it is omitted, which makes
        // every command unavailable to a user-installed app. Nothing surfaces that at runtime.
        foreach (var command in _interactionService.SlashCommands)
        {
            Assert.Contains(ApplicationIntegrationType.UserInstall, command.IntegrationTypes);
            Assert.Contains(ApplicationIntegrationType.GuildInstall, command.IntegrationTypes);
        }
    }

    [Fact]
    public void AllowsEveryContextAUserInstalledAppCanReach()
    {
        // A user-installed app is used in DMs and in guilds the bot is not a member of.
        foreach (var command in _interactionService.SlashCommands)
        {
            Assert.Contains(InteractionContextType.BotDm, command.ContextTypes);
            Assert.Contains(InteractionContextType.PrivateChannel, command.ContextTypes);
            Assert.Contains(InteractionContextType.Guild, command.ContextTypes);
        }
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
