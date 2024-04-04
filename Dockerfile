# Usa a imagem base do SDK do .NET para construir o projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# Copia o arquivo de solução e os arquivos de projeto csproj
COPY ["src/Monkify.sln", "./"]
COPY ["src/Monkify.Api/Monkify.Api.csproj", "Monkify.Api/"]
COPY ["src/Monkify.Domain/Monkify.Domain.csproj", "Monkify.Domain/"]
COPY ["src/Monkify.Common/Monkify.Common.csproj", "Monkify.Common/"]
COPY ["src/Monkify.Infrastructure/Monkify.Infrastructure.csproj", "Monkify.Infrastructure/"]

# Restaura as dependências para todos os projetos da solução
RUN dotnet restore

# Copia os arquivos restantes e constrói a aplicação
COPY . .
RUN dotnet publish "src/Monkify.Api/Monkify.Api.csproj" -c Release -o /app/publish

# Gera a imagem final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Monkify.Api.dll"]