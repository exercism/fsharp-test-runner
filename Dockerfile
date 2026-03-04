FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.103-alpine3.23 AS build
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
RUN dotnet add package FsCheck --version 2.16.3
RUN dotnet add package FsCheck.Xunit --version 2.14.3
RUN dotnet add package FsCheck.Xunit.v3 --version 3.3.2
RUN dotnet add package FSharp.Core --version 9.0.201
RUN dotnet add package FSharp.Core --version 10.0.103
RUN dotnet add package FParsec --version 1.1.1
RUN dotnet add package FsToolkit.ErrorHandling --version 4.15.2

WORKDIR /app

# Copy fsproj and restore as distinct layers
COPY src/Exercism.TestRunner.FSharp/Exercism.TestRunner.FSharp.fsproj ./
RUN dotnet restore -a $TARGETARCH

# Copy everything else and build
COPY src/Exercism.TestRunner.FSharp/ ./
RUN dotnet publish -a $TARGETARCH -c Release -o /opt/test-runner --no-restore

# Build runtime image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.103-alpine3.23 AS runtime

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_ROLL_FORWARD=Major
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

WORKDIR /opt/test-runner

COPY --from=build /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=build /opt/test-runner/ .
COPY bin/ bin/

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
