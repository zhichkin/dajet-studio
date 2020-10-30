using DaJet.Studio.MVVM;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DaJet.Studio
{
    public sealed class TSqlFragmentTreeBuilder
    {
        public void Build(TSqlFragment fragment, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel() { NodeText = "ROOT" };
            VisitRecursively(fragment, treeNode);
            // VisitIteratively using queue ...
        }
        private void VisitRecursively(TSqlFragment node, TreeNodeViewModel result)
        {
            if (node == null) return;
            VisitChildren(node, VisitNode(node, result));
        }
        /// <summary>
        /// Transforms syntax node into tree view node
        /// </summary>
        /// <param name="node">Syntax node to transform</param>
        /// <param name="result">Result of transformation</param>
        /// <returns></returns>
        private TreeNodeViewModel VisitNode(TSqlFragment node, TreeNodeViewModel result)
        {
            Type type = node.GetType();
            TreeNodeViewModel child = new TreeNodeViewModel()
            {
                NodeText = type.Name
            };
            PropertyInfo property = type.GetProperty("Value");
            if (property != null)
            {
                child.NodeText += " = " + property.GetValue(node).ToString();
            }
            result.TreeNodes.Add(child);
            return child;
        }
        private void VisitChildren(TSqlFragment parent, TreeNodeViewModel result)
        {
            Type type = parent.GetType();

            // visit children which resides in properties
            foreach (PropertyInfo property in type.GetProperties())
            {
                // ignore indexed properties
                if (property.GetIndexParameters().Length > 0) // property is an indexer
                {
                    // indexer property name is "Item" and it has parameters
                    continue;
                }

                // get type of property
                Type propertyType = property.PropertyType;
                bool isList = (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IList<>));
                if (isList) { propertyType = propertyType.GetGenericArguments()[0]; }

                // continue if child is not TSqlFragment
                if (!propertyType.IsSubclassOf(typeof(TSqlFragment))) { continue; }

                // check if property is null
                object child = property.GetValue(parent);
                if (child == null) { continue; }

                // visit property or collection
                if (isList)
                {
                    IList list = (IList)child;
                    for (int i = 0; i < list.Count; i++)
                    {
                        object item = list[i];
                        VisitRecursively((TSqlFragment)item, result);
                    }
                }
                else
                {
                    VisitRecursively((TSqlFragment)child, result);
                }
            }
        }
    }
}