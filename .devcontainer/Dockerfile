FROM node:22-bookworm-slim AS node

FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm AS server

COPY --from=node /usr/local/ /usr/local/

WORKDIR /workspaces/acs-emulator

CMD ["sh", "-c", "npm --prefix AcsEmulator/acs-emulator-ui ci && npm --prefix AcsEmulator/acs-emulator-ui run build && exec dotnet run --project AcsEmulator/AcsEmulatorAPI/AcsEmulatorAPI.csproj --no-launch-profile"]