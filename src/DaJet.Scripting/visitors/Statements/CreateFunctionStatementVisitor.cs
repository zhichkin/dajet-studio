using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DaJet.Scripting
{
    public sealed class CreateFunctionStatementVisitor : TSqlFragmentVisitor
    {
        public string FunctionName { get; private set; } = string.Empty;
        public override void ExplicitVisit(CreateFunctionStatement node)
        {
            FunctionName = node.Name.BaseIdentifier.Value;
            base.ExplicitVisit(node);
        }
    }
}