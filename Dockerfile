# -- Stage 1: Build Environment --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the entire project into the container (respects .dockerignore)
COPY . .

# Publish the Application project directly for Linux x64.
# We bypass publish.sh / BuildTool because:
#   - publish.sh tries to download a native build-tool binary from GitHub releases
#   - It falls back to `dotnet run --project BuildTool.csproj` which runs prerequisite
#     checks, `dotnet tool restore`, and schema generation — all unnecessary in Docker
#   - The BuildTool staleness check requires git history which is excluded by .dockerignore
RUN dotnet publish Projects/Application/Application.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false

# Also publish the WebPortal project to include its DLL and dependencies
RUN dotnet publish Projects/WebPortal/WebPortal.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false \
    -o /webportal-publish

# -- Stage 2: Runtime Environment --
# Using aspnet image since we use ASP.NET Core (Kestrel) for the web portal
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    libicu-dev \
    libdeflate-dev \
    zstd \
    libargon2-dev \
    liburing-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the published Distribution folder from the build stage
COPY --from=build /src/Distribution .

# Copy WebPortal outputs (DLL and wwwroot)
COPY --from=build /webportal-publish/WebPortal.dll ./Assemblies/WebPortal.dll
COPY --from=build /src/Projects/WebPortal/wwwroot ./wwwroot

# Expose both the game server port and the web portal port
EXPOSE 2593
EXPOSE 8080

# The main executable
ENTRYPOINT ["dotnet", "ModernUO.dll"]
