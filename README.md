# Monody

[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/wakeops/monody/build-test.yml?branch=main&style=for-the-badge)](https://github.com/wakeops/monody/actions/workflows/build-test.yml)
[![Latest Release](https://img.shields.io/github/v/release/wakeops/monody?style=for-the-badge)](https://github.com/wakeops/monody/releases/latest)
[![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

Monody is a Discord bot written in C#. It provides weather forecasts and an AI assistant
that can use tools (web search, page fetching, weather, reminders, per-user memory and
more). It is built on [Semantic Kernel](https://github.com/microsoft/semantic-kernel) and OpenAI.

The bot can be installed on servers (guilds) and on individual users, so its commands work
in DMs and in servers where the bot is not a member.

## Usage

### Weather

| Command | Description |
| --- | --- |
| `/weather now` | Current conditions for a location |
| `/weather hourly` | Hourly forecast, with paging buttons |
| `/weather week` | Weekly forecast |

Each command takes a `location` (any place name) and an optional `units` choice.

### AI assistant

| Command | Description |
| --- | --- |
| `/slop ask` | Ask the assistant a question. Replies can be followed up on, and the conversation survives restarts. |
| `/slop memories` | View and delete what the assistant remembers about you. |

During a conversation the assistant can call these tools:

- **Web:** search the web, fetch and read a URL, read Bluesky posts, and delegate to a research sub-agent.
- **Info:** current weather, geocoding, current time in a time zone, and a calculator.
- **Discord:** read a message or recent channel history.
- **Personal:** remember and forget facts about you, and set and list reminders.
- **Graylog:** list streams and search logs (only when Graylog is configured).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build and run from source, or Docker to run the published image.
- A Discord application and bot token.
- API keys for the external services below.

| Service | Used for | Setting |
| --- | --- | --- |
| [Discord](https://discord.com/developers/applications) | Bot token | `Discord:Token` |
| [HERE](https://developer.here.com/) | Geocoding | `Services:Geocode:HereApiKey` |
| [Pirate Weather](https://pirateweather.net/) | Weather data | `Services:Weather:PirateWeatherApiKey` |
| [Google Custom Search](https://programmablesearchengine.google.com/) | Web search | `Services:WebSearch:GoogleApiKey`, `Services:WebSearch:GoogleSearchEngineId` |
| [OpenAI](https://platform.openai.com/) | Chat completions | `AIOptions:Providers:OpenAI:ApiKey` |

## Configuration

Configuration is read from `appsettings.json`, then `appsettings.{Environment}.json`
(gitignored), then environment variables. In environment variables, use `__` for nesting,
for example `Services__Geocode__HereApiKey`.

| Setting | Required | Description |
| --- | --- | --- |
| `Discord:Token` | Yes | Discord bot token |
| `Services:Geocode:HereApiKey` | Yes | HERE API key |
| `Services:Weather:PirateWeatherApiKey` | Yes | Pirate Weather API key |
| `Services:WebSearch:GoogleApiKey` | Yes | Google Custom Search API key |
| `Services:WebSearch:GoogleSearchEngineId` | Yes | Google Programmable Search Engine ID |
| `AIOptions:Providers:OpenAI:ApiKey` | Yes | OpenAI API key |
| `Data:ConnectionString` | No | SQLite connection string. Defaults to `Data Source=/data/monody.db` |
| `Cache:RedisConfiguration` | No | Redis connection string. Enables the shared cache backplane |
| `Services:Graylog:BaseUrl` / `Services:Graylog:ApiKey` | No | Enables the Graylog tools |

The bot refuses to start if a required value is missing, and the error names the setting,
for example `Services:Geocode:HereApiKey - The HereApiKey field is required.`

## Running

### Docker

```bash
docker build -t monody .
docker run -d \
  -v monody-data:/data \
  -e Discord__Token=... \
  -e Services__Geocode__HereApiKey=... \
  -e Services__Weather__PirateWeatherApiKey=... \
  -e Services__WebSearch__GoogleApiKey=... \
  -e Services__WebSearch__GoogleSearchEngineId=... \
  -e AIOptions__Providers__OpenAI__ApiKey=... \
  monody
```

Mount `/data`. The SQLite database holds conversations, memories and reminders and is
migrated on startup, so without a volume everything is lost when the container is replaced.

### From source

```bash
dotnet build -c Release
dotnet test  -c Release
dotnet run --project src/Monody.App -c Release
```

Put your settings in `src/Monody.App/appsettings.Development.json` (or set environment
variables) and set `Data:ConnectionString` to a writable local path, since the default
points at `/data`.

## License

[MIT](LICENSE)
