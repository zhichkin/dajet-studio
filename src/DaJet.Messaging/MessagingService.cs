using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace OneCSharp.Messaging
{
    public interface IMessagingService
    {
        string CurrentServer { get; }
        string ConnectionString { get; }
        void UseServer(string address);
        void SetupServiceBroker();
        void CreatePublicEndpoint(string name, int port);
        void CreateQueue(string name);
        void SendMessage(string routeName, string payload);
    }
    public sealed class MessagingService : IMessagingService
    {
        private const string ERROR_SERVER_IS_NOT_DEFINED = "Server is not defined. Try to call \"UseServer\" method first.";
        public string CurrentServer { get; private set; } = string.Empty;
        public string ConnectionString { get; private set; } = string.Empty;
        public void UseServer(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address));

            SqlConnectionStringBuilder csb;
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                csb = new SqlConnectionStringBuilder() { IntegratedSecurity = true };
            }
            else
            {
                csb = new SqlConnectionStringBuilder(ConnectionString);
            }
            csb.DataSource = address;
            ConnectionString = csb.ToString();

            CurrentServer = address;
        }

        public void CreatePublicEndpoint(string name, int port)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            { /* start of limited scope */

                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = SqlScripts.CreatePublicEndpointScript(name, port);
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
            } /* end of limited scope */
        }
        public void SetupServiceBroker()
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            SqlScripts.ExecuteScript(ConnectionString, SqlScripts.CreateDatabaseScript());
            Guid brokerId = SqlScripts.ExecuteScalar<Guid>(ConnectionString, SqlScripts.SelectServiceBrokerIdentifierScript());
            SqlScripts.ExecuteScript(ConnectionString, SqlScripts.CreateDatabaseUserScript(brokerId));
            SqlScripts.ExecuteScript(ConnectionString, SqlScripts.CreateChannelsTableScript());
        }
        public void CreateQueue(string name)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            Guid brokerId = SqlScripts.ExecuteScalar<Guid>(ConnectionString, SqlScripts.SelectServiceBrokerIdentifierScript());
            SqlScripts.ExecuteScript(ConnectionString, SqlScripts.CreateServiceQueueScript(brokerId, name));
        }
        public string GetUserCertificateBinaryData()
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            return SqlScripts.ExecuteScalar<string>(ConnectionString, SqlScripts.SelectCertificateBinaryDataScript());
        }
        public void CreateRemoteUser(string targetQueueFullName, string certificateBinaryData)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            Guid targetBroker = SqlScripts.GetBrokerId(targetQueueFullName);
            SqlScripts.ExecuteScript(ConnectionString, SqlScripts.CreateRemoteUserScript(targetBroker, certificateBinaryData));
        }
        public void CreateRoute(string name, string sourceQueueFullName, string targetAddress, string targetQueueFullName)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            Guid sourceBroker = SqlScripts.GetBrokerId(sourceQueueFullName);
            Guid targetBroker = SqlScripts.GetBrokerId(targetQueueFullName);
            string sourceQueueName = SqlScripts.GetQueueName(sourceQueueFullName);
            string targetQueueName = SqlScripts.GetQueueName(targetQueueFullName);
            
            string script = SqlScripts.CreateRouteScript(name, sourceBroker, sourceQueueName, targetAddress, targetBroker, targetQueueName);
            SqlScripts.ExecuteScript(ConnectionString, script);
        }

        public void SendMessage(string routeName, string payload)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            Guid handle = SqlScripts.ExecuteScalar<Guid>(ConnectionString, SqlScripts.SelectDialogHandleScript(routeName));
            string sql = SqlScripts.SendMessageToChannelScript();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("handle", handle);
            parameters.Add("message", payload);
            SqlScripts.ExecuteScript(ConnectionString, sql, parameters);
        }
        private string AlterDatabaseConnectionString()
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString);
            csb.InitialCatalog = SqlScripts.SERVICE_BROKER_DATABASE;
            return csb.ToString();
        }
        public IMessageConsumer CreateMessageConsumer(string queueName)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            Guid brokerId = SqlScripts.ExecuteScalar<Guid>(ConnectionString, SqlScripts.SelectServiceBrokerIdentifierScript());
            string queueFullName = SqlScripts.CreateQueueName(brokerId, queueName);
            return new MessageConsumer(CurrentServer, queueFullName);
        }
        public string ReceiveMessage(string queueFullName)
        {
            if (string.IsNullOrWhiteSpace(CurrentServer)) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            string connectionString = AlterDatabaseConnectionString();
            string message = SqlScripts.ExecuteScalar<string>(connectionString, SqlScripts.SimpleReceiveMessageScript(queueFullName));
            return message;
        }
        public void Commit()
        {
            // TODO: commit active transaction begun by ReceiveMessage method ...
        }
        
        public void CreateTopic()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}