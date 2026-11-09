FROM mcr.microsoft.com/devcontainers/base:jammy AS server

ARG USERNAME=vscode

RUN curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends dotnet-sdk-8.0 \
    && apt-get clean

ENV NVM_DIR=/home/${USERNAME}/.nvm

RUN su "${USERNAME}" -c "bash -o pipefail -c 'curl -fsSL https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.5/install.sh | bash'"
RUN su "${USERNAME}" -c "bash -c 'source ${NVM_DIR}/nvm.sh && nvm install 24 && nvm alias default 24 && nvm use 24'"
RUN su "${USERNAME}" -c "bash -c 'source ${NVM_DIR}/nvm.sh && npm install -g pnpm'"

WORKDIR /workspaces/acs-emulator

CMD ["bash", "-c", "source \"$NVM_DIR/nvm.sh\" && npm --prefix AcsEmulator/acs-emulator-ui ci && npm --prefix AcsEmulator/acs-emulator-ui run build && exec dotnet run --project AcsEmulator/AcsEmulatorAPI/AcsEmulatorAPI.csproj --no-launch-profile"]