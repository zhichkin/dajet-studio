using System.Reflection;

namespace DaJet.Messaging
{
    public sealed class QueueInfo
    {
        /// <summary>
        /// The name of the queue. This name must meet the guidelines for SQL Server identifiers.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Specifies whether the queue is available (ON) or unavailable (OFF).
        /// </summary>
        public bool Status { get; set; } = true;
        /// <summary>
        /// Specifies the retention setting for the queue.
        /// </summary>
        public bool Retention { get; set; } = false;
        /// <summary>
        /// Specifies information about which stored procedure you have to start to process messages in this queue.
        /// </summary>
        public bool Activation { get; set; } = false;
        /// <summary>
        /// Specifies the name of the stored procedure to start to process messages in this queue. This value must be a SQL Server identifier.
        /// </summary>
        public string ProcedureName { get; set; }
        /// <summary>
        /// Specifies the maximum number of instances of the activation stored procedure that the queue starts at the same time.
        /// </summary>
        public short MaxQueueReaders { get; set; } = 1;
        /// <summary>
        /// Specifies whether poison message handling is enabled for the queue.
        /// </summary>
        public bool PoisonMessageHandling { get; set; } = false;
        public override string ToString() { return Name; }
        public QueueInfo Copy()
        {
            QueueInfo copy = new QueueInfo();
            this.CopyTo(copy);
            return copy;
        }
        public void CopyTo(QueueInfo database)
        {
            foreach (PropertyInfo property in typeof(QueueInfo).GetProperties())
            {
                if (property.IsList()) continue;
                if (!property.CanWrite) continue;
                property.SetValue(database, property.GetValue(this));
            }
        }
    }
}