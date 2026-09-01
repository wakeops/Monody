# AGENTS.md

Monody is a C# Discord bot: slash commands for weather (`/weather now|hourly|week`)
and an LLM bridge (`/slop ask|image`) built on Semantic Kernel and OpenAI.

## Commands

```bash
dotnet build -c Release          # Release is the default Configuration (Directory.Build.props)
dotnet test  -c Release          # xunit; one test project
dotnet publish src/Monody.Bot/Monody.Bot.csproj -c Release -o ./out
```

CI runs exactly `restore` → `build` → `test` → `docker build`, so a clean local
build plus tests is a good proxy for green CI.

Targets **net10.0**. If the SDK is missing on a Linux box, the Microsoft download
CDN is often blocked but Ubuntu 24.04 ships it: `apt-get install -y dotnet-sdk-10.0`.

## Layout

```
src/Monody.Domain      Shared DI/options helpers. No project dependencies.
src/Monody.Services    External APIs: geocoding (HERE), weather (Pirate Weather),
                       web search (Google CSE), Bluesky. Caching lives here.
src/Monody.AI.Tools    Semantic Kernel plugins (the tools the model can call).
src/Monody.AI          Kernel/OpenAI wiring, system prompts, the research agent,
                       and the structured-output JSON Schema generator.
src/Monody.Bot         Host, Discord wiring, interaction modules. The entrypoint.
test/…AI.Tools.Tests   Only covers HTML content extraction today.
```

Dependencies flow one way: `Bot → AI → AI.Tools → Services → Domain`.

`Monody.AI.Tools` must not reference `Monody.AI`. `ResearchAssistantPlugin` needs
the research agent, which needs the `Kernel` the plugin is being registered on —
so `IResearchAgent` is declared in `AI.Tools/Abstractions` and implemented in
`Monody.AI`, and the plugin resolves it from `IServiceProvider` at call time.
Do not "clean this up" into constructor injection; it reintroduces the cycle.

## Running locally

Config is `appsettings.json` → `appsettings.{Environment}.json` (gitignored) →
environment variables. Env keys use `__` for nesting: `Services__Geocode__HereApiKey`.

`appsettings.json` ships every secret as `""`, so you must override them. Only
three are needed to reach startup — **verified by running the published bot**:

| Setting | Needed to start? |
| --- | --- |
| `Discord:Token` | Yes — Discord.Addons.Hosting validates the token *format* |
| `Services:Geocode:HereApiKey` | Yes — the HERE SDK rejects an empty key at registration |
| `AIOptions:Providers:OpenAI:ApiKey` | Yes — Semantic Kernel rejects an empty key |
| `Services:Weather:PirateWeatherApiKey` | No — fails later, when a command runs |
| `Services:WebSearch:GoogleApiKey` / `GoogleSearchEngineId` | No — fails later |
| `Cache:RedisConfiguration` | No — optional; set it to enable the FusionCache backplane |

**Gotcha:** the `[Required]` attributes on the options classes are *not* enforced.
`ApplyValidatedOptions` calls `AddOptionsWithValidateOnStart` but never
`.ValidateDataAnnotations()`, so no validator is registered. What actually stops
startup is each SDK rejecting an empty key, with a message that doesn't name the
setting (e.g. `ArgumentException: The API key cannot be null or empty`). If you
hit that, it is a missing config value, not a DI bug.

A well-formed but fake token gets you through the whole DI graph to
`Gateway: Connecting` before failing on Discord auth — a useful smoke test that
startup wiring is intact.

## Adding things

**A slash command.** Add an `InteractionModule : InteractionModuleBase<SocketInteractionContext>`
under `src/Monody.Bot/Modules/<Area>/`. It must live in `Monody.Bot`:
`ModuleLoaderService` only scans the executing assembly. If the module needs its
own services, add an `InjectionHandler : ModuleInjectionHandler` beside it —
`AddModulesFromAssembly` finds every such type by reflection and calls it. There
is no registry or manifest to update.

