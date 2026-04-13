# -- Stage 1: Build Environment --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the entire project into the container (respects .dockerignore)
COPY . .

# Single publish of Application.csproj builds ALL projects transitively:
#   Server → Logger
#   Application → Server + UOContent + WebPortal
# WebPortal's NuGet deps (JWT packages) flow through because it's a normal
# ProjectReference in Application.csproj (not Private=false).
# We bypass publish.sh / BuildTool because:
#   - publish.sh tries to download a native build-tool binary from GitHub releases
#   - It falls back to `dotnet run --project BuildTool.csproj` which runs prerequisite
#     checks, `dotnet tool restore`, and schema generation — all unnecessary in Docker
#   - The BuildTool staleness check requires git history which is excluded by .dockerignore
RUN dotnet publish Projects/Application/Application.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false \
    -o /publish

# Copy game data files (these are source files in Distribution/Data, not build output)
RUN cp -r Distribution/Data /publish/Data

# Copy wwwroot static files for the Web Portal
# (the CopyWwwroot MSBuild target may not fire correctly with -r linux-x64)
RUN cp -r Projects/WebPortal/wwwroot /publish/wwwroot

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

# Copy the published output from the build stage
COPY --from=build /publish .

# Expose both the game server port and the web portal port
EXPOSE 2593
EXPOSE 8080

# The main executable
ENTRYPOINT ["dotnet", "ModernUO.dll"]
