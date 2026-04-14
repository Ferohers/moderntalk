# =============================================================================
# ModernUO + WebPortal + HTTP API — Standalone Docker Build
#
# This Dockerfile clones upstream ModernUO, injects the WebPortal project,
# adds the HTTP API module for UO Commander admin app,
# patches the build configuration, and produces a lean runtime image.
# =============================================================================

# -- Stage 1: Clone upstream ModernUO and inject projects --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS source

RUN apt-get update && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Clone upstream ModernUO (full clone needed by Nerdbank.GitVersioning)
ARG MODERNUO_REPO=https://github.com/modernuo/ModernUO.git
ARG MODERNUO_BRANCH=main
RUN git clone --branch ${MODERNUO_BRANCH} ${MODERNUO_REPO} .

# Copy WebPortal project from build context
COPY Projects/WebPortal/ Projects/WebPortal/

# Copy HTTP API module files (for UO Commander admin app)
COPY Projects/Server/HTTP/ Projects/Server/HTTP/

# --- Patch build configuration to include WebPortal ---

# 1. Application.csproj: add ASP.NET Core framework reference after </PropertyGroup>
RUN sed -i '/<\/PropertyGroup>/a\    <ItemGroup>\n        <FrameworkReference Include="Microsoft.AspNetCore.App" />\n    </ItemGroup>' \
    Projects/Application/Application.csproj

# 2. Application.csproj: add WebPortal project reference before </ItemGroup>
RUN sed -i '/<\/ItemGroup>/i\        <ProjectReference Include="..\\WebPortal\\WebPortal.csproj" />' \
    Projects/Application/Application.csproj

# 3. assemblies.json: register WebPortal.dll for runtime assembly loading
RUN sed -i 's/"UOContent.dll"/"UOContent.dll",\n  "WebPortal.dll"/' \
    Distribution/Data/assemblies.json

# 4. ModernUO.slnx: add WebPortal to solution
RUN sed -i '/UOContent\/UOContent.csproj/a\  <Project Path="Projects/WebPortal/WebPortal.csproj" />' \
    ModernUO.slnx

# Verify patches applied correctly
RUN echo "=== Verifying patches ===" && \
    grep -q "Microsoft.AspNetCore.App" Projects/Application/Application.csproj && \
    echo "  ✓ FrameworkReference added" && \
    grep -q "WebPortal.csproj" Projects/Application/Application.csproj && \
    echo "  ✓ ProjectReference added" && \
    grep -q "WebPortal.dll" Distribution/Data/assemblies.json && \
    echo "  ✓ assemblies.json patched" && \
    grep -q "WebPortal.csproj" ModernUO.slnx && \
    echo "  ✓ ModernUO.slnx patched" && \
    test -f Projects/Server/HTTP/HttpApiServer.cs && \
    echo "  ✓ HTTP API module files copied"

# -- Stage 2: Build --
FROM source AS build

# Publish the main Application (also builds WebPortal as a dependency)
RUN dotnet publish Projects/Application/Application.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false

# Separately publish WebPortal to collect its DLL and NuGet dependencies
RUN dotnet publish Projects/WebPortal/WebPortal.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false \
    -o /webportal-publish

# -- Stage 3: Runtime --
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install native runtime dependencies required by ModernUO
RUN apt-get update && apt-get install -y --no-install-recommends \
    libicu-dev \
    libdeflate-dev \
    zstd \
    libargon2-dev \
    liburing-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the published Distribution folder (ModernUO.dll, Data/, Assemblies/, etc.)
COPY --from=build /src/Distribution .

# Copy WebPortal assembly to Assemblies folder (ensures it's in the right place)
COPY --from=build /webportal-publish/WebPortal.dll ./Assemblies/WebPortal.dll

# Copy WebPortal frontend files
COPY --from=build /src/Projects/WebPortal/wwwroot ./wwwroot

# Copy HTTP API module assemblies (if built separately)
# Note: HttpApiServer.cs is compiled as part of Server.dll, no separate DLL needed

# Expose game server and web portal ports
EXPOSE 2593
EXPOSE 8080
EXPOSE 8081

# ModernUO entry point
ENTRYPOINT ["dotnet", "ModernUO.dll"]
