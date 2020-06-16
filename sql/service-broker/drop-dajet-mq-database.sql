USE [master];
GO

IF (DB_ID('dajet-mq') IS NOT NULL)
BEGIN
	DROP DATABASE [dajet-mq];
END;
GO