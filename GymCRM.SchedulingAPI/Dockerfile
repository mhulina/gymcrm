FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8082
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
