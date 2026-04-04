ARG ENVIRONMENT="Development"
ARG VERSION="1.0.0-rc.1"

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-amd64 AS runtime

ENV APPDIR="/wadio-bot"

RUN apt-get update \
  && apt-get install -y --no-install-recommends ffmpeg \
  && apt-get autoremove --purge -y && apt-get clean -y && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/* \
  && mkdir -p "${APPDIR}"

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot-amd64 AS sdk

ENV DOTNET_NOLOGO=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

FROM sdk AS source

WORKDIR "/workspace"

COPY ["./wadio.slnx", "."]
COPY ["./Directory.Build.*", "."]
COPY ["./.editorconfig", "."]
COPY ["./global.json", "."]
COPY ["./src", "./src"]

WORKDIR "/workspace/src/Platform/Discord"

RUN dotnet restore --locked-mode

FROM source AS build

ARG VERSION

RUN dotnet publish Wadio.Platform.Discord.csproj -o /wadio-bot -c Release -r linux-x64 --self-contained --no-restore -p:DebugType=none -p:StripSymbols=true -p:MinVerSkip=true -p:InformationalVersion=${VERSION}

FROM runtime AS wadio-bot

ARG ENVIRONMENT

COPY --from=build ["/wadio-bot", "${APPDIR}"]

ENV ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR ${APPDIR}
ENTRYPOINT [ "./Wadio.Platform.Discord" ]