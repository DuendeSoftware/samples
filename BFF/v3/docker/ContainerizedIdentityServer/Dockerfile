FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ContainerizedIdentityServer/ContainerizedIdentityServer.csproj", "ContainerizedIdentityServer/"]
RUN dotnet restore "ContainerizedIdentityServer/ContainerizedIdentityServer.csproj"
COPY . .
WORKDIR "/src/ContainerizedIdentityServer"
RUN dotnet build "ContainerizedIdentityServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContainerizedIdentityServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN mkdir -p /app/keys 
RUN chown 1654:1654 /app/keys

USER 1654

ENTRYPOINT ["dotnet", "ContainerizedIdentityServer.dll"]
