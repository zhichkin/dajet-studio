﻿using System.Collections.Generic;

namespace DaJet.Metadata
{
    public sealed class DatabaseServer
    {
        public string Name { get; set; }
        public string Address { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<DatabaseInfo> Databases { get; set; } = new List<DatabaseInfo>();
    }
}