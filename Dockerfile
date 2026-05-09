FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Set NuGet package folder to avoid Windows path issues
ENV NUGET_PACKAGES=/root/.nuget/packages

# Copy project files first for better layer caching
COPY PaymentsAPI.slnx ./
COPY PaymentsApi/*.csproj ./PaymentsApi/
COPY Shared.Contracts/*.csproj ./Shared.Contracts/
COPY EventPublisher/*.csproj ./EventPublisher/

# Restore dependencies
RUN dotnet restore PaymentsApi/PaymentsApi.csproj

# Copy remaining source code
COPY . .

# Build and publish
RUN dotnet publish PaymentsApi/PaymentsApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PaymentsApi.dll"]
