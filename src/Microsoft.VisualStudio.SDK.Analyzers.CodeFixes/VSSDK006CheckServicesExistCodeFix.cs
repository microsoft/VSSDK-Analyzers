// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace Microsoft.VisualStudio.SDK.Analyzers
{
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
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root is null || diagnostic.AdditionalLocations.Count == 0)
            {
                return;
            }

            // The first additional location points to the assignment target or variable declarator.
            SyntaxNode? node = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            if (node != null)
            {
                SyntaxNode? presentArgument = node is VariableDeclaratorSyntax declaratorSyntax ? SyntaxFactory.IdentifierName(declaratorSyntax.Identifier)
                    : node is NameSyntax ? node
                    : node is MemberAccessExpressionSyntax ? node
                    : null;

                if (presentArgument != null)
                {
                    StatementSyntax? statementSyntax = node.FirstAncestorOrSelf<StatementSyntax>();
                    if (statementSyntax != null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add Assumes.Present after assignment",
                                ct => AppendAfterAssignmentAsync(context, statementSyntax, presentArgument, ct),
                                "After"),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> AppendAfterAssignmentAsync(CodeFixContext context, StatementSyntax relativeTo, SyntaxNode presentArgument, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
            Document document = context.Document;
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
            Assumes.NotNull(root);
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
