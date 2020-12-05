USE [master];
GO

-- =========================
-- Create messaging database
-- =========================
IF (DB_ID('dajet-mq') IS NULL)
BEGIN
	CREATE DATABASE [dajet-mq];
END;
GO

USE [dajet-mq];
GO

-- ================================================
-- Enable service broker for the messaging database
-- ================================================
IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id = DB_ID('dajet-mq') AND is_broker_enabled = 0x01)
BEGIN
	ALTER DATABASE [dajet-mq] SET ENABLE_BROKER;
END;
GO

-- ==========================================
-- Create function to get service broker guid
-- ==========================================
IF OBJECT_ID('dbo.fn_service_broker_guid', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_service_broker_guid];
END;
GO

CREATE FUNCTION [dbo].[fn_service_broker_guid]()
RETURNS uniqueidentifier
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier);
	SELECT @broker_guid = service_broker_guid FROM sys.databases WHERE database_id = DB_ID('dajet-mq');
	RETURN @broker_guid;
END;
GO

-- ===================================================
-- Create function to get service broker endpoint port
-- ===================================================
IF OBJECT_ID('dbo.fn_service_broker_port', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_service_broker_port];
END;
GO

CREATE FUNCTION [dbo].[fn_service_broker_port]()
RETURNS int
AS
BEGIN
	DECLARE @broker_port int;
	
	SELECT @broker_port = [tcp].[port]
	FROM sys.tcp_endpoints AS [tcp]
	INNER JOIN sys.service_broker_endpoints AS [sbe]
	ON [sbe].[endpoint_id] = [tcp].[endpoint_id]
	WHERE [sbe].[type] = 3; -- Service Broker endpoint type

	IF (@broker_port IS NULL) SET @broker_port = 0;

	RETURN @broker_port;
END;
GO

-- ====================================
-- Create messaging database master key
-- ====================================
IF NOT EXISTS(SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
BEGIN
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = '{0D71CAEC-7BA4-44CE-8531-66367F31D8F4}';
END;
GO

-- ==========================================
-- Create function to build default user name
-- ==========================================
IF OBJECT_ID('dbo.fn_default_user_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_user_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_user_name]()
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/user';
END;
GO

-- =======================================
-- Create procedure to create default user
-- =======================================
IF OBJECT_ID('dbo.sp_create_default_user', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_default_user];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_default_user]
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @user_name nvarchar(128) = (SELECT [dbo].[fn_default_user_name]());

	IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = @user_name)
	BEGIN
		EXEC('CREATE USER [' + @user_name + '] WITHOUT LOGIN;');
	END;
END;
GO

-- ============================================================
-- Create service broker user for the messaging database
-- This user gets control over service broker services
-- Export this user to the remote database for message exchange
-- ============================================================
EXEC [dbo].[sp_create_default_user];
GO

