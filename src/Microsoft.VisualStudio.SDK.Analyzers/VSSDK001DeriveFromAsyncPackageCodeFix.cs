// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Simplification;

    /// <summary>
    /// Offers code fixes for diagnostics produced by the <see cref="VSSDK001DeriveFromAsyncPackageAnalyzer"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class VSSDK001DeriveFromAsyncPackageCodeFix : CodeFixProvider
    {
        private static readonly ImmutableArray<string> ReusableFixableDiagnosticIds = ImmutableArray.Create(
          VSSDK001DeriveFromAsyncPackageAnalyzer.Descriptor.Id);

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ReusableFixableDiagnosticIds;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var baseTypeSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            var classDeclarationSyntax = baseTypeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create("Change base type to AsyncPackage", ct => this.ChangeBaseTypeAsync(context.Document, diagnostic, ct), classDeclarationSyntax.Identifier.ToString()),
                diagnostic);
        }

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> ChangeBaseTypeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var baseTypeSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            var classDeclarationSyntax = baseTypeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            var asyncPackageBaseTypeSyntax = SyntaxFactory.SimpleBaseType(Utils.QualifyName(Types.AsyncPackage.Namespace, SyntaxFactory.IdentifierName(Types.AsyncPackage.TypeName)))
                .WithAdditionalAnnotations(Simplifier.Annotation)
                .WithLeadingTrivia(baseTypeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(baseTypeSyntax.GetTrailingTrivia());
            var updatedRoot = root.ReplaceNode(baseTypeSyntax, asyncPackageBaseTypeSyntax);

            return document.WithSyntaxRoot(updatedRoot);
        }
    }
}
