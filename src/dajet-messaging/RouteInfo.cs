namespace DaJet.Messaging
{
    public sealed class RouteInfo
    {
        /// <summary>
        /// Name of the route, unique within the database.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Identifier of the broker that hosts the remote service.
        /// </summary>
        public string Broker { get; set; }
        /// <summary>
        /// Network address to which Service Broker sends messages for the remote service.
        /// </summary>
        public string Address { get; set; }
    }
}