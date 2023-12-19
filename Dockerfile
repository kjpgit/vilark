# Compiles Vilark to native code.
# Currently, only Linux is supported.

# Use an old ubuntu, because its glibc version should be compatible on most linuxes
FROM ubuntu:20.04
#FROM mcr.microsoft.com/dotnet/sdk:8.0

RUN apt-get update && apt-get install -y --no-install-recommends wget ca-certificates

RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
        && dpkg -i packages-microsoft-prod.deb

# Install SDK and NativeAOT build prerequisites
RUN apt-get update && apt-get install -y --no-install-recommends dotnet-sdk-8.0 clang zlib1g-dev

WORKDIR /build
COPY src ./
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
RUN dotnet restore
RUN dotnet publish -o out
