#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WarshipEnrichment/WarshipEnrichment.csproj", "WarshipEnrichment/"]
RUN dotnet nuget add source "https://nuget.pkg.github.com/CombatWarships/index.json" --name WarshipPackages --username __USERNAME__ --password __TOKEN__ --store-password-in-clear-text
RUN dotnet restore "WarshipEnrichment/WarshipEnrichment.csproj"
COPY . .
WORKDIR "/src/WarshipEnrichment"
RUN dotnet build "WarshipEnrichment.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WarshipEnrichment.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WarshipEnrichment.dll"]