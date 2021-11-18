FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.13-amd64 AS build
WORKDIR /app

# Copy fsproj and restore as distinct layers
COPY src/Exercism.TestRunner.FSharp/Exercism.TestRunner.FSharp.fsproj ./
RUN dotnet restore -r linux-musl-x64

# Copy everything else and build
COPY src/Exercism.TestRunner.FSharp/ ./
RUN dotnet publish -r linux-musl-x64 -c Release -o /opt/test-runner --no-restore --self-contained true

# Pre-install packages for offline usage
RUN dotnet add package Microsoft.NET.Test.Sdk -v 16.8.3
RUN dotnet add package xunit -v 2.4.1
RUN dotnet add package xunit.runner.visualstudio -v 2.4.3
RUN dotnet add package FsUnit.xUnit -v 4.0.4
RUN dotnet add package Exercism.Tests -v 0.1.0-alpha
RUN dotnet add package Exercism.Tests -v 0.1.0-beta1
RUN dotnet add package Aether -v 8.3.1
RUN dotnet add package BenchmarkDotNet -v 0.12.1
RUN dotnet add package FakeItEasy -v 6.2.1
RUN dotnet add package FsCheck.Xunit -v 2.14.3

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.13-amd64 AS runtime
WORKDIR /opt/test-runner

COPY --from=build /opt/test-runner/ .
COPY --from=build /usr/local/bin/ /usr/local/bin/
COPY --from=build /root/.nuget/packages/ /root/.nuget/packages/

COPY run.sh /opt/test-runner/bin/
COPY Directory.Build.props /

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