-- ========================================================
-- Create function to build default user's certificate name
-- ========================================================
IF OBJECT_ID('dbo.fn_default_certificate_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_certificate_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_certificate_name]()
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/certificate';
END;
GO

-- ==============================================
-- Create procedure to create default certificate
-- ==============================================
IF OBJECT_ID('dbo.sp_create_default_certificate', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_default_certificate];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_default_certificate]
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @certificate_name nvarchar(128) = (SELECT [dbo].[fn_default_certificate_name]());

	IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE name = @certificate_name)
	BEGIN
		DECLARE @user_name nvarchar(128) = (SELECT [dbo].[fn_default_user_name]());

		EXEC('CREATE CERTIFICATE [' + @certificate_name + '] AUTHORIZATION [' + @user_name + ']
		WITH SUBJECT = ''Default service broker user certificate'',
			START_DATE = ''20200101'',
			EXPIRY_DATE = ''20300101'';');
	END;
END;
GO

-- ===========================================================================
-- Create service broker authentication certificate for the messaging database
-- This certificate is used to authenticate as default user
-- Export certificate's public key to the remote database for message exchange
-- ===========================================================================
EXEC [dbo].[sp_create_default_certificate];
GO

-- ===================================
-- Create function to build queue name
-- ===================================
IF OBJECT_ID('dbo.fn_create_queue_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_create_queue_name];
END;
GO

CREATE FUNCTION [dbo].[fn_create_queue_name](@name nvarchar(80))
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/queue/' + @name;
END;
GO

-- ===========================================
-- Create function to build default queue name
-- ===========================================
IF OBJECT_ID('dbo.fn_default_queue_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_queue_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_queue_name]()
RETURNS nvarchar(128)
AS
BEGIN
	RETURN [dbo].[fn_create_queue_name](N'default');
END;
GO

-- =====================================
-- Create function to build service name
-- =====================================
IF OBJECT_ID('dbo.fn_create_service_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_create_service_name];
END;
GO

CREATE FUNCTION [dbo].[fn_create_service_name](@name nvarchar(80))
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/service/' + @name;
END;
GO

-- =============================================
-- Create function to build default service name
-- =============================================
IF OBJECT_ID('dbo.fn_default_service_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_service_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_service_name]()
RETURNS nvarchar(128)
AS
BEGIN
	RETURN [dbo].[fn_create_service_name](N'default');
END;
GO

-- ====================================================
-- Create function to check if queue exists and enabled
-- ====================================================
IF OBJECT_ID('dbo.fn_queue_exists', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_queue_exists];
END;
GO

CREATE FUNCTION [dbo].[fn_queue_exists](@name nvarchar(128))
RETURNS int
AS
BEGIN
	DECLARE @enabled bit;

	SELECT @enabled = is_enqueue_enabled FROM sys.service_queues WHERE name = @name;

	IF (@enabled IS NULL) RETURN 1;
	ELSE IF (@enabled = 0x01) RETURN 0;
	
	RETURN 2; -- exists disabled
END;
GO

-- =========================================
-- Create procedure to get local queues list
-- =========================================
IF OBJECT_ID('dbo.sp_select_local_queue_list', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_select_local_queue_list];
END;
GO

CREATE PROCEDURE [dbo].[sp_select_local_queue_list]
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @default_queue_name nvarchar(128) = [dbo].[fn_default_queue_name]();

	SELECT
		[name],
		[object_id],
		CAST([is_enqueue_enabled] AS int) AS [enabled]
	FROM
		sys.service_queues
	WHERE
		[name] != @default_queue_name AND [is_ms_shipped] = 0;
END;
GO

-- =====================================================
-- Create procedure to get outbound (remote) queues list
-- =====================================================
IF OBJECT_ID('dbo.sp_select_remote_queue_list', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_select_remote_queue_list];
END;
GO

CREATE PROCEDURE [dbo].[sp_select_remote_queue_list]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		REPLACE([remote_service_name], N'/service/', N'/queue/') AS [name],
		[broker_instance] AS [broker_guid],
		[address]
	FROM
		sys.routes
	WHERE
		[remote_service_name] IS NOT NULL AND NOT [remote_service_name] LIKE N'%/default';
END;
GO

-- =================================================
-- Create procedure to get local queue current state
-- =================================================
IF OBJECT_ID('dbo.sp_select_queue_state', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_select_queue_state];
END;
GO

CREATE PROCEDURE [dbo].[sp_select_queue_state]
	@queue nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @local_broker_guid nvarchar(36) = CAST([dbo].[fn_service_broker_guid]() AS nvarchar(36));
	DECLARE @queue_broker_guid nvarchar(36) = SUBSTRING(@queue, 1, 36);

	IF (@local_broker_guid = @queue_broker_guid)
	BEGIN -- local queue
		
		DECLARE @queue_table nvarchar(136) = N'[dbo].[' + @queue + N']';

		EXEC(N'SELECT COUNT(*) AS [Count], ISNULL(SUM(DATALENGTH(message_body)), 0) AS [Size]
		FROM' + @queue_table + N' WITH(NOLOCK);');

	END;
	ELSE
	BEGIN
		
		DECLARE @service_name nvarchar(128) = REPLACE(@queue, N'/queue/', N'/service/');

		SELECT
			COUNT(*) AS [Count],
			ISNULL(SUM(DATALENGTH(message_body)), 0) AS [Size]
		FROM sys.transmission_queue
		WHERE to_service_name = @service_name;

	END;
END;
GO

-- ================================
-- Create procedure to delete queue
-- ================================
IF OBJECT_ID('dbo.sp_delete_queue', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_delete_queue];
END;
GO

CREATE PROCEDURE [dbo].[sp_delete_queue](@name nvarchar(80))
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @queue_name nvarchar(128) = [dbo].[fn_create_queue_name](@name);
	DECLARE @service_name nvarchar(128) = [dbo].[fn_create_service_name](@name);
	DECLARE @default_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	IF (@default_service_name = @queue_name) THROW 50001, N'The default queue can not be droped!', 1;
	
	IF EXISTS(SELECT 1 FROM sys.services WHERE name = @service_name)
	BEGIN
		EXEC(N'DROP SERVICE [' + @service_name + N'];');
	END;

	IF EXISTS(SELECT 1 FROM sys.service_queues WHERE name = @queue_name)
	BEGIN
		EXEC(N'DROP QUEUE [dbo].[' + @queue_name + N'];');
	END;

	DECLARE @handle uniqueidentifier = [dbo].[fn_get_dialog_handle](@queue_name);
	IF (@handle IS NOT NULL AND @handle != CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier))
	BEGIN
		END CONVERSATION @handle;
	END;

	-- ==============================================
	-- Receive error message for default local dialog
	-- ==============================================
	-- http://schemas.microsoft.com/SQL/ServiceBroker/Error
	-- 'Remote service has been dropped.' message !
END;
GO

-- =====================================================
-- Create function to get dialog handle for target queue
-- =====================================================
IF OBJECT_ID('dbo.fn_get_dialog_handle', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_get_dialog_handle];
END;
GO

CREATE FUNCTION [dbo].[fn_get_dialog_handle](@target_queue_name nvarchar(128))
RETURNS uniqueidentifier
AS
BEGIN
	DECLARE @source_broker_guid nvarchar(36) = [dbo].[fn_service_broker_guid]();
	DECLARE @source_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	IF (@target_queue_name IS NULL) RETURN CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier);
	
	DECLARE @target_broker_guid nvarchar(36) = SUBSTRING(@target_queue_name, 1, 36);
	DECLARE @target_service_name nvarchar(128) = REPLACE(@target_queue_name, N'/queue/', N'/service/');

	DECLARE @handle uniqueidentifier;

	SELECT @handle = conversation_handle
	FROM sys.conversation_endpoints AS e
	INNER JOIN sys.services AS s ON e.service_id = s.service_id
	AND e.is_initiator = 1
	AND e.state IN ('SO', 'CO')
	AND s.name = @source_service_name
	AND e.far_service = @target_service_name
	AND e.far_broker_instance = @target_broker_guid;

	IF (@handle IS NULL) SET @handle = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier);

	RETURN @handle;
END;
GO

-- =================================
-- Create procedure to create queues
-- =================================
IF OBJECT_ID('dbo.sp_create_queue', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_queue];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_queue](@name nvarchar(80))
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @user_name nvarchar(128) = [dbo].[fn_default_user_name]();
	DECLARE @queue_name nvarchar(128) = [dbo].[fn_create_queue_name](@name);
	DECLARE @service_name nvarchar(128) = [dbo].[fn_create_service_name](@name);
	DECLARE @default_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	IF NOT EXISTS(SELECT 1 FROM sys.service_queues WHERE name = @queue_name)
	BEGIN
		EXEC('CREATE QUEUE [' + @queue_name + '] WITH POISON_MESSAGE_HANDLING (STATUS = OFF);');
	END;

	IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = @service_name)
	BEGIN
		EXEC('CREATE SERVICE [' + @service_name + '] ON QUEUE [' + @queue_name + '] ([DEFAULT]);
		GRANT CONTROL ON SERVICE::[' + @service_name + '] TO [' + @user_name + '];
		GRANT SEND ON SERVICE::[' + @service_name + '] TO [PUBLIC];');
	END;

	-- ===========================
	-- Create default local dialog
	-- ===========================

	DECLARE @handle uniqueidentifier = [dbo].[fn_get_dialog_handle](@queue_name);
	DECLARE @broker_guid nvarchar(36) = CAST([dbo].[fn_service_broker_guid]() AS nvarchar(36));

	IF (@handle IS NULL OR @handle = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier))
	BEGIN  -- dialog handle is not found
		DECLARE @sql nvarchar(1024) = 'BEGIN DIALOG @handle_out
		FROM SERVICE [' + @default_service_name + ']
		TO SERVICE ''' + @service_name + ''', ''' + @broker_guid + '''
		ON CONTRACT [DEFAULT]
		WITH ENCRYPTION = OFF;';
		EXECUTE sp_executesql @sql, N'@handle_out uniqueidentifier OUTPUT', @handle_out = @handle OUTPUT;
	END;
END;
GO

-- ====================
-- Create default queue
-- ====================
EXEC [dbo].[sp_create_queue] N'default';
GO

-- ========================================
-- Create procedure to send binary messages
-- ========================================
IF OBJECT_ID('dbo.sp_send_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_send_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_send_message]
	@dialog_handle uniqueidentifier,
	@message_body varbinary(max),
	@message_type nvarchar(128) = NULL
AS
BEGIN
	SET NOCOUNT ON;
	
	IF (@message_type IS NULL) SET @message_type = N'DEFAULT';

	SEND ON CONVERSATION @dialog_handle MESSAGE TYPE @message_type (@message_body);

    RETURN 0;
END
GO

-- ==========================================================================
-- Create procedure to send UTF-8 messages - single-byte-character set (SBCS)
-- ==========================================================================
IF OBJECT_ID('dbo.sp_send_message_sbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_send_message_sbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_send_message_sbcs]
	@dialog_handle uniqueidentifier,
	@message_body varchar(max),
	@message_type nvarchar(128) = NULL
AS
BEGIN
	SET NOCOUNT ON;
	
	IF (@message_type IS NULL) SET @message_type = N'DEFAULT';

	SEND ON CONVERSATION @dialog_handle MESSAGE TYPE @message_type (CAST(@message_body AS varbinary(max)));

    RETURN 0;
END
GO

-- =========================================================================
-- Create procedure to send UTF-16 messages - multibyte-character set (MBCS)
-- =========================================================================
IF OBJECT_ID('dbo.sp_send_message_mbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_send_message_mbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_send_message_mbcs]
	@dialog_handle uniqueidentifier,
	@message_body nvarchar(max),
	@message_type nvarchar(128) = NULL
AS
BEGIN
	SET NOCOUNT ON;
	
	IF (@message_type IS NULL) SET @message_type = N'DEFAULT';

	SEND ON CONVERSATION @dialog_handle MESSAGE TYPE @message_type (CAST(@message_body AS varbinary(max)));

    RETURN 0;
END
GO

-- ==============================================
-- Create procedure to receive one binary message
-- ==============================================
IF OBJECT_ID('dbo.sp_receive_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_message]
	@queue nvarchar(128),
	@timeout int = 1000
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @handle uniqueidentifier;
	DECLARE @message_type nvarchar(128);
	DECLARE @message_body varbinary(max);

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (1)
		@handle_out = conversation_handle,
		@message_type_out = message_type_name,
		@message_body_out = message_body
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;';

	EXECUTE sp_executesql @sql,
		N'@handle_out uniqueidentifier OUTPUT, @message_type_out nvarchar(128) OUTPUT,
		  @message_body_out varbinary(max) OUTPUT, @timeout int',
		  @handle_out       = @handle       OUTPUT,
		  @message_type_out = @message_type OUTPUT,
		  @message_body_out = @message_body OUTPUT,
		  @timeout          = @timeout;

	IF (@@ROWCOUNT = 0)
	BEGIN
		SELECT CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier) AS [dialog_handle],
			N''  AS [message_type],
			0x00 AS [message_body];
		RETURN 0;
	END

	IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR
        @message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
	BEGIN
		END CONVERSATION @handle;
	END

	SELECT @handle AS [dialog_handle], @message_type AS [message_type], @message_body AS [message_body];

    RETURN 0;
END
GO

-- ================================================================================
-- Create procedure to receive one UTF-8 message - single-byte-character set (SBCS)
-- ================================================================================
IF OBJECT_ID('dbo.sp_receive_message_sbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_message_sbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_message_sbcs]
	@queue nvarchar(128),
	@timeout int = 1000
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @handle uniqueidentifier;
	DECLARE @message_type nvarchar(128);
	DECLARE @message_body varchar(max);

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (1)
		@handle_out = conversation_handle,
		@message_type_out = message_type_name,
		@message_body_out = CAST(message_body AS varchar(max))
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;';

	EXECUTE sp_executesql @sql,
		N'@handle_out uniqueidentifier OUTPUT, @message_type_out nvarchar(128) OUTPUT,
		  @message_body_out varchar(max) OUTPUT, @timeout int',
		  @handle_out       = @handle       OUTPUT,
		  @message_type_out = @message_type OUTPUT,
		  @message_body_out = @message_body OUTPUT,
		  @timeout          = @timeout;

	IF (@@ROWCOUNT = 0)
	BEGIN
		SELECT CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier) AS [dialog_handle],
				N'' AS [message_type],
				 '' AS [message_body];
		RETURN 0;
	END

	IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR
        @message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
	BEGIN
		END CONVERSATION @handle;
	END

	SELECT @handle AS [dialog_handle], @message_type AS [message_type], @message_body AS [message_body];

    RETURN 0;
END
GO

-- ===============================================================================
-- Create procedure to receive one UTF-16 message - multibyte-character set (MBCS)
-- ===============================================================================
IF OBJECT_ID('dbo.sp_receive_message_mbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_message_mbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_message_mbcs]
	@queue nvarchar(128),
	@timeout int = 1000
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @handle uniqueidentifier;
	DECLARE @message_type nvarchar(128);
	DECLARE @message_body nvarchar(max);

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (1)
		@handle_out = conversation_handle,
		@message_type_out = message_type_name,
		@message_body_out = CAST(message_body AS nvarchar(max))
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;';

	EXECUTE sp_executesql @sql,
		N'@handle_out uniqueidentifier OUTPUT, @message_type_out nvarchar(128) OUTPUT,
		  @message_body_out nvarchar(max) OUTPUT, @timeout int',
		  @handle_out       = @handle       OUTPUT,
		  @message_type_out = @message_type OUTPUT,
		  @message_body_out = @message_body OUTPUT,
		  @timeout          = @timeout;

	IF (@@ROWCOUNT = 0)
	BEGIN
		SELECT CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier) AS [dialog_handle],
				N'' AS [message_type],
				N'' AS [message_body];
		RETURN 0;
	END

	IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR
        @message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
	BEGIN
		END CONVERSATION @handle;
	END

	SELECT @handle AS [dialog_handle], @message_type AS [message_type], @message_body AS [message_body];

    RETURN 0;
END
GO

-- ====================================================
-- Create procedure to receive multiple binary messages
-- ====================================================
IF OBJECT_ID('dbo.sp_receive_messages', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_messages];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_messages]
	@queue nvarchar(128),
	@timeout int = 1000,
	@number_of_messages int = 10
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @row_count int = 0;

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (@number_of_messages)
		conversation_handle AS [dialog_handle],
		message_type_name   AS [message_type],
		message_body        AS [message_body]
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;
	SET @row_count_out = @@ROWCOUNT;';

	EXECUTE sp_executesql @sql, N'@number_of_messages int, @timeout int, @row_count_out int OUTPUT',
		  @number_of_messages = @number_of_messages, @timeout = @timeout, @row_count_out = @row_count OUTPUT;

	RETURN @row_count;
END
GO

-- =======================================================================================
-- Create procedure to receive multiple UTF-16 messages - single-byte-character set (SBCS)
-- =======================================================================================
IF OBJECT_ID('dbo.sp_receive_messages_sbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_messages_sbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_messages_sbcs]
	@queue nvarchar(128),
	@timeout int = 1000,
	@number_of_messages int = 10
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @row_count int = 0;

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (@number_of_messages)
		conversation_handle                AS [dialog_handle],
		message_type_name                  AS [message_type],
		CAST(message_body AS varchar(max)) AS [message_body]
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;
	SET @row_count_out = @@ROWCOUNT;';

	EXECUTE sp_executesql @sql, N'@number_of_messages int, @timeout int, @row_count_out int OUTPUT',
		  @number_of_messages = @number_of_messages, @timeout = @timeout, @row_count_out = @row_count OUTPUT;

	RETURN @row_count;
END
GO

-- =====================================================================================
-- Create procedure to receive multiple UTF-16 messages - multibyte-character set (MBCS)
-- =====================================================================================
IF OBJECT_ID('dbo.sp_receive_messages_mbcs', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_messages_mbcs];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_messages_mbcs]
	@queue nvarchar(128),
	@timeout int = 1000,
	@number_of_messages int = 10
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @row_count int = 0;

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (@number_of_messages)
		conversation_handle                 AS [dialog_handle],
		message_type_name                   AS [message_type],
		CAST(message_body AS nvarchar(max)) AS [message_body]
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;
	SET @row_count_out = @@ROWCOUNT;';

	EXECUTE sp_executesql @sql, N'@number_of_messages int, @timeout int, @row_count_out int OUTPUT',
		  @number_of_messages = @number_of_messages, @timeout = @timeout, @row_count_out = @row_count OUTPUT;

	RETURN @row_count;
END
GO

-- ================================================
-- Create procedure to create route to remote queue
-- ================================================
IF OBJECT_ID('dbo.sp_create_route', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_route];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_route]
	@remote_queue_name nvarchar(128),
	@remote_server_address nvarchar(128),
	@remote_broker_port_number int
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sql nvarchar(1024);

	DECLARE @local_broker_guid nvarchar(36) = CAST([dbo].[fn_service_broker_guid]() AS nvarchar(36));
	DECLARE @default_service_name nvarchar(128) = [dbo].[fn_default_service_name]();
	
	DECLARE @remote_broker_guid nvarchar(128) = SUBSTRING(@remote_queue_name, 1, 36);
	DECLARE @route_name nvarchar(128) = REPLACE(@remote_queue_name, N'/queue/', N'/route/');
	DECLARE @remote_user nvarchar(128) = @remote_broker_guid + N'/user';
	DECLARE @binding_name nvarchar(128) = REPLACE(@remote_queue_name, N'/queue/', N'/binding/');
	DECLARE @remote_service_name nvarchar(128) = REPLACE(@remote_queue_name, N'/queue/', N'/service/');
	
	IF (@local_broker_guid = @remote_broker_guid) THROW 50001, N'Invalid operation: there is no meaning to create route to the local queue.', 1;

	IF NOT EXISTS(SELECT 1 FROM sys.routes WHERE [name] = @route_name)
	BEGIN
		SET @sql = N'CREATE ROUTE [' + @route_name + N'] WITH
			SERVICE_NAME = ''' + @remote_service_name + N''',
			BROKER_INSTANCE = ''' + @remote_broker_guid + ''',
			ADDRESS = ''TCP://' + @remote_server_address + N':' + CAST(@remote_broker_port_number AS nvarchar(5)) + N'''' + N';';
		EXEC(@sql);
	END;

	IF NOT EXISTS(SELECT 1 FROM sys.remote_service_bindings WHERE [name] = @binding_name)
	BEGIN
		SET @sql = N'CREATE REMOTE SERVICE BINDING [' + @binding_name + N']
			TO SERVICE ''' + @remote_service_name + N'''
			WITH
			USER = [' + @remote_user + N'],
			ANONYMOUS = ON;';
		EXEC(@sql);
	END;

	DECLARE @handle uniqueidentifier = [dbo].[fn_get_dialog_handle](@remote_queue_name);
	
	IF (@handle IS NULL OR @handle = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier))
	BEGIN
		SET @sql = N'BEGIN DIALOG @handle_out
		FROM SERVICE [' + @default_service_name + N']
		TO SERVICE ''' + @remote_service_name + N''', ''' + @remote_broker_guid + N'''
		ON CONTRACT [DEFAULT]
		WITH ENCRYPTION = OFF;';
		EXECUTE sp_executesql @sql, N'@handle_out uniqueidentifier OUTPUT', @handle_out = @handle OUTPUT;
	END;

END;
GO

-- =====================================================================
-- Create procedure to create remote user and its certificate public key
-- =====================================================================
IF OBJECT_ID('dbo.sp_create_remote_user', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_remote_user];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_remote_user]
	@remote_user_name nvarchar(128),
	@remote_certificate_name nvarchar(128),
	@remote_certificate_data nvarchar(max)
