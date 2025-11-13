# --- Build (restore + publish) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY ./src/Monody.Bot/*.csproj ./src/Monody.Bot/
COPY ./src/Monody.Domain/*.csproj ./src/Monody.Domain/
COPY ./src/Modules/Monody.Module.AIChat/*.csproj ./src/Modules/Monody.Module.AIChat/
COPY ./src/Modules/Monody.Module.Weather/*.csproj ./src/Modules/Monody.Module.Weather/

RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore --nologo

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Monody.Bot.dll"]