**A tool the model can call.** Add the plugin plus its request/response types under
`src/Monody.AI.Tools/Capabilities/<Name>/`, then register it in
`Monody.AI/ServiceCollectionExtensions.AddMonodyAI` (`Plugins.AddFromType<T>()`).
Its dependencies must be resolvable from the container.

**A config-backed service.** Use `services.ApplyValidatedOptions<T>(configuration, "Section:Path")`,
which binds the options and hands back an instance for use during registration.

## Conventions

Enforced by `.editorconfig` (several rules are warnings or errors — read it before
fighting the analyzer):

- Private fields are `_camelCase` — including `private static readonly`.
- Unused usings (IDE0005), dead assignments (IDE0052/0059), namespace/folder
  mismatch (IDE0130) and "can be static" (CA1822) are warnings; CA2000 (dispose)
  is an error.
- `ImplicitUsings` and nullable reference types are **off**. Every file writes its
  own `using System;` etc., and `?` annotations are not checked.
- File-scoped namespaces, 4-space indent, `System` usings sorted first.

Package versions are managed centrally: add a `<PackageVersion>` to
`Directory.Packages.props` and a bare `<PackageReference Include="..." />` (no
`Version`) to the csproj. A `Version` in a csproj is an error under central
management.

UTF-8 BOMs are inconsistent across files (about 60% have one), and `.editorconfig`
asks for CRLF while the working tree is LF with no `.gitattributes`. Both are
accidents rather than conventions — leave them alone unless you are fixing them
deliberately, since normalising in passing turns a small diff into a whole-file one.

## Things that have bitten before

**Discord's 3-second acknowledgement window.** An interaction must be acknowledged
within 3s or it dies with `Unknown interaction (10062)`. Every command calls
`DeferAsync()` first and then `ModifyOriginalResponseAsync` / `FollowupAsync`.
Do not do slow work before the defer. `Program.cs` raises `ThreadPool.SetMinThreads`
for the same reason, and `UseInteractionSnowflakeDate = false` stops Discord.Net
from measuring that window against its own clock. Don't remove either.

**Structured outputs are strict.** `StructuredOutputSchema` generates the schema
OpenAI receives, and strict mode means: every object gets `additionalProperties: false`,
*every* property must appear in `required` (optionality is expressed through the
property's type, not by omission), and `$ref` may not have sibling keywords — so
descriptions and constraints are only applied to inline schemas. If you change
this generator, diff the generated JSON before and after; it is easy to produce
something the API rejects at request time rather than at build time.

**`System.Text.Json` ignores public fields.** Tool response types must use
properties. A `public List<T> Messages = [];` field silently serialised to nothing
and the model never saw the data.

**SmartReader's `Reader.Dispose()` tears down the document it was given.** Anything
that needs the document after the reader is disposed must parse its own copy;
`HtmlContentExtractor` does this deliberately.

**Weather is always fetched from the API in US units** and converted on the way out
(`WeatherService.ConvertTempUnit`). Pirate Weather also returns some
DarkSkyCore-integer fields as fractional numbers, which
`DarkSkyJsonSerializerService` truncates before deserialisation.

**The model sometimes calls the weather tool with a bare string** instead of the
request object. `WeatherRequestCoercionFilter` (an `IFunctionInvocationFilter`)
fixes that up; it is a real workaround, not dead code.

**Component custom IDs carry state.** Buttons encode their arguments in the ID
(e.g. `forecast_hourly_{page}_({location})_{unit}`) and are matched by the wildcard
pattern on `[ComponentInteraction]`. Changing the format on one side without the
other breaks paging silently.

## CI and release

- `01-build-test.yaml` on push to `main`: build, test, Docker build, then auto-tag
  and create a release via shared workflows in `wakeops/ci`.
- `02-publish-on-release.yaml`: publishes the image when a release is published.
- Existing warnings are expected: `NU1902` (AngleSharp advisory) and `SKEXP0001` /
  `SKEXP0010` (Semantic Kernel experimental APIs, deliberately kept as warnings in
  `.editorconfig`). Don't add suppressions to "fix" them; do avoid adding new ones.
