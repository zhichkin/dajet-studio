using Microsoft.SqlServer.TransactSql.ScriptDom;
using DaJet.Metadata;
using System;
using System.Collections.Generic;

namespace DaJet.Scripting
{
    internal sealed class InsertSpecificationVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal InsertSpecificationVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return new List<string>() { "Target", "Columns", "InsertSource" }; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            InsertSpecification insert = node as InsertSpecification;
            if (insert == null) return result;

            StatementNode statement = new StatementNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty
            };
            if (result is ScriptNode script)
            {
                if (parent is InsertStatement)
                {
                    script.Statements.Add(statement);
                    return statement;
                }
            }
            return result;
        }
    }
}