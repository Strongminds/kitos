FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR app

COPY ["Tests.PubSubTester/Tests.PubSubTester.csproj", "Tests.PubSubTester/"]

RUN dotnet restore "Tests.PubSubTester/Tests.PubSubTester.csproj"

COPY . .

RUN dotnet publish "Tests.PubSubTester/Tests.PubSubTester.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Tests.PubSubTester.dll"]