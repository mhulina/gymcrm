FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GymCRM.SchedulingAPI/GymCRM.SchedulingAPI.csproj", "GymCRM.SchedulingAPI/"]
RUN dotnet restore "GymCRM.SchedulingAPI/GymCRM.SchedulingAPI.csproj"
COPY . .
WORKDIR "/src/GymCRM.SchedulingAPI"
RUN dotnet build "GymCRM.SchedulingAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GymCRM.SchedulingAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymCRM.SchedulingAPI.dll"]
