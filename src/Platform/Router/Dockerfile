ARG ENVIRONMENT="Production"
ARG VERSION="1.0.0-rc.1"

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ENV APPDIR="/wadio-router"

RUN apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && apt-get autoremove --purge && apt-get clean && rm -rf /var/lib/apt/lists/* && rm -rf /tmp/* && rm -rf /var/tmp/* && \
    mkdir -p "${APPDIR}"

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS source

ENV DOTNET_NOLOGO=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY ["./wadio.slnx", "."]
COPY ["./Directory.Build.*", "."]
COPY ["./.editorconfig", "."]
COPY ["./global.json", "."]
COPY ["./src", "/src"]

WORKDIR "/src/Platform/Router"

RUN dotnet restore --locked-mode

FROM source AS build

ARG VERSION

RUN dotnet publish /src/Platform/Router/Wadio.Platform.Router.csproj -o /wadio-router -c Release -r linux-x64 --self-contained --no-restore -p:DebugType=none -p:StripSymbols=true -p:MinVerSkip=true -p:InformationalVersion=${VERSION} && dotnet clean -c Release

FROM runtime AS wadio-router

ARG ENVIRONMENT

COPY --from=build /wadio-router "${APPDIR}"

ENV ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_ENVIRONMENT="${ENVIRONMENT}"
ENV DOTNET_gcServer=1
ENV DOTNET_GCRetainVM=0
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR ${APPDIR}
ENTRYPOINT [ "./Wadio.Platform.Router" ]