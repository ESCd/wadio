ARG ENVIRONMENT="Development"
ARG VERSION="1.0.0-rc.1"

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-amd64 AS runtime

ENV APPDIR="/wadio-web"

RUN apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && apt-get autoremove --purge && apt-get clean && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/* && \
    mkdir -p "${APPDIR}"

FROM ghcr.io/escd/dotnet-wasm-sdk:10.0-noble-aot-amd64 AS source

ENV DOTNET_NOLOGO=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR "/workspace"

COPY ["./wadio.slnx", "."]
COPY ["./Directory.Build.*", "."]
COPY ["./.editorconfig", "."]
COPY ["./global.json", "."]
COPY ["./src", "./src"]

WORKDIR "/workspace/src/Platform/Web/src"

RUN dotnet restore --locked-mode

FROM source AS build

ARG VERSION

RUN dotnet publish Wadio.Platform.Web.csproj -o /wadio-web -c Release -r linux-x64 --self-contained --no-restore -p:DebugType=none -p:StripSymbols=true -p:MinVerSkip=true -p:InformationalVersion=${VERSION} && dotnet clean -c Release

FROM runtime AS wadio-web

ARG ENVIRONMENT

COPY --from=build /wadio-web "${APPDIR}"

ENV ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR ${APPDIR}
ENTRYPOINT [ "./Wadio.Platform.Web" ]