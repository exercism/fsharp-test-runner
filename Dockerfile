FROM mcr.microsoft.com/dotnet/sdk:5.0.100-alpine3.12-amd64 AS build
WORKDIR /app

RUN dotnet new classlib --language='f#' --output .
RUN dotnet build

# # Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0.5-alpine3.13-amd64 AS runtime
WORKDIR /opt/test-runner

COPY --from=build /root/.nuget/packages /root/.nuget/packages
COPY --from=build /usr/share/dotnet/sdk/5.0.100/FSharp /usr/share/dotnet/sdk/5.0.100/FSharp
COPY --from=build /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0 /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0
COPY --from=build /app .

COPY run.sh /opt/test-runner/bin/

ENTRYPOINT ["sh", "/opt/test-runner/bin/run.sh"]
