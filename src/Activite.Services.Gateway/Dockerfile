FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/Activite.Services.Gateway/Activite.Services.Gateway.csproj", "src/Activite.Services.Gateway/"]
RUN dotnet restore "src/Activite.Services.Gateway/Activite.Services.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Activite.Services.Gateway"
RUN dotnet build "Activite.Services.Gateway.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Activite.Services.Gateway.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Activite.Services.Gateway.dll"]
