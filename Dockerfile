FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

COPY ["src/Monkify.sln", "./"]
COPY ["src/Monkify.Api/Monkify.Api.csproj", "Monkify.Api/"]
COPY ["src/Monkify.Domain/Monkify.Domain.csproj", "Monkify.Domain/"]
COPY ["src/Monkify.Common/Monkify.Common.csproj", "Monkify.Common/"]
COPY ["src/Monkify.Infrastructure/Monkify.Infrastructure.csproj", "Monkify.Infrastructure/"]
COPY ["src/Monkify.Tests/Monkify.Tests.csproj", "Monkify.Tests/"]

RUN dotnet restore

COPY . .
RUN dotnet publish "src/Monkify.Api/Monkify.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Monkify.Api.dll"]