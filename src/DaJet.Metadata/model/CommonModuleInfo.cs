namespace DaJet.Metadata
{
    public sealed class CommonModuleInfo
    {
        public CommonModuleInfo(string uuid, string name)
        {
            UUID = uuid;
            Name = name;
        }
        public string UUID { get; }
        public string Name { get; }
        public override string ToString()
        {
            return Name + " {" + UUID + "}";
        }
    }
}