# --- Build (restore + publish) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY ./src/Monody.Bot/*.csproj ./src/Monody.Bot/
COPY ./src/Monody.Domain/*.csproj ./src/Monody.Domain/

COPY ./src/Modules/Monody.Module.AIChat/*.csproj ./src/Modules/Monody.Module.AIChat/
COPY ./src/Modules/Monody.Module.Weather/*.csproj ./src/Modules/Monody.Module.Weather/

COPY ./src/Monody.AI/*.csproj ./src/Monody.AI/
COPY ./src/Monody.AI.Domain/*.csproj ./src/Monody.AI.Domain/
COPY ./src/Monody.AI.Provider/*.csproj ./src/Monody.AI.Provider/
COPY ./src/Monody.AI.Provider.OpenAI/*.csproj ./src/Monody.AI.Provider.OpenAI/
COPY ./src/Monody.AI.Tools/*.csproj ./src/Monody.AI.Tools/


RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore --nologo

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app/publish --no-restore ./src/Monody.Bot/Monody.Bot.csproj

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Monody.Bot.dll"]
