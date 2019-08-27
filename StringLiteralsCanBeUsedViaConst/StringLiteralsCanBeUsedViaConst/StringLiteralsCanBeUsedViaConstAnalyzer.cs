using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringLiteralsCanBeUsedViaConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringLiteralsCanBeUsedViaConstAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "StringLiteralsCanBeUsedViaConst";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Maintainability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Argument);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (!(node is ArgumentSyntax methodArgument))
            {
                return;
            }

            if (!(methodArgument.Expression is LiteralExpressionSyntax literalExpressionSyntax))
            {
                return;
            }

            var literals = literalExpressionSyntax.GetText().ToString();
            var root = context.SemanticModel.SyntaxTree.GetRoot();

            var reportNodes = root.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(lit => lit.Token.ToString().Equals(literals, StringComparison.Ordinal))
                .ToArray();

            if (reportNodes.Count() < 2)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, methodArgument.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
