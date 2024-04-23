@echo off
setlocal

:: Set database connection parameters
set CONTAINER_ID=df77e82edfa44f78846140c82a689de83fdf9cb942e1cfb2d00d7b505592977d
set SQL_SERVER=localhost
set SQL_USER=SA
set SQL_PASSWORD=Monkify@123

echo Creating databases...
docker exec %CONTAINER_ID% /opt/mssql-tools/bin/sqlcmd -S %SQL_SERVER% -U %SQL_USER% -P "%SQL_PASSWORD%" -Q "
    CREATE DATABASE MONKIFY
    GO
"

echo Databases created.

echo Inserting session parameter...
docker exec %CONTAINER_ID% /opt/mssql-tools/bin/sqlcmd -S %SQL_SERVER% -U %SQL_USER% -P "%SQL_PASSWORD%" -Q "
    USE MONKIFY
    GO

    INSERT INTO SESSIONPARAMETERS VALUES
    (NEWID(), 2, 2, 1, 4, 1, 1, GETDATE(), NULL)
"

echo Session parameter inserted.

endlocal