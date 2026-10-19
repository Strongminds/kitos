FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
# Copy only the MCP projects and their dependencies, never local certificates or repository secrets.
COPY Directory.Build.props .editorconfig ./
COPY Core.Abstractions/ Core.Abstractions/
COPY Mcp.Core/ Mcp.Core/
COPY Mcp.ReadBoundary.Core/ Mcp.ReadBoundary.Core/
COPY Mcp.Broker/ Mcp.Broker/
COPY Mcp.Worker/ Mcp.Worker/
RUN dotnet publish Mcp.Broker/Mcp.Broker.csproj -c Release -o /out/broker \
    && dotnet publish Mcp.Worker/Mcp.Worker.csproj -c Release -o /out/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS broker
WORKDIR /app
COPY --from=build /out/broker ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Mcp.Broker.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /out/worker ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Mcp.Worker.dll"]
