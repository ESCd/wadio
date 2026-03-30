ARG ENVIRONMENT="Development"
ARG VERSION="1.0.0-rc.1"

FROM debian:bookworm-slim AS libdave

RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates wget unzip \
  && wget https://github.com/discord/libdave/releases/download/v1.1.1%2Fcpp/libdave-Linux-X64-boringssl.zip \
  && unzip libdave-Linux-X64-boringssl.zip -d /libdave \
  && rm libdave-Linux-X64-boringssl.zip \
  && apt-get remove -y --no-install-recommends ca-certificates wget unzip \
  && apt-get autoremove --purge -y && apt-get clean -y && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/*

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-amd64 AS runtime

ENV APPDIR="/wadio-bot"

RUN apt-get update \
  && apt-get install -y --no-install-recommends ffmpeg libopus0 libopus-dev libsodium23 libsodium-dev \
  && apt-get autoremove --purge -y && apt-get clean -y && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/* \
  && mkdir -p "${APPDIR}"

COPY --from=libdave ["/libdave/lib", "/usr/lib/x86_64-linux-gnu"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot-amd64 AS sdk

ENV DOTNET_NOLOGO=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV WADIO_BUILD_IN_CONTAINER=true

RUN apt-get update \
  && apt-get install -y --no-install-recommends libopus0 libopus-dev libsodium23 libsodium-dev \
  && apt-get autoremove --purge -y && apt-get clean -y && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/*

COPY --from=libdave ["/libdave/lib", "/usr/lib/x86_64-linux-gnu"]

FROM sdk AS source

COPY ["./wadio.slnx", "."]
COPY ["./Directory.Build.*", "."]
COPY ["./.editorconfig", "."]
COPY ["./global.json", "."]
COPY ["./src", "/src"]

WORKDIR "/src/Platform/Discord"

RUN cp -f /usr/lib/x86_64-linux-gnu/libdave.* \
  /usr/lib/x86_64-linux-gnu/libopus.* \
  /usr/lib/x86_64-linux-gnu/libsodium.* \
  lib/linux-x64

RUN dotnet restore --locked-mode

FROM source AS build

ARG VERSION

RUN dotnet publish /src/Platform/Discord/Wadio.Platform.Discord.csproj -o /wadio-bot -c Release -r linux-x64 --self-contained --no-restore -p:DebugType=none -p:StripSymbols=true -p:MinVerSkip=true -p:InformationalVersion=${VERSION}

FROM runtime AS wadio-bot

ARG ENVIRONMENT

COPY --from=build ["/wadio-bot", "${APPDIR}"]

ENV ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR ${APPDIR}
ENTRYPOINT [ "./Wadio.Platform.Discord" ]