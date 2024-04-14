
# Comando para criar os bancos
docker exec df77e82edfa44f78846140c82a689de83fdf9cb942e1cfb2d00d7b505592977d /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Monkify@123" -Q "
	CREATE DATABASE MONKIFY
	GO
	
	CREATE DATABASE LOGS
	GO
"

# Depois de rodar o container da API uma vez, rodar o comando abaixo
# Comando para criar o parametro de sessão
docker exec df77e82edfa44f78846140c82a689de83fdf9cb942e1cfb2d00d7b505592977d /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Monkify@123" -Q "
	USE MONKIFY
	GO

	INSERT INTO SESSIONPARAMETERS VALUES
	(NEWID(), 2, 1, 1, GETDATE(), NULL, 4, 1, 2)
"