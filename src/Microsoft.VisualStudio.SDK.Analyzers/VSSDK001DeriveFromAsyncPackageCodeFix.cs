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
                CodeAction.Create("Convert to async package", ct => this.ConvertToAsyncPackageAsync(context.Document, diagnostic, ct), classDeclarationSyntax.Identifier.ToString()),
                diagnostic);
        }

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> ConvertToAsyncPackageAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var baseTypeSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            var classDeclarationSyntax = baseTypeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var initializeMethodSyntax = classDeclarationSyntax.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)) && method.Identifier.Text == Types.Package.Initialize);
            var baseInitializeInvocationSyntax = initializeMethodSyntax?.Body?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(ies => ies.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name?.Identifier.Text == Types.Package.Initialize && memberAccess.Expression is BaseExpressionSyntax);

            // Make it easier to track nodes across changes.
            var nodesToTrack = new List<SyntaxNode>
            {
                baseTypeSyntax,
                initializeMethodSyntax,
                baseInitializeInvocationSyntax,
            };
            nodesToTrack.RemoveAll(n => n == null);
            var updatedRoot = root.TrackNodes(nodesToTrack);

            // Replace the Package base type with AsyncPackage
            baseTypeSyntax = updatedRoot.GetCurrentNode(baseTypeSyntax);
            var asyncPackageBaseTypeSyntax = SyntaxFactory.SimpleBaseType(Types.AsyncPackage.TypeSyntax)
                .WithLeadingTrivia(baseTypeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(baseTypeSyntax.GetTrailingTrivia());
            updatedRoot = updatedRoot.ReplaceNode(baseTypeSyntax, asyncPackageBaseTypeSyntax);

            // Find the Initialize override, if present, and update it to InitializeAsync
            if (initializeMethodSyntax != null)
            {
                var cancellationTokenLocalVarName = SyntaxFactory.IdentifierName("cancellationToken");
                var progressLocalVarName = SyntaxFactory.IdentifierName("progress");
                initializeMethodSyntax = updatedRoot.GetCurrentNode(initializeMethodSyntax);
                var newBody = initializeMethodSyntax.Body;
                if (baseInitializeInvocationSyntax != null)
                {
                    var baseInitializeAsyncInvocationBookmark = new SyntaxAnnotation();
                    var baseInitializeAsyncInvocationSyntax = SyntaxFactory.AwaitExpression(
                        baseInitializeInvocationSyntax
                            .WithLeadingTrivia()
                            .WithExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.BaseExpression(),
                                    SyntaxFactory.IdentifierName(Types.AsyncPackage.InitializeAsync)))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(cancellationTokenLocalVarName),
                                SyntaxFactory.Argument(progressLocalVarName)))
                        .WithLeadingTrivia(baseInitializeInvocationSyntax.GetLeadingTrivia())
                        .WithAdditionalAnnotations(baseInitializeAsyncInvocationBookmark);
                    newBody = newBody.ReplaceNode(initializeMethodSyntax.GetCurrentNode(baseInitializeInvocationSyntax), baseInitializeAsyncInvocationSyntax);
                    var baseInvocationStatement = newBody.GetAnnotatedNodes(baseInitializeAsyncInvocationBookmark).First().FirstAncestorOrSelf<StatementSyntax>();

                    var leadingTrivia = SyntaxFactory.TriviaList(
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.Comment(@"// When initialized asynchronously, we *may* be on a background thread at this point."),
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.Comment(@"// Do any initialization that requires the UI thread after switching to the UI thread."),
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.Comment(@"// Otherwise, remove the switch to the UI thread if you don't need it."),
                        SyntaxFactory.LineFeed);

                    var switchToMainThreadStatement = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AwaitExpression(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ThisExpression(),
                                        SyntaxFactory.IdentifierName(Types.ThreadHelper.JoinableTaskFactory)),
                                    SyntaxFactory.IdentifierName(Types.JoinableTaskFactory.SwitchToMainThreadAsync)))
                                .AddArgumentListArguments(SyntaxFactory.Argument(cancellationTokenLocalVarName))))
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(SyntaxFactory.LineFeed);

                    newBody = newBody.InsertNodesAfter(baseInvocationStatement, new[] { switchToMainThreadStatement });
                }

                var initializeAsyncMethodSyntax = initializeMethodSyntax
                    .WithIdentifier(SyntaxFactory.Identifier(Types.AsyncPackage.InitializeAsync))
                    .WithReturnType(Types.Task.TypeSyntax)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(cancellationTokenLocalVarName.Identifier).WithType(Types.CancellationToken.TypeSyntax),
                        SyntaxFactory.Parameter(progressLocalVarName.Identifier).WithType(Types.IProgress.TypeSyntaxOf(Types.ServiceProgressData.TypeSyntax)))
                    .WithBody(newBody);
                updatedRoot = updatedRoot.ReplaceNode(initializeMethodSyntax, initializeAsyncMethodSyntax);
            }

            return document.WithSyntaxRoot(updatedRoot);
        }
    }
}
