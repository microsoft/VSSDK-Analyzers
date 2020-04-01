// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Simplification;

    /// <summary>
    /// Offers code fixes for diagnostics produced by the <see cref="VSSDK006CheckServicesExistCodeFix"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class VSSDK006CheckServicesExistCodeFix : CodeFixProvider
    {
        private static readonly ImmutableArray<string> ReusableFixableDiagnosticIds = ImmutableArray.Create(
          VSSDK006CheckServicesExistAnalyzer.Descriptor.Id);

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ReusableFixableDiagnosticIds;

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            SyntaxNode? presentArgument = node is VariableDeclaratorSyntax declaratorSyntax ? SyntaxFactory.IdentifierName(declaratorSyntax.Identifier)
                : node.Parent is InvocationExpressionSyntax ? null // direct GetService result invocation
                : node is NameSyntax ? node
                : node is MemberAccessExpressionSyntax ? node
                : null;

            if (presentArgument != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add Assumes.Present after assignment",
                        ct => AppendAfterAssignmentAsync(context, node.FirstAncestorOrSelf<StatementSyntax>(), presentArgument, ct),
                        "After"),
                    diagnostic);
            }
        }

        private static async Task<Document> AppendAfterAssignmentAsync(CodeFixContext context, StatementSyntax relativeTo, SyntaxNode presentArgument, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
            Document document = context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode assumesStatement = CreateAssumesPresentStatement(editor.Generator, presentArgument);
            root = root.InsertNodesAfter(relativeTo, new SyntaxNode[] { assumesStatement });
            document = document.WithSyntaxRoot(root);
            document = await ImportAdder.AddImportsAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken);
            return document;
        }

        private static SyntaxNode CreateAssumesPresentStatement(SyntaxGenerator generator, SyntaxNode possiblyNullVariable)
        {
            return generator.ExpressionStatement(
                generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.QualifiedName(generator.IdentifierName(Types.Assumes.Namespace.Single()), generator.IdentifierName(Types.Assumes.TypeName)),
                        Types.Assumes.Present),
                    generator.Argument(possiblyNullVariable)))
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }
}
