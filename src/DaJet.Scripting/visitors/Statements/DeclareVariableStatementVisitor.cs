using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace DaJet.Scripting
{
    public sealed class DeclareVariableStatementVisitor : TSqlFragmentVisitor
    {
        private List<string> Parameters { get; } = new List<string>();
        public override void ExplicitVisit(DeclareVariableStatement node)
        {
            foreach (DeclareVariableElement declaration in node.Declarations)
            {
                Parameters.Add("\"" + declaration.VariableName.Value.TrimStart('@') + "\" : \"" + declaration.DataType.Name.BaseIdentifier.Value + "\"");
            }
            base.ExplicitVisit(node);
        }
        public string GenerateJsonParametersObject()
        {
            string json = string.Empty;
            foreach (string parameter in Parameters)
            {
                json += (string.IsNullOrEmpty(json)) ? "\t" : ",\n\t";
                json += parameter;
            }
            json = "{\n" + json + "\n}";
            return json;
        }
    }
}