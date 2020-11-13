using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;

namespace DaJet.Scripting
{
    public sealed class DeclareVariableStatementVisitor : TSqlFragmentVisitor
    {
        private Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        public DeclareVariableStatementVisitor() { }
        public DeclareVariableStatementVisitor(Dictionary<string, object> parameters) : this()
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
        public override void ExplicitVisit(DeclareVariableStatement node)
        {
            foreach (DeclareVariableElement declaration in node.Declarations)
            {
                string parameterName = declaration.VariableName.Value.TrimStart('@');
                if (Parameters.TryGetValue(parameterName, out object parameterValue))
                {
                    if (declaration.Value is StringLiteral stringLiteral)
                    {
                        stringLiteral.Value = (string)parameterValue;
                    }
                    else if (declaration.Value is IntegerLiteral integerLiteral)
                    {
                        integerLiteral.Value = (string)parameterValue;
                    }
                    else if (declaration.Value is NumericLiteral numericLiteral)
                    {
                        numericLiteral.Value = (string)parameterValue;
                    }
                    else if (declaration.Value is MoneyLiteral moneyLiteral)
                    {
                        moneyLiteral.Value = (string)parameterValue;
                    }
                }
                else
                {
                    Parameters.Add("\"" + parameterName + "\" : \"" + declaration.DataType.Name.BaseIdentifier.Value + "\"", null);
                }
            }
            base.ExplicitVisit(node);
        }
        public string GenerateJsonParametersObject()
        {
            string json = string.Empty;
            foreach (string parameterName in Parameters.Keys)
            {
                json += (string.IsNullOrEmpty(json)) ? "\t" : ",\n\t";
                json += parameterName;
            }
            json = "{\n" + json + "\n}";
            return json;
        }
    }
}