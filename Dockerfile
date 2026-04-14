# =============================================================================
# ModernUO + WebPortal + CommanderApi — Standalone Docker Build
#
# This Dockerfile clones upstream ModernUO, injects the WebPortal and
# CommanderApi projects, patches the build configuration, and produces
# a lean runtime image.
#
# Build context needs: Projects/WebPortal/ and Projects/CommanderApi/
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

# Copy CommanderApi project from build context
COPY Projects/CommanderApi/ Projects/CommanderApi/

# --- Patch build configuration to include WebPortal ---

# 1. Application.csproj: add ASP.NET Core framework reference after </PropertyGroup>
RUN sed -i '/<\/PropertyGroup>/a\    <ItemGroup>\n        <FrameworkReference Include="Microsoft.AspNetCore.App" />\n    </ItemGroup>' \
    Projects/Application/Application.csproj

# 2. Application.csproj: add WebPortal project reference before </ItemGroup>
RUN sed -i '/<\/ItemGroup>/i\        <ProjectReference Include="..\\WebPortal\\WebPortal.csproj" />' \
    Projects/Application/Application.csproj

# 3. Application.csproj: add CommanderApi project reference before </ItemGroup>
RUN sed -i '/<\/ItemGroup>/i\        <ProjectReference Include="..\\CommanderApi\\CommanderApi.csproj" />' \
    Projects/Application/Application.csproj

# 4. assemblies.json: register WebPortal.dll and CommanderApi.dll for runtime assembly loading
RUN sed -i 's/"UOContent.dll"/"UOContent.dll",\n  "WebPortal.dll",\n  "CommanderApi.dll"/' \
    Distribution/Data/assemblies.json

# 5. ModernUO.slnx: add WebPortal and CommanderApi to solution
RUN sed -i '/UOContent\/UOContent.csproj/a\  <Project Path="Projects/WebPortal/WebPortal.csproj" />\n  <Project Path="Projects/CommanderApi/CommanderApi.csproj" />' \
    ModernUO.slnx

# Verify patches applied correctly
RUN echo "=== Verifying patches ===" && \
    grep -q "Microsoft.AspNetCore.App" Projects/Application/Application.csproj && \
    echo "  ✓ FrameworkReference added" && \
    grep -q "WebPortal.csproj" Projects/Application/Application.csproj && \
    echo "  ✓ WebPortal ProjectReference added" && \
    grep -q "CommanderApi.csproj" Projects/Application/Application.csproj && \
    echo "  ✓ CommanderApi ProjectReference added" && \
    grep -q "WebPortal.dll" Distribution/Data/assemblies.json && \
    echo "  ✓ WebPortal.dll in assemblies.json" && \
    grep -q "CommanderApi.dll" Distribution/Data/assemblies.json && \
    echo "  ✓ CommanderApi.dll in assemblies.json" && \
    grep -q "WebPortal.csproj" ModernUO.slnx && \
    echo "  ✓ WebPortal in ModernUO.slnx" && \
    grep -q "CommanderApi.csproj" ModernUO.slnx && \
    echo "  ✓ CommanderApi in ModernUO.slnx"

# -- Stage 2: Build --
FROM source AS build

# Publish the main Application (also builds WebPortal and CommanderApi as dependencies)
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

# Separately publish CommanderApi to collect its DLL and NuGet dependencies
RUN dotnet publish Projects/CommanderApi/CommanderApi.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false \
    -o /commanderapi-publish

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

# Copy WebPortal assembly to Assemblies folder
COPY --from=build /webportal-publish/WebPortal.dll ./Assemblies/WebPortal.dll

# Copy CommanderApi assembly to Assemblies folder
COPY --from=build /commanderapi-publish/CommanderApi.dll ./Assemblies/CommanderApi.dll

# Copy WebPortal frontend files
COPY --from=build /src/Projects/WebPortal/wwwroot ./wwwroot

# Expose game server, web portal, and commander API ports
EXPOSE 2593
EXPOSE 8080
EXPOSE 8090

# ModernUO entry point
ENTRYPOINT ["dotnet", "ModernUO.dll"]
