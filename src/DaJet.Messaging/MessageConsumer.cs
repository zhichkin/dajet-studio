using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace OneCSharp.Messaging
{
    public interface IMessageConsumer : IDisposable
    {
        string QueueName { get; }
        string Receive();
        IList<string> Receive(int numberOfMessages);
        IList<string> Receive(int numberOfMessages, int timeoutMilliseconds);
        void Commit();
        void Refuse();
    }
    public sealed class MessageConsumer : IMessageConsumer
    {
        private const string ERROR_CONNECTION_IS_CLOSED = "Database connection is closed. Try to call \"Receive\" method first.";
        private const string ERROR_TRANSACTION_IS_ACTIVE = "Database transaction is still active. Try to call \"Commit\" or \"Refuse\" method first.";
        private const string SQL_ERROR_PARALLEL_TRANSACTIONS_ARE_NOT_SUPPORTED = "SqlConnection does not support parallel transactions.";

        private SqlTransaction _transaction;
        private readonly SqlConnection _connection;
        
        internal MessageConsumer(string serverAddress, string queueFullName)
        {
            if (string.IsNullOrWhiteSpace(serverAddress)) throw new ArgumentNullException(nameof(serverAddress));
            if (string.IsNullOrWhiteSpace(queueFullName)) throw new ArgumentNullException(nameof(queueFullName));

            QueueName = queueFullName;
            _connection = new SqlConnection(CreateConnectionString(serverAddress));
        }
        private string CreateConnectionString(string serverAddress)
        {
            var csb = new SqlConnectionStringBuilder()
            {
                DataSource = serverAddress,
                InitialCatalog = SqlScripts.SERVICE_BROKER_DATABASE,
                IntegratedSecurity = true
            };
            return csb.ToString();
        }
        private void BeginTransaction()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
            try
            {
                _transaction = _connection.BeginTransaction();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == SQL_ERROR_PARALLEL_TRANSACTIONS_ARE_NOT_SUPPORTED)
                {
                    throw new InvalidOperationException(ERROR_TRANSACTION_IS_ACTIVE, ex);
                }
                else
                {
                    throw;
                }
            }
        }
        public string QueueName { get; private set; }
        public string Receive()
        {
            BeginTransaction();
            return DoReceive(1000);
        }
        public IList<string> Receive(int numberOfMessages)
        {
            BeginTransaction();
            return DoReceive(numberOfMessages, 1000);
        }
        public IList<string> Receive(int numberOfMessages, int timeoutMilliseconds)
        {
            BeginTransaction();
            return DoReceive(numberOfMessages, timeoutMilliseconds);
        }
        private string DoReceive(int timeoutMilliseconds)
        {
            string message_body = null;
            {
                SqlCommand command = null;
                SqlDataReader reader = null;
                try
                {
                    command = _connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = SqlScripts.ReceiveOneMessageScript(QueueName, timeoutMilliseconds);
                    command.Transaction = _transaction;

                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            message_body = (string)reader[0];
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows)
                        {
                            command.Cancel();
                        }
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                }
            }
            return message_body;
        }
        private IList<string> DoReceive(int numberOfMessages, int timeoutMilliseconds)
        {
            List<string> messages = new List<string>();
            {
                SqlCommand command = null;
                SqlDataReader reader = null;
                try
                {
                    command = _connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = SqlScripts.ReceiveListOfMessagesScript(QueueName, numberOfMessages, timeoutMilliseconds);
                    command.Transaction = _transaction;

                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        // 0 - dialog_handle : uniqueidentifier
                        // 1 - message_type  : nvarchar(256)
                        if (!reader.IsDBNull(2))
                        {
                            messages.Add(reader.GetString(2)); // message_body
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows)
                        {
                            command.Cancel();
                        }
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                }
            }
            return messages;
        }
        public void Commit()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                throw new InvalidOperationException(ERROR_CONNECTION_IS_CLOSED);
            }
            _transaction.Commit();
        }
        public void Refuse()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                throw new InvalidOperationException(ERROR_CONNECTION_IS_CLOSED);
            }
            _transaction.Rollback(); // may throw sql exceptions
        }
        public void Dispose()
        {
            if (_transaction != null) _transaction.Dispose();
            if (_connection != null) _connection.Dispose();
        }
    }
}