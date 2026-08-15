FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5103

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["API/NeverMissLead.API.csproj", "API/"]
COPY ["Application/NeverMissLead.Application.csproj", "Application/"]
COPY ["Domain/NeverMissLead.Domain.csproj", "Domain/"]
COPY ["Infrastructure/NeverMissLead.Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "API/NeverMissLead.API.csproj"

COPY . .
# Publish using the already-restored packages; --no-restore is intentional after COPY .
# Use a nuget.config to force package cache so analyzers are always available
RUN dotnet publish "API/NeverMissLead.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NeverMissLead.API.dll"]

