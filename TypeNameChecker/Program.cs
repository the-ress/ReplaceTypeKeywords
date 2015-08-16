﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace TypeNameChecker
{
    internal class Program
    {
        internal static void Main(String[] args)
        {
            var fileNames = Directory.GetFiles(Environment.CurrentDirectory, "*.cs", SearchOption.AllDirectories);
            foreach (var fileName in fileNames)
            {
                String source;
                Encoding encoding;

                using (var inStream = new StreamReader(fileName, Encoding.Default, detectEncodingFromByteOrderMarks: true))
                {
                    source = inStream.ReadToEnd();
                    encoding = inStream.CurrentEncoding;
                }

                source = ReplaceTypeKeywords(source);
                File.WriteAllText(fileName, source, encoding);
            }
        }

        private static String ReplaceTypeKeywords(String source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            if (IsAutoGenerated(tree))
            {
                return source;
            }

            tree = tree.WithRootAndOptions(TraverseSyntaxTree(tree.GetRoot()), null);
            return tree.ToString();
        }

        private static Boolean IsAutoGenerated(SyntaxTree tree)
        {
            return tree.GetRoot()
                .GetLeadingTrivia()
                .Where(t =>
                    t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && t.ToString().IndexOf("<auto-generated>", StringComparison.OrdinalIgnoreCase) != -1
                )
                .Any();
        }

        private static SyntaxNode TraverseSyntaxTree(SyntaxNode node)
        {
            node = node.ReplaceNodes(node.ChildNodes(), (child, dummy) => TraverseSyntaxTree(child));
            return Visit(node);
        }

        private static SyntaxNode Visit(SyntaxNode node)
        {
            var pds = node as PredefinedTypeSyntax;
            if (pds != null)
            {
                if (pds.Keyword.ValueText == "void")
                {
                    return node;
                }

                String replacement;
                if (replacements.TryGetValue(pds.Keyword.ValueText, out replacement))
                {
                    node = SyntaxFactory
                        .IdentifierName(replacement)
                        .WithTriviaFrom(node);
                }
                else
                {
                    throw new InvalidOperationException($"ERROR: Type '{pds.Keyword.Value}' not defined.");
                }

            }

            return node;
        }

        private static Dictionary<String, String> replacements = new Dictionary<String, String>()
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
