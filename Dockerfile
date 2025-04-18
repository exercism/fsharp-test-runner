FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH

WORKDIR /tmp

# Pre-install packages for offline usage
RUN dotnet new console
RUN dotnet add package Microsoft.NET.Test.Sdk -v 16.8.3
RUN dotnet add package xunit -v 2.4.1
RUN dotnet add package xunit.runner.visualstudio -v 2.4.3
RUN dotnet add package FsUnit -v 4.0.4
RUN dotnet add package FsUnit.xUnit -v 4.0.4
RUN dotnet add package Exercism.Tests -v 0.1.0-alpha
RUN dotnet add package Exercism.Tests -v 0.1.0-beta1
RUN dotnet add package Aether -v 8.3.1
RUN dotnet add package BenchmarkDotNet -v 0.12.1
RUN dotnet add package FakeItEasy -v 6.2.1
RUN dotnet add package FsCheck -v 2.14.3
RUN dotnet add package FsCheck -v 2.16.3
RUN dotnet add package FsCheck.Xunit -v 2.14.3
RUN dotnet add package FSharp.Core -v 6.0.1
RUN dotnet add package FSharp.Core -v 7.0.400
RUN dotnet add package FSharp.Core -v 8.0.101
RUN dotnet add package FSharp.Core -v 8.0.403
RUN dotnet add package FSharp.Core -v 9.0.201
RUN dotnet add package FParsec -v 1.1.1
RUN dotnet add package FsToolkit.ErrorHandling -v 4.15.2

WORKDIR /app

# Copy fsproj and restore as distinct layers
COPY src/Exercism.TestRunner.FSharp/Exercism.TestRunner.FSharp.fsproj ./
RUN dotnet restore -a $TARGETARCH

# Copy everything else and build
COPY src/Exercism.TestRunner.FSharp/ ./
RUN dotnet publish -a $TARGETARCH -c Release -o /opt/test-runner --no-restore

# Build runtime image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS runtime

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_ROLL_FORWARD=Major
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

WORKDIR /opt/test-runner

COPY --from=build /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=build /opt/test-runner/ .
COPY bin/ bin/

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
