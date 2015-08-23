﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReplaceTypeKeywords.Core
{
    public class TypeKeywordsReplacer
    {
        private String _source;
        private Boolean _typeKeywordsReplaced;

        public static String Process(String source)
            => new TypeKeywordsReplacer(source).GetReplacedSource();

        private TypeKeywordsReplacer(String source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
        }

        private String GetReplacedSource()
        {
            var tree = CSharpSyntaxTree.ParseText(_source);
            if (IsAutoGenerated(tree))
            {
                return _source;
            }

            var newRoot = TraverseSyntaxTree(tree.GetRoot());

            if (_typeKeywordsReplaced)
            {
                newRoot = EnsureUsingSystem(newRoot);
            }

            tree = tree.WithRootAndOptions(newRoot, null);
            return tree.ToString();
        }

        private Boolean IsAutoGenerated(SyntaxTree tree)
            => tree.GetRoot()
                .GetLeadingTrivia()
                .Where(t =>
                    t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && t.ToString().IndexOf("<auto-generated>", StringComparison.OrdinalIgnoreCase) != -1
                )
                .Any();

        private SyntaxNode TraverseSyntaxTree(SyntaxNode node)
        {
            node = node.ReplaceNodes(node.ChildNodes(), (child, dummy) => TraverseSyntaxTree(child));
            return Visit(node);
        }

        private SyntaxNode Visit(SyntaxNode node)
        {
            var pds = node as PredefinedTypeSyntax;
            if (pds != null)
            {
                return VisitPredefinedTypeSyntax(pds);
            }

            return node;
        }

        private SyntaxNode VisitPredefinedTypeSyntax(PredefinedTypeSyntax node)
        {
            if (node.Keyword.ValueText == "void")
            {
                return node;
            }

            String replacement;
            if (!replacements.TryGetValue(node.Keyword.ValueText, out replacement))
            {
                throw new InvalidOperationException($"ERROR: Type '{node.Keyword.Value}' not defined.");
            }

            _typeKeywordsReplaced = true;

            return SyntaxFactory
                .IdentifierName(replacement)
                .WithTriviaFrom(node);
        }

        private SyntaxNode EnsureUsingSystem(SyntaxNode rootNode)
        {
            var usings = rootNode.ChildNodes()
                .OfType<UsingDirectiveSyntax>()
                .ToArray();

            if (IsUsingSystemPresent(usings))
            {
                return rootNode;
            }

            var systemName = SyntaxFactory.IdentifierName(nameof(System))
                .WithLeadingTrivia(SyntaxFactory.Space);
            var systemUsing = SyntaxFactory.UsingDirective(systemName)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            SyntaxNode insertBeforeNode = null;
            foreach (var node in usings)
            {
                if (String.CompareOrdinal(nameof(System), node.Name.ToString()) <= 0)
                {
                    insertBeforeNode = node;
                    break;
                }
            }

            if (insertBeforeNode != null)
            {
                return rootNode.InsertNodesBefore(insertBeforeNode, new[] { systemUsing });
            }
            else if (usings.Any())
            {
                return rootNode.InsertNodesAfter(usings.Last(), new[] { systemUsing });
            }
            else
            {
                return rootNode.InsertNodesBefore(rootNode.ChildNodes().First(), new[] { systemUsing });
            }
        }

        private Boolean IsUsingSystemPresent(IEnumerable<UsingDirectiveSyntax> nodes)
        {
            foreach (var node in nodes)
            {
                if (
                    (node.Name.ToString() == nameof(System)) &&
                    (node.StaticKeyword.Kind() == SyntaxKind.None) &&
                    (node.Alias == null)
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<String, String> replacements
            = new Dictionary<String, String>()
            {
                ["object"] = typeof(Object).Name,
                ["sbyte"] = typeof(SByte).Name,
                ["byte"] = typeof(Byte).Name,
                ["short"] = typeof(Int16).Name,
                ["ushort"] = typeof(UInt16).Name,
                ["int"] = typeof(Int32).Name,
                ["uint"] = typeof(UInt32).Name,
                ["long"] = typeof(Int64).Name,
                ["ulong"] = typeof(UInt64).Name,
                ["char"] = typeof(Char).Name,
                ["bool"] = typeof(Boolean).Name,
                ["float"] = typeof(Single).Name,
                ["double"] = typeof(Double).Name,
                ["decimal"] = typeof(Decimal).Name,
                ["string"] = typeof(String).Name,
            };
    }
}