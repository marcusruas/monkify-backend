FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

COPY ["Monkify.sln", "./"]
COPY ["Monkify.Api/Monkify.Api.csproj", "Monkify.Api/"]
COPY ["Monkify.Consumers/Monkify.Consumers.csproj", "Monkify.Consumers/"]
COPY ["Monkify.Domain/Monkify.Domain.csproj", "Monkify.Domain/"]
COPY ["Monkify.Common/Monkify.Common.csproj", "Monkify.Common/"]
COPY ["Monkify.Infrastructure/Monkify.Infrastructure.csproj", "Monkify.Infrastructure/"]
COPY ["Monkify.Tests/Monkify.Tests.csproj", "Monkify.Tests/"]

RUN dotnet restore

COPY . .
RUN dotnet publish "Monkify.Api/Monkify.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Monkify.Api.dll"]