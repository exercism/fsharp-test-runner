FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401-alpine3.23 AS build
ARG TARGETARCH

WORKDIR /tmp

# Pre-install packages for offline usage
RUN dotnet new console
RUN dotnet add package Microsoft.NET.Test.Sdk --version 16.8.3
RUN dotnet add package Microsoft.NET.Test.Sdk --version 18.3.0
RUN dotnet add package xunit --version 2.4.1
RUN dotnet add package xunit.v3 --version 3.2.2
RUN dotnet add package xunit.runner.visualstudio --version 2.4.3
RUN dotnet add package xunit.runner.visualstudio --version 3.1.5
RUN dotnet add package FsUnit --version 4.0.4
RUN dotnet add package FsUnit.xUnit --version 4.0.4
RUN dotnet add package FsUnit.xUnit --version 7.1.1
RUN dotnet add package Exercism.Tests --version 0.1.0-alpha
RUN dotnet add package Exercism.Tests --version 0.1.0-beta1
RUN dotnet add package Exercism.Tests.xunit.v3 --version 0.1.0-beta1
RUN dotnet add package Aether --version 8.3.1
RUN dotnet add package BenchmarkDotNet --version 0.12.1
RUN dotnet add package FakeItEasy --version 6.2.1
RUN dotnet add package FsCheck --version 2.14.3
RUN dotnet add package FsCheck --version 3.3.2
RUN dotnet add package FsCheck.Xunit --version 2.14.3
RUN dotnet add package FsCheck.Xunit.v3 --version 3.3.2
RUN dotnet add package FSharp.Core --version 9.0.201
RUN dotnet add package FSharp.Core --version 10.0.103
RUN dotnet add package FParsec --version 1.1.1
RUN dotnet add package FsToolkit.ErrorHandling --version 4.15.2

# Student projects reference the FSharp.Core version bundled with the SDK
# without naming it. If that version is missing from the package cache, each
# run will restore it from the SDK's library-packs folder and verify its
# signature. With no network, the revocation checks in that verification wait
# about 40 s on failed DNS lookups, so every run times out. Restoring an F#
# project here caches the bundled version, so runs find it already installed.
RUN dotnet new console -lang "F#" -o /tmp/fsharp-core && dotnet restore /tmp/fsharp-core

WORKDIR /app

# Copy fsproj and restore as distinct layers.
# The runner is published with ReadyToRun, so its code (mostly the F# parser in
# Fantomas.FCS) is precompiled and doesn't go through the JIT on every run.
COPY src/Exercism.TestRunner.FSharp/Exercism.TestRunner.FSharp.fsproj ./
RUN dotnet restore -a $TARGETARCH -p:PublishReadyToRun=true

# Copy everything else and build
COPY src/Exercism.TestRunner.FSharp/ ./
RUN dotnet publish -a $TARGETARCH -c Release -o /opt/test-runner --no-restore -p:PublishReadyToRun=true

# ReadyToRun needs the runtime packs and the ReadyToRun compiler, which only the
# runner's own build uses. Removing them keeps them out of the runtime image.
RUN rm -rf /root/.nuget/packages/microsoft.netcore.app.crossgen2.* \
    /root/.nuget/packages/microsoft.netcore.app.runtime.* \
    /root/.nuget/packages/microsoft.aspnetcore.app.runtime.*

# Build runtime image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401-alpine3.23 AS runtime

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_ROLL_FORWARD=Major
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

WORKDIR /opt/test-runner

COPY --from=build /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=build /opt/test-runner/ .
COPY bin/run.sh bin/run.sh

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
