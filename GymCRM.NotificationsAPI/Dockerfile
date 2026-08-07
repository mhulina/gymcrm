FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GymCRM.NotificationsAPI/GymCRM.NotificationsAPI.csproj", "GymCRM.NotificationsAPI/"]
RUN dotnet restore "GymCRM.NotificationsAPI/GymCRM.NotificationsAPI.csproj"
COPY . .
WORKDIR "/src/GymCRM.NotificationsAPI"
RUN dotnet build "GymCRM.NotificationsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GymCRM.NotificationsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymCRM.NotificationsAPI.dll"]
