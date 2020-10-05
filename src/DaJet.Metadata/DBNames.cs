using System.Collections.Generic;

namespace DaJet.Metadata
{
    internal sealed class DBNameEntry
    {
        internal MetaObject MetaObject = new MetaObject();
        internal List<DBName> DBNames = new List<DBName>();
        public override string ToString()
        {
            return string.Format("{0} {{{1}:{2}}}", MetaObject.Token, MetaObject.TypeCode, MetaObject.UUID);
        }
    }
    internal sealed class DBName
    {
        internal string Token;
        internal int TypeCode;
        internal bool IsMainTable;
        public override string ToString()
        {
            return string.Format("{0}({1})", Token, TypeCode);
        }
    }
    internal static class DBToken
    {
        internal const string L = "L";
        internal const string N = "N";
        internal const string S = "S";
        internal const string D = "D"; // Config metadata (SDBL)
        internal const string T = "T"; // SQL tables fields (RDBMS)
        internal const string B = "B";
        internal const string RRef = "RRef";
        internal const string TRef = "TRef";
        internal const string RRRef = "RRRef";
        internal const string RTRef = "RTRef";
        internal const string TYPE = "TYPE";

        internal const string Fld = "Fld";
        internal const string IDRRef = "IDRRef";
        internal const string Version = "Version";
        internal const string Marked = "Marked";
        internal const string DateTime = "DateTime";
        internal const string NumberPrefix = "NumberPrefix";
        internal const string Number = "Number";
        internal const string Posted = "Posted";
        internal const string PredefinedID = "PredefinedID";
        internal const string Description = "Description";
        internal const string Code = "Code";
        internal const string OwnerID = "OwnerID";
        internal const string Folder = "Folder";
        internal const string ParentIDRRef = "ParentIDRRef";

        internal const string KeyField = "KeyField";
        internal const string LineNo = "LineNo";
        internal const string EnumOrder = "EnumOrder";

        internal const string Period = "Period";
        internal const string Recorder = "Recorder";
        internal const string RecorderRRef = "RecorderRRef";
        internal const string RecorderTRef = "RecorderTRef";
        internal const string Active = "Active";
        internal const string RecordKind = "RecordKind";

        internal const string VT = "VT";
        internal const string Enum = "Enum";
        internal const string Chrc = "Chrc";
        internal const string Const = "Const";
        internal const string InfoRg = "InfoRg";
        internal const string AccumRg = "AccumRg";
        internal const string Document = "Document";
        internal const string Reference = "Reference";
        internal const string Node = "Node";
        internal const string ChngR = "ChngR";
    }
}