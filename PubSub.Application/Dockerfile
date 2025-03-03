# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR app

# Copy the solution file and the project files
COPY ["PubSub.Application/PubSub.Application.csproj", "PubSub.Application/"]
COPY ["PubSub.Core/PubSub.Core.csproj", "PubSub.Core/"]

# Restore the dependencies
RUN dotnet restore "PubSub.Application/PubSub.Application.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
RUN dotnet publish "PubSub.Application/PubSub.Application.csproj" -c Release -o out

# Use the official ASP.NET Core runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expose the port the app runs on
EXPOSE 8080
EXPOSE 8081

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "PubSub.Application.dll"]
