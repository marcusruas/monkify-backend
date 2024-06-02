@echo off
setlocal

:: Set database connection parameters
set CONTAINER_ID=b5d07c8da7cb6d15988d37f8318b1817b484c52ace2ca943cdda259a1fcef7b5
set SQL_SERVER=localhost
set SQL_USER=SA
set SQL_PASSWORD=Monkify@123

echo Creating databases...
docker exec %CONTAINER_ID% /opt/mssql-tools/bin/sqlcmd -S %SQL_SERVER% -U %SQL_USER% -P "%SQL_PASSWORD%" -Q "CREATE DATABASE MONKIFY"

echo Databases created.

echo Inserting session parameter...
docker exec %CONTAINER_ID% /opt/mssql-tools/bin/sqlcmd -S %SQL_SERVER% -U %SQL_USER% -P "%SQL_PASSWORD%" -Q "INSERT INTO MONKIFY.DBO.SESSIONPARAMETERS VALUES (NEWID(), 2, 'Test parameter - 4 letter', 1, 2, 4, 1, 1, GETDATE(), NULL)"

echo Session parameter inserted.

endlocal