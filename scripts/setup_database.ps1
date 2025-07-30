# Name of your SQL Server container (must match container_name in docker-compose)
$containerName = "sqlserver"

# SQL Server credentials
$saUser = "sa"
$saPassword = "Monkify@123"

# Database name
$databaseName = "MONKIFY"

# Run sqlcmd inside the container
docker exec -it $containerName /opt/mssql-tools18/bin/sqlcmd `
  -S localhost `
  -U $saUser `
  -P $saPassword `
  -C `
  -Q "CREATE DATABASE [$databaseName];"