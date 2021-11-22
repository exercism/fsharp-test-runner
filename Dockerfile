FROM mcr.microsoft.com/dotnet/sdk:6.0.100-alpine3.14-amd64 AS build

WORKDIR /tmp

# Pre-install packages for offline usage
RUN dotnet new console
RUN dotnet add package Microsoft.NET.Test.Sdk -v 16.8.3
RUN dotnet add package xunit -v 2.4.1
RUN dotnet add package xunit.runner.visualstudio -v 2.4.3
RUN dotnet add package FsUnit -v 4.0.4
RUN dotnet add package FsUnit.xUnit -v 4.0.4
RUN dotnet add package NUnit -v 3.13.2
RUN dotnet add package NUnit3TestAdapter -v 4.1.0
RUN dotnet add package Exercism.Tests -v 0.1.0-alpha
RUN dotnet add package Exercism.Tests -v 0.1.0-beta1
RUN dotnet add package Aether -v 8.3.1
RUN dotnet add package BenchmarkDotNet -v 0.12.1
RUN dotnet add package FakeItEasy -v 6.2.1
RUN dotnet add package FsCheck.Xunit -v 2.14.3
RUN dotnet add package FsCheck.Nunit -v 2.16.3
RUN dotnet add package FSharp.Core -v 6.0.1

WORKDIR /app

# Copy fsproj and restore as distinct layers
COPY src/Exercism.TestRunner.FSharp/Exercism.TestRunner.FSharp.fsproj ./
RUN dotnet restore -r linux-musl-x64

# Copy everything else and build
COPY src/Exercism.TestRunner.FSharp/ ./
RUN dotnet publish -r linux-musl-x64 -c Release -o /opt/test-runner --no-restore --self-contained true

# # Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0.100-alpine3.14-amd64 AS runtime
WORKDIR /opt/test-runner

COPY --from=build /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=build /opt/test-runner/ .
COPY bin/ /bin/

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
