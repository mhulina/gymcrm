FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GymCRM.MessagingAPI/GymCRM.MessagingAPI.csproj", "GymCRM.MessagingAPI/"]
RUN dotnet restore "GymCRM.MessagingAPI/GymCRM.MessagingAPI.csproj"
COPY . .
WORKDIR "/src/GymCRM.MessagingAPI"
RUN dotnet build "GymCRM.MessagingAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GymCRM.MessagingAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymCRM.MessagingAPI.dll"]
