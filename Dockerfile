# --- Build (restore + publish) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY ./src/Monody.Bot/*.csproj ./src/Monody.Bot/
COPY ./src/Monody.Data/*.csproj ./src/Monody.Data/
COPY ./src/Monody.Domain/*.csproj ./src/Monody.Domain/
COPY ./src/Monody.Services/*.csproj ./src/Monody.Services/

COPY ./src/Monody.AI/Monody.AI/*.csproj ./src/Monody.AI/Monody.AI/
COPY ./src/Monody.AI/Monody.AI.Tools/*.csproj ./src/Monody.AI/Monody.AI.Tools/

COPY ./test/Monody.AI.Tools.Tests/*.csproj ./test/Monody.AI.Tools.Tests/
COPY ./test/Monody.Bot.Tests/*.csproj ./test/Monody.Bot.Tests/
COPY ./test/Monody.Data.Tests/*.csproj ./test/Monody.Data.Tests/


RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore --nologo

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app/publish --no-restore ./src/Monody.Bot/Monody.Bot.csproj

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/publish ./

# The default connection string is Data Source=/data/monody.db. Create it so a plain
# `docker run` starts, and mount a volume here to keep memories and reminders across deploys.
RUN mkdir -p /data
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "Monody.Bot.dll"]
