using DaJet.Metadata;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DaJet.Studio
{
    public sealed class WebSettings
    {
        public List<WebServer> WebServers { get; set; } = new List<WebServer>();
    }
    public sealed class WebServer
    {
        public Guid Identity { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Address { get; set; }
        public List<DatabaseServer> DatabaseServers { get; set; } = new List<DatabaseServer>();
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Address) ? Name : string.Format("{0} ({1})", Name, Address);
        }
        public WebServer Copy()
        {
            WebServer server = new WebServer();
            this.CopyTo(server);
            return server;
        }
        public void CopyTo(WebServer server)
        {
            foreach (PropertyInfo property in typeof(WebServer).GetProperties())
            {
                if (property.IsList()) continue;
                if (!property.CanWrite) continue;
                property.SetValue(server, property.GetValue(this));
            }
        }
    }
}