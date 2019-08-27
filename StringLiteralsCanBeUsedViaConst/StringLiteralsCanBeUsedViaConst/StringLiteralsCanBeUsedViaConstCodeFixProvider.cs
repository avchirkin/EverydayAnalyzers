using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StringLiteralsCanBeUsedViaConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringLiteralsCanBeUsedViaConstCodeFixProvider)), Shared]
    public class StringLiteralsCanBeUsedViaConstCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Create constant";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(StringLiteralsCanBeUsedViaConstAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: async (ct) => await LiteralArgumentToConstAsync(document, diagnostic),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Document> LiteralArgumentToConstAsync(Document document, Diagnostic diagnostic)
        {
            var root = await document.GetSyntaxRootAsync();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var literalArgumentUsing = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
            if (literalArgumentUsing == null)
            {
                return document;
            }

            var oldClassNode = GetClassBySourcePosition(root, literalArgumentUsing.SpanStart);

            var literalText = literalArgumentUsing.GetText().ToString();
            var trimmedLiteralText = literalText.Trim('\"');
            
            const string constFieldName = "_someArg";
            var constField = DeclareAndInitializeConstField(constFieldName, trimmedLiteralText);

            var oldClassNodeMembers = oldClassNode.Members;
            var newClassNodeMembers = oldClassNodeMembers.Insert(0, constField);
            var newClassNode = oldClassNode.WithMembers(newClassNodeMembers);

            var updatingNodes = newClassNode.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(node => node.GetText().ToString().Equals(literalText, StringComparison.Ordinal))
                .Select(node => node.Parent)
                .OfType<ArgumentSyntax>()
                .ToArray();

            newClassNode = newClassNode.ReplaceNodes(updatingNodes, (oldNode, newNode) => oldNode.WithExpression(IdentifierName(constFieldName)));
            
            var newRoot = root.ReplaceNode(oldClassNode, newClassNode);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }

        private static FieldDeclarationSyntax DeclareAndInitializeConstField(string constFieldName, string literalText)
        {
            var privateToken = Token(SyntaxKind.PrivateKeyword);
            var constToken = Token(SyntaxKind.ConstKeyword);
            var constField =
                FieldDeclaration(
                        VariableDeclaration(
                                PredefinedType(
                                    Token(SyntaxKind.StringKeyword)))
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator(
                                            Identifier(constFieldName))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(literalText)))))))
                    .WithModifiers(TokenList(privateToken, constToken));

            var formattedLocal = constField.WithAdditionalAnnotations(Formatter.Annotation);
            return formattedLocal;
        }

        private ClassDeclarationSyntax GetClassBySourcePosition(SyntaxNode root, int position)
        {
            var typeNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(node => position >= node.Span.Start && position <= node.Span.End);

            return typeNode;
        }
    }
}
