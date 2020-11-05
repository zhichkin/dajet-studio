using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DaJet.Scripting
{
    public sealed class CreateProcedureStatementVisitor : TSqlFragmentVisitor
    {
        public string ProcedureName { get; private set; } = string.Empty;
        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            ProcedureName = node.ProcedureReference.Name.BaseIdentifier.Value;
            base.ExplicitVisit(node);
        }
        public override void ExplicitVisit(CreateOrAlterProcedureStatement node)
        {
            ProcedureName = node.ProcedureReference.Name.BaseIdentifier.Value;
            base.ExplicitVisit(node);
        }
    }
}