AS
BEGIN
	SET NOCOUNT ON;

	IF (@remote_user_name        IS NULL) THROW 50001, N'Invalid remote user name.', 1;
	IF (@remote_certificate_name IS NULL) THROW 50002, N'Invalid remote certificate name.', 1;
	IF (@remote_certificate_data IS NULL) THROW 50003, N'Invalid remote certificate data.', 1;

	IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE [name] = @remote_user_name)
	BEGIN
		EXEC(N'CREATE USER [' + @remote_user_name + N'] WITHOUT LOGIN;');
	END;

	IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE [name] = @remote_certificate_name)
	BEGIN
		EXEC(N'CREATE CERTIFICATE [' + @remote_certificate_name + N']
			AUTHORIZATION [' + @remote_user_name + N']
			FROM BINARY = ' + @remote_certificate_data + N';');
	END;
END;
GO

-- =====================================================
-- Create function to get default certificate public key
-- =====================================================
IF OBJECT_ID('dbo.fn_default_certificate_data', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_certificate_data];
END;
GO

CREATE FUNCTION [dbo].[fn_default_certificate_data]()
RETURNS nvarchar(4000)
AS
BEGIN
	DECLARE @certificate_name nvarchar(128) = [dbo].[fn_default_certificate_name]();
	DECLARE @certificate_data nvarchar(4000);
	
	SELECT @certificate_data = CONVERT(nvarchar(4000), CERTENCODED(certificate_id), 1)
	  FROM sys.certificates
	  WHERE [name] = @certificate_name;

	RETURN @certificate_data;
END;
GO

-- ====================================================
-- Create procedure to delete route to the remote queue
-- ====================================================
IF OBJECT_ID('dbo.sp_delete_route', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_delete_route];
END;
GO

CREATE PROCEDURE [dbo].[sp_delete_route]
	@remote_queue_name nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @handle uniqueidentifier = [dbo].[fn_get_dialog_handle](@remote_queue_name);

	IF (@handle IS NULL OR @handle = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier))
	BEGIN
		END CONVERSATION @handle; -- WITH CLEANUP;
	END;

	WAITFOR DELAY '00:00:10'; -- wait for 10 seconds - let service broker to send end dialog message

	DECLARE @sql nvarchar(1024);

	DECLARE @route_name nvarchar(128) = REPLACE(@remote_queue_name, N'/queue/', N'/route/');
	DECLARE @binding_name nvarchar(128) = REPLACE(@remote_queue_name, N'/queue/', N'/binding/');

	IF EXISTS(SELECT 1 FROM sys.remote_service_bindings WHERE [name] = @binding_name)
	BEGIN
		SET @sql = N'DROP REMOTE SERVICE BINDING [' + @binding_name + N'];';
		EXECUTE(@sql);
	END;

	IF EXISTS(SELECT 1 FROM sys.routes WHERE [name] = @route_name)
	BEGIN
		SET @sql = N'DROP ROUTE [' + @route_name + N'];';
		EXECUTE(@sql);
	END;
END;
GO