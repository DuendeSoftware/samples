FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FrontendHost/FrontendHost.csproj", "FrontendHost/"]
RUN dotnet restore "FrontendHost/FrontendHost.csproj"
COPY . .
WORKDIR "/src/FrontendHost"
RUN dotnet build "FrontendHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FrontendHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY ContainerizedIdentityServer/wwwroot /app/wwwroot

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FrontendHost.dll"]
