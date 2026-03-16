# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY WebDashboard.csproj .
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /data

COPY --from=build /app/publish .

# Render.com sets PORT; default to 8080 if not set
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DATA_DIR=/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebDashboard.dll"]
