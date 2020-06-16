USE [master];
GO

DECLARE @broker_endpoint_name nvarchar(128);
SELECT @broker_endpoint_name = [name] FROM sys.service_broker_endpoints WHERE [type] = 3;
IF (@broker_endpoint_name IS NOT NULL)
BEGIN
	EXEC(N'DROP ENDPOINT [' + @broker_endpoint_name + N'];');
END;
GO