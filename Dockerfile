# --- Restore Layer ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY *.sln ./
COPY Directory.Packages.props ./

COPY ./src/Monody.Bot/*.csproj ./src/Monody.Bot/

COPY *.sln ./
COPY Directory.Packages.props ./
COPY ./src/Monody.Bot/*.csproj ./src/Monody.Bot/

RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore --nologo

# --- Publish Layer ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/src/Monody.Bot/obj \
    --mount=type=cache,target=/src/Monody.Bot/bin \
    dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime Layer ---
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app
COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "Monody.Bot.dll"]
