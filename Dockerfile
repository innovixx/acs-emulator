FROM node:22-bookworm-slim AS ui-build
WORKDIR /src/ui
COPY AcsEmulator/acs-emulator-ui/package*.json ./
RUN npm ci
COPY AcsEmulator/acs-emulator-ui/ ./
ARG REACT_APP_API_URL=http://localhost:8080
ENV REACT_APP_API_URL=${REACT_APP_API_URL}
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY AcsEmulator/NuGet.config ./NuGet.config
COPY AcsEmulator/AcsEmulatorAPI/AcsEmulatorAPI.csproj AcsEmulatorAPI/
RUN dotnet restore AcsEmulatorAPI/AcsEmulatorAPI.csproj
COPY AcsEmulator/AcsEmulatorAPI/ AcsEmulatorAPI/
RUN dotnet publish AcsEmulatorAPI/AcsEmulatorAPI.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish ./
COPY --from=ui-build /src/ui/build ./wwwroot/
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8025 \
    ASPNETCORE_WEBROOT=/app/wwwroot \
    DisableHttpsRedirection=true \
    ConnectionStrings__EmulatorDb="Data Source=/data/AcsEmulator.db" \
    EventGridSimulatorSystemTopicHostname="" \
    EventGridSimulatorSystemTopicCredentials=""
RUN mkdir /data && chown app:app /data
USER app
EXPOSE 8025
ENTRYPOINT ["dotnet", "AcsEmulatorAPI.dll"]