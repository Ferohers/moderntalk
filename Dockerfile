# -- Stage 1: Build Environment --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install native dependencies + git for cloning
RUN apt-get update && apt-get install -y \
    libicu-dev \
    libdeflate-dev \
    zstd \
    libargon2-dev \
    liburing-dev \
    git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Clone upstream ModernUO — gets latest updates automatically on rebuild
RUN git clone https://github.com/modernuo/ModernUO.git .

# Copy our WebPortal project into the cloned source tree
COPY Projects/WebPortal/ Projects/WebPortal/

# Patch Application.csproj to include WebPortal:
#   1. Add FrameworkReference for ASP.NET Core (needed for Kestrel runtime)
#   2. Add ProjectReference to WebPortal project
RUN sed -i '/<ItemGroup>/a \    <FrameworkReference Include="Microsoft.AspNetCore.App" />' Projects/Application/Application.csproj \
    && sed -i '/<\/Project>/i \  <ItemGroup>\n    <ProjectReference Include="..\\WebPortal\\WebPortal.csproj" />\n  </ItemGroup>' Projects/Application/Application.csproj

# Build the complete application with WebPortal included
RUN dotnet publish Projects/Application/Application.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained=false

# -- Stage 2: Runtime Environment --
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

# Copy the published Distribution folder from build stage
# WebPortal.dll is in Assemblies/ and wwwroot/ is copied by the csproj build targets
COPY --from=build /src/Distribution .

# Expose both the game server port and the web portal port
EXPOSE 2593
EXPOSE 8080

# The main executable — AssemblyHandler auto-discovers WebPortal.dll in Assemblies/
ENTRYPOINT ["dotnet", "ModernUO.dll"]
