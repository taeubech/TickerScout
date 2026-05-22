# Build stage for React frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ .
# Accept VITE_API_BASE_URL as build argument and set as environment variable for the build
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

# Build stage for .NET backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app

# Copy . NET project files
COPY backend/*.csproj ./TickerScoutApi/
WORKDIR /app/TickerScoutApi
RUN dotnet restore

# Copy remaining source code and build
WORKDIR /app
COPY backend/ ./TickerScoutApi/
WORKDIR /app/TickerScoutApi
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=backend-build /app/TickerScoutApi/out ./

# Copy frontend build to wwwroot
COPY --from=frontend-build /app/dist /app/wwwroot/

ARG PORT=8080


# Set environment variable for ASP.NET to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}


# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:${PORT:-8080}/health || exit 1

ENTRYPOINT ["dotnet", "TickerScout.Backend.dll"]