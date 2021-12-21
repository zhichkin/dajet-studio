using System.Reflection;

namespace DaJet.UI.Model
{
    public sealed class RabbitMQServer
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 15672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Description { get; set; } = "Local RabbitMQ server";
        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }
        public RabbitMQServer Copy()
        {
            RabbitMQServer server = new RabbitMQServer();
            CopyTo(server);
            return server;
        }
        public void CopyTo(RabbitMQServer server)
        {
            foreach (PropertyInfo property in typeof(RabbitMQServer).GetProperties())
            {
                if (!property.CanWrite) continue;
                property.SetValue(server, property.GetValue(this));
            }
        }
    }
}