// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Offers code fixes for diagnostics produced by the <see cref="VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class VSSDK002PackageRegistrationMatchesBaseTypeCodeFix : CodeFixProvider
    {
        private static readonly ImmutableArray<string> ReusableFixableDiagnosticIds = ImmutableArray.Create(
          VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer.Descriptor.Id);

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ReusableFixableDiagnosticIds;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();

            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            ClassDeclarationSyntax classDeclarationSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclarationSyntax != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Fix attribute to match package type", ct => this.UpdateAttributeAsync(context, diagnostic, ct), "only one"),
                    diagnostic);
            }
        }

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> UpdateAttributeAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            bool isAsyncPackage = diagnostic.Properties[VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer.BaseTypeDiagnosticPropertyName] == Types.AsyncPackage.TypeName;
            LiteralExpressionSyntax appropriateArgument = SyntaxFactory.LiteralExpression(isAsyncPackage ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

            SyntaxNode root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            switch (root.FindNode(diagnostic.Location.SourceSpan))
            {
                case AttributeSyntax packageRegistrationSyntax:
                    // We need to add an argument to this attribute
                    root = root.ReplaceNode(
                        packageRegistrationSyntax,
                        packageRegistrationSyntax.AddArgumentListArguments(
                            SyntaxFactory.AttributeArgument(appropriateArgument).WithNameEquals(SyntaxFactory.NameEquals(Types.PackageRegistrationAttribute.AllowsBackgroundLoading))));
                    break;
                case AttributeArgumentSyntax allowBackgroundLoadingSyntax:
                    if (isAsyncPackage)
                    {
                        // We need to update this argument on the attribute.
                        AttributeSyntax originalAttribute = allowBackgroundLoadingSyntax.FirstAncestorOrSelf<AttributeSyntax>();
                        root = root.ReplaceNode(allowBackgroundLoadingSyntax, allowBackgroundLoadingSyntax.WithExpression(appropriateArgument));
                    }
                    else
                    {
                        // Let's just remove it since false is its default value.
                        root = root.RemoveNode(allowBackgroundLoadingSyntax, SyntaxRemoveOptions.KeepNoTrivia);
                    }

                    break;
                case SyntaxNode node:
                    throw new InvalidOperationException("Unexpected syntax type: " + node.GetType().Name);
            }

            return context.Document.WithSyntaxRoot(root);
        }
    }
}
