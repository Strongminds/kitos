FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR app

COPY ["PubSub.Application.csproj", "./"]

RUN dotnet restore "PubSub.Application.csproj"

COPY . .

RUN dotnet publish "PubSub.Application.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "PubSub.Application.dll"]
