# -- Stage 1: Build Environment --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install the required native dependencies
RUN apt-get update && apt-get install -y \
    libicu-dev \
    libdeflate-dev \
    zstd \
    libargon2-dev \
    liburing-dev \
    git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy local source code instead of cloning from GitHub
COPY . .

# Make the publish script executable and run it for Linux x64
RUN chmod +x publish.sh
RUN ./publish.sh release linux x64

# -- Stage 2: Runtime Environment --
# Changed from runtime to aspnet since we now use ASP.NET Core (Kestrel) for the web portal
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install the same native dependencies
RUN apt-get update && apt-get install -y \
    libicu-dev \
    libdeflate-dev \
    zstd \
    libargon2-dev \
    liburing-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the completely prepared Distribution folder from the build stage
COPY --from=build /src/Distribution .

# Expose both the game server port and the web portal port
EXPOSE 2593
EXPOSE 8080

# The main executable
ENTRYPOINT ["dotnet", "ModernUO.dll"]
