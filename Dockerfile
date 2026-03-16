# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WebApp/WebApp.csproj WebApp/
RUN dotnet restore WebApp/WebApp.csproj

COPY WebApp/ WebApp/
RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render.com sets PORT dynamically; ASP.NET Core respects ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}

EXPOSE 10000

ENTRYPOINT ["dotnet", "WebApp.dll"]
