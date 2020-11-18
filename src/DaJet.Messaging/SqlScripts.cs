using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DaJet.Messaging
{
    internal static class SqlScripts
    {
        internal const string SERVICE_BROKER_DATABASE = "one-c-sharp-messaging";
        private const string CHANNELS_TABLE = "channels";
        private const string ServiceBrokerTransportAuthenticationCertificate = "ServiceBrokerTransportAuthenticationCertificate";
        private const string ServiceBrokerDatabaseAuthenticationCertificate = "ServiceBrokerDatabaseAuthenticationCertificate";

        internal static void ExecuteScript(string connectionString, string sqlScript)
        {
            { /* start of the limited scope */

                SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = sqlScript;
                try
                {
                    connection.Open();
                    int result = command.ExecuteNonQuery();
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                    throw;
                }
                finally
                {
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } /* end of the limited scope */
        }
        internal static T ExecuteScalar<T>(string connectionString, string sqlScript)
        {
            T result = default;

            { // start of the limited scope
                SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = sqlScript;
                try
                {
                    connection.Open();
                    result = (T)command.ExecuteScalar();
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                }
                finally
                {
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of the limited scope

            return result;
        }
        internal static void ExecuteScript(string connectionString, string sqlScript, Dictionary<string, object> parameters)
        {
            { /* start of the limited scope */

                SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = sqlScript;
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
                try
                {
                    connection.Open();
                    int result = command.ExecuteNonQuery();
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                    throw;
                }
                finally
                {
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } /* end of the limited scope */
        }

        internal static string CreateQueueName(Guid brokerId, string name)
        {
            return $"{brokerId}/Queue/{name}";
        }
        private static string CreateServiceName(Guid brokerId, string name)
        {
            return $"{brokerId}/Service/{name}";
        }
        private static string CreateBrokerUserName(Guid brokerId)
        {
            return $"{brokerId}/User";
        }
        private static string CreateRemoteUserCertificateName(Guid brokerId)
        {
            return $"{brokerId}/RemoteUser/Certificate";
        }

        internal static Guid GetBrokerId(string queueFullName)
        {
            if (string.IsNullOrWhiteSpace(queueFullName)) throw new ArgumentNullException(nameof(queueFullName));

            string[] names = queueFullName.Split('/');
            return new Guid(names[0]);
        }
        internal static string GetQueueName(string queueFullName)
        {
            if (string.IsNullOrWhiteSpace(queueFullName)) throw new ArgumentNullException(nameof(queueFullName));

            string[] names = queueFullName.Split('/');
            return names[2];
        }

        internal static string CreatePublicEndpointScript(string name, int port)
		{
			StringBuilder script = new StringBuilder();
            script.AppendLine("USE [master];");
            script.AppendLine("IF NOT EXISTS(SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')");
            script.AppendLine("BEGIN");
            script.AppendLine("\tCREATE MASTER KEY ENCRYPTION BY PASSWORD = 'ServiceBrokerMasterKey';");
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE name='{ServiceBrokerTransportAuthenticationCertificate}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE CERTIFICATE {ServiceBrokerTransportAuthenticationCertificate}");
            script.AppendLine("\tWITH");
            script.AppendLine($"\tSUBJECT = '{ServiceBrokerTransportAuthenticationCertificate}',");
            script.AppendLine("\tSTART_DATE = '20200101',");
            script.AppendLine("\tEXPIRY_DATE = '20500101'");
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.service_broker_endpoints WHERE name = '{name}{port}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE ENDPOINT {name}{port}");
            script.AppendLine("\tSTATE = STARTED");
            script.AppendLine("\tAS TCP");
            script.AppendLine("\t(");
            script.AppendLine($"\t\tLISTENER_PORT = {port}");
            script.AppendLine("\t)");
            script.AppendLine("\tFOR SERVICE_BROKER");
            script.AppendLine("\t(");
            script.AppendLine($"\t\tAUTHENTICATION = CERTIFICATE {ServiceBrokerTransportAuthenticationCertificate},");
            script.AppendLine($"\t\tENCRYPTION = DISABLED");
            script.AppendLine("\t)");
            script.AppendLine($"\tGRANT CONNECT ON ENDPOINT::{name}{port} TO [PUBLIC];");
            script.Append("END");
            return script.ToString();
		}
        internal static string CreateDatabaseScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("USE [master];");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE name = '{SERVICE_BROKER_DATABASE}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE DATABASE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"\tALTER DATABASE [{SERVICE_BROKER_DATABASE}] SET ENABLE_BROKER;");
            script.Append("END");
            return script.ToString();

            // create user to consume messaging services from external app
            //script.AppendLine("USE [master];");
            //script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = 'one-c-sharp-user')");
            //script.AppendLine("BEGIN");
            //script.AppendLine(//CREATE LOGIN [one-c-sharp-user] WITH PASSWORD = 'one-c-sharp';);
            //script.AppendLine("END");
            //script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            //CREATE USER [one-c-sharp-user] FOR LOGIN [one-c-sharp-user];
            //ALTER ROLE [db_owner] ADD MEMBER [one-c-sharp-user]; 
        }
        internal static string CreateDatabaseUserScript(Guid brokerId)
        {
            string userName = CreateBrokerUserName(brokerId);

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine("IF NOT EXISTS(SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')");
            script.AppendLine("BEGIN");
            script.AppendLine("\tCREATE MASTER KEY ENCRYPTION BY PASSWORD = 'ServiceBrokerDatabaseMasterKey';");
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = '{userName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE USER [{userName}] WITHOUT LOGIN;");
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE name = '{ServiceBrokerDatabaseAuthenticationCertificate}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE CERTIFICATE {ServiceBrokerDatabaseAuthenticationCertificate}");
            script.AppendLine($"\t\tAUTHORIZATION [{userName}]");
            script.AppendLine("\t\tWITH");
            script.AppendLine($"\t\tSUBJECT = '{ServiceBrokerDatabaseAuthenticationCertificate}',");
            script.AppendLine("\t\tSTART_DATE = '20200101',");
            script.AppendLine("\t\tEXPIRY_DATE = '20500101';");
            script.Append("END");
            return script.ToString();
        }
        internal static string CreateChannelsTableScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE type = 'U' AND name = '{CHANNELS_TABLE}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE TABLE [dbo].[{CHANNELS_TABLE}]");
            script.AppendLine("\t(");
            script.AppendLine("\t\t[id]     int IDENTITY(1,1) NOT NULL,");
            script.AppendLine("\t\t[name]   nvarchar(256)     NOT NULL,");
            script.AppendLine("\t\t[handle] uniqueidentifier  NOT NULL,");
            script.AppendLine("\t\tCONSTRAINT [pk_channels] PRIMARY KEY CLUSTERED ([id] ASC)");
            script.AppendLine("\t);");
            script.AppendLine($"\tCREATE UNIQUE NONCLUSTERED INDEX [unx_channels_name] ON [dbo].[{CHANNELS_TABLE}]([name] ASC);");
            script.Append("END");
            return script.ToString();
        }
        internal static string SelectServiceBrokerIdentifierScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"SELECT service_broker_guid FROM sys.databases WHERE [name] = '{SERVICE_BROKER_DATABASE}';");
            return script.ToString();
        }
        internal static string SelectQueuesScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("SELECT name, is_poison_message_handling_enabled, is_retention_enabled, ");
            script.Append("is_activation_enabled, activation_procedure, max_readers, execute_as_principal_id, ");
            script.Append("is_receive_enabled, is_enqueue_enabled ");
            script.Append("FROM sys.service_queues WHERE is_ms_shipped = 0;");
            return script.ToString();
        }
        internal static string CreateServiceQueueScript(Guid brokerId, string name)
        {
            string userName = CreateBrokerUserName(brokerId);
            string queueName = CreateQueueName(brokerId, name);
            string serviceName = CreateServiceName(brokerId, name);

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.service_queues WHERE name = '{queueName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE QUEUE [{queueName}] WITH POISON_MESSAGE_HANDLING (STATUS = OFF);");
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = '{serviceName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE SERVICE [{serviceName}] ON QUEUE [{queueName}] ([DEFAULT]);");
            script.AppendLine($"\tGRANT CONTROL ON SERVICE::[{serviceName}] TO [{userName}];");
            script.AppendLine($"\tGRANT SEND ON SERVICE::[{serviceName}] TO [PUBLIC];");
            script.AppendLine("END");

            // TODO: backup certificate to binary !

            // for initiator side:
            // 1. create user for remote service
            // 2. certificate for remote service user from binary backuped at target side !
            // 3. create remote service binding

            return script.ToString();
        }

        internal static string SelectCertificateBinaryDataScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine("SELECT");
            script.AppendLine("\tCONVERT(NVARCHAR(MAX), CERTENCODED(certificate_id), 1)");
            script.AppendLine("FROM sys.certificates");
            script.AppendLine($"WHERE name = '{ServiceBrokerDatabaseAuthenticationCertificate}';");
            return script.ToString();

            //SELECT 'CREATE CERTIFICATE ' +
            //QUOTENAME(C.name) +
            //' FROM BINARY = ' +
            //CONVERT(NVARCHAR(MAX), CERTENCODED(C.certificate_id), 1) +
            //';' AS create_cmd
            //FROM sys.certificates AS C
            //WHERE C.name = 'ServiceBrokerDatabaseAuthenticationCertificate';
        }
        internal static string CreateRemoteUserScript(Guid targetBroker, string certificateBinaryData)
        {
            string userName = CreateBrokerUserName(targetBroker);
            string certificateName = CreateRemoteUserCertificateName(targetBroker);

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");

            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = '{userName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE USER [{userName}] WITHOUT LOGIN;");
            script.AppendLine("END");

            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE name = '{certificateName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE CERTIFICATE [{certificateName}]");
            script.AppendLine($"\t\tAUTHORIZATION [{userName}]");
            script.AppendLine($"\t\tFROM BINARY = {certificateBinaryData};");
            script.Append("END");
            return script.ToString();
        }
        internal static string CreateRouteScript(string routeName, Guid sourceBroker, string sourceQueue, string targetAddress, Guid targetBroker, string targetQueue)
        {
            string targetUserName = CreateBrokerUserName(targetBroker);
            string targetServiceName = CreateServiceName(targetBroker, targetQueue);
            string sourceServiceName = CreateServiceName(sourceBroker, sourceQueue);

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.routes WHERE name = '{routeName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE ROUTE [{routeName}] WITH");
            script.AppendLine($"\t\tSERVICE_NAME = '{targetServiceName}',");
            script.AppendLine($"\t\tADDRESS = 'TCP://{targetAddress}';"); // TCP://targetserver:4022
            script.AppendLine("END");
            script.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.remote_service_bindings WHERE name = '{routeName}')");
            script.AppendLine("BEGIN");
            script.AppendLine($"\tCREATE REMOTE SERVICE BINDING [{routeName}]");
            script.AppendLine($"\t\tTO SERVICE '{targetServiceName}'");
            script.AppendLine($"\t\tWITH");
            script.AppendLine($"\t\t\tUSER = [{targetUserName}],");
            script.AppendLine("\t\t\tANONYMOUS = ON;");
            script.AppendLine("END");
            script.AppendLine("DECLARE @handle UNIQUEIDENTIFIER = NULL;");
            script.AppendLine($"SELECT @handle = handle FROM [dbo].[channels] WHERE name = N'{routeName}';");
            script.AppendLine("IF (@handle IS NULL)");
            script.AppendLine("BEGIN");
            script.AppendLine("\tSET XACT_ABORT ON;");
            script.AppendLine("\tBEGIN TRY");
            script.AppendLine("\t\tBEGIN TRANSACTION;");
            script.AppendLine("\t\tBEGIN DIALOG @handle");
            script.AppendLine($"\t\tFROM SERVICE [{sourceServiceName}]");
            script.AppendLine($"\t\tTO SERVICE '{targetServiceName}', '{targetBroker}'");
            script.AppendLine("\t\tON CONTRACT [DEFAULT]");
            script.AppendLine("\t\tWITH ENCRYPTION = OFF;");
            script.AppendLine($"\t\tINSERT [dbo].[{CHANNELS_TABLE}] (name, handle)");
            script.AppendLine($"\t\tVALUES (N'{routeName}', @handle);");
            script.AppendLine("\t\tCOMMIT TRANSACTION;");
            script.AppendLine("\tEND TRY");
            script.AppendLine("\tBEGIN CATCH");
            script.AppendLine("\t\tIF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION;");
            script.AppendLine("\t\tTHROW;");
            script.AppendLine("\tEND CATCH");
            script.Append("END");
            return script.ToString();
        }

        internal static string SelectDialogHandleScript(string routeName)
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"SELECT [handle] FROM [dbo].[{CHANNELS_TABLE}] WHERE name = N'{routeName}';");
            return script.ToString();
        }
        internal static string SendMessageToChannelScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{SERVICE_BROKER_DATABASE}];");
            script.AppendLine($"SEND ON CONVERSATION @handle MESSAGE TYPE [DEFAULT] (CAST(@message AS varbinary(max)));");
            return script.ToString();
        }
        internal static string SimpleReceiveMessageScript(string queueFullName)
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("DECLARE @handle uniqueidentifier;");
            script.AppendLine("DECLARE @message_type nvarchar(256);");
            script.AppendLine("DECLARE @message_body NVARCHAR(max) = N'';");
            script.AppendLine("SET XACT_ABORT ON;");
            script.AppendLine("BEGIN TRY");
            script.AppendLine("\tBEGIN TRANSACTION;"); // begin transaction !!!
            script.AppendLine("\tWAITFOR (RECEIVE TOP (1)"); // receive 1 message at once
            script.AppendLine("\t\t@handle = conversation_handle,");
            script.AppendLine("\t\t@message_type = message_type_name,");
            script.AppendLine("\t\t@message_body = CAST(message_body AS nvarchar(max))");
            script.AppendLine($"\tFROM [{queueFullName}]");
            script.AppendLine("\t), TIMEOUT 1000;"); // 1 second wait timeout
            script.AppendLine("\tIF (@@ROWCOUNT = 0)");
            script.AppendLine("\tBEGIN");
            script.AppendLine("\t\tROLLBACK TRANSACTION;");
            script.AppendLine("\t\tRETURN;");
            script.AppendLine("\tEND");
            script.AppendLine("\tELSE IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR");
            script.AppendLine("\t\t\t@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')");
            script.AppendLine("\tBEGIN");
            script.AppendLine("\t\tEND CONVERSATION @handle;");
            script.AppendLine("\tEND");
            script.AppendLine("\tSELECT @message_body;");
            script.AppendLine("\tCOMMIT TRANSACTION;");
            script.AppendLine("END TRY");
            script.AppendLine("BEGIN CATCH");
            script.AppendLine("\tIF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION;");
            script.AppendLine("\tTHROW;");
            script.AppendLine("END CATCH");
            return script.ToString();
        }
        internal static string ReceiveOneMessageScript(string queueFullName, int timeoutMilliseconds)
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("DECLARE @handle uniqueidentifier;");
            script.AppendLine("DECLARE @message_type nvarchar(256);");
            script.AppendLine("DECLARE @message_body NVARCHAR(max);");
            script.AppendLine($"WAITFOR (RECEIVE TOP (1)");
            script.AppendLine("\t@handle = conversation_handle,");
            script.AppendLine("\t@message_type = message_type_name,");
            script.AppendLine("\t@message_body = CAST(message_body AS nvarchar(max))");
            script.AppendLine($"FROM [{queueFullName}]");
            script.AppendLine($"), TIMEOUT {timeoutMilliseconds};"); // 1000 milliseconds = 1 second
            script.AppendLine("IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR");
            script.AppendLine("\t\t@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')");
            script.AppendLine("BEGIN");
            script.AppendLine("\tEND CONVERSATION @handle;");
            script.AppendLine("END");
            script.AppendLine("SELECT @message_body;");
            return script.ToString();
        }
        internal static string ReceiveListOfMessagesScript(string queueFullName, int numberOfMessages, int timeoutMilliseconds)
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine($"WAITFOR (RECEIVE TOP ({numberOfMessages})");
            script.AppendLine("\tconversation_handle AS [dialog_handle],");
            script.AppendLine("\tmessage_type_name AS [message_type],");
            script.AppendLine("\tCAST(message_body AS nvarchar(max)) AS [message_body]");
            script.AppendLine($"FROM [{queueFullName}]");
            script.AppendLine($"), TIMEOUT {timeoutMilliseconds};"); // 1000 milliseconds = 1 second
            return script.ToString();
        }
    }
}