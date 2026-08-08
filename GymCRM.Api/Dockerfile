#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ENV ASPNETCORE_ENVIRONMENT=Production
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GymCRM.Api/GymCRM.Api.csproj", "GymCRM.Api/"]
COPY ["GymCRM.IdentityAPI/GymCRM.IdentityAPI.csproj", "GymCRM.IdentityAPI/"]
COPY ["GymCRM.SchedulingAPI/GymCRM.SchedulingAPI.csproj", "GymCRM.SchedulingAPI/"]
COPY ["GymCRM.Shared/GymCRM.Shared.csproj", "GymCRM.Shared/"]
RUN dotnet restore "./GymCRM.Api/GymCRM.Api.csproj"
COPY . .
WORKDIR "/src/GymCRM.Api"
RUN dotnet build "./GymCRM.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GymCRM.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymCRM.Api.dll"]
