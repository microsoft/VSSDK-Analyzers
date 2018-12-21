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
                CodeAction.Create("Convert to async package", ct => this.ConvertToAsyncPackageAsync(context, diagnostic, ct), "only one"),
                diagnostic);
        }

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> ConvertToAsyncPackageAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = await context.Document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var baseTypeSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            var classDeclarationSyntax = baseTypeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var initializeMethodSyntax = classDeclarationSyntax.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)) && method.Identifier.Text == Types.Package.Initialize);
            var baseInitializeInvocationSyntax = initializeMethodSyntax?.Body?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(ies => ies.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name?.Identifier.Text == Types.Package.Initialize && memberAccess.Expression is BaseExpressionSyntax);
            var getServiceInvocationsSyntax = new List<InvocationExpressionSyntax>();
            AttributeSyntax packageRegistrationSyntax = null;
            {
                var userClassSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax, context.CancellationToken);
                var packageRegistrationType = compilation.GetTypeByMetadataName(Types.PackageRegistrationAttribute.FullName);
                var packageRegistrationInstance = userClassSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass == packageRegistrationType);
                if (packageRegistrationInstance?.ApplicationSyntaxReference != null)
                {
                    packageRegistrationSyntax = (AttributeSyntax)await packageRegistrationInstance.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            if (initializeMethodSyntax != null)
            {
                getServiceInvocationsSyntax.AddRange(
                    from invocation in initializeMethodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    let memberBinding = invocation.Expression as MemberAccessExpressionSyntax
                    let identifierName = invocation.Expression as IdentifierNameSyntax
                    where identifierName?.Identifier.Text == Types.Package.GetService
                       || (memberBinding.Name.Identifier.Text == Types.Package.GetService && memberBinding.Expression.IsKind(SyntaxKind.ThisExpression))
                    select invocation);
            }

            // Make it easier to track nodes across changes.
            var nodesToTrack = new List<SyntaxNode>
            {
                baseTypeSyntax,
                initializeMethodSyntax,
                baseInitializeInvocationSyntax,
                packageRegistrationSyntax,
            };
            nodesToTrack.AddRange(getServiceInvocationsSyntax);
            nodesToTrack.RemoveAll(n => n == null);
            var updatedRoot = root.TrackNodes(nodesToTrack);

            // Replace the Package base type with AsyncPackage
            baseTypeSyntax = updatedRoot.GetCurrentNode(baseTypeSyntax);
            var asyncPackageBaseTypeSyntax = SyntaxFactory.SimpleBaseType(Types.AsyncPackage.TypeSyntax.WithAdditionalAnnotations(Simplifier.Annotation))
                .WithLeadingTrivia(baseTypeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(baseTypeSyntax.GetTrailingTrivia());
            updatedRoot = updatedRoot.ReplaceNode(baseTypeSyntax, asyncPackageBaseTypeSyntax);

            // Update the PackageRegistration attribute
            if (packageRegistrationSyntax != null)
            {
                var trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                packageRegistrationSyntax = updatedRoot.GetCurrentNode(packageRegistrationSyntax);
                var allowsBackgroundLoadingSyntax = packageRegistrationSyntax.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name?.Identifier.Text == Types.PackageRegistrationAttribute.AllowsBackgroundLoading);
                if (allowsBackgroundLoadingSyntax != null)
                {
                    updatedRoot = updatedRoot.ReplaceNode(
                        allowsBackgroundLoadingSyntax,
                        allowsBackgroundLoadingSyntax.WithExpression(trueExpression));
                }
                else
                {
                    updatedRoot = updatedRoot.ReplaceNode(
                        packageRegistrationSyntax,
                        packageRegistrationSyntax.AddArgumentListArguments(
                            SyntaxFactory.AttributeArgument(trueExpression).WithNameEquals(SyntaxFactory.NameEquals(Types.PackageRegistrationAttribute.AllowsBackgroundLoading))));
                }
            }

            // Find the Initialize override, if present, and update it to InitializeAsync
            if (initializeMethodSyntax != null)
            {
                var cancellationTokenLocalVarName = SyntaxFactory.IdentifierName("cancellationToken");
                var progressLocalVarName = SyntaxFactory.IdentifierName("progress");
                initializeMethodSyntax = updatedRoot.GetCurrentNode(initializeMethodSyntax);
                var newBody = initializeMethodSyntax.Body;

                var leadingTrivia = SyntaxFactory.TriviaList(
                    SyntaxFactory.Comment(@"// When initialized asynchronously, we *may* be on a background thread at this point."),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment(@"// Do any initialization that requires the UI thread after switching to the UI thread."),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment(@"// Otherwise, remove the switch to the UI thread if you don't need it."),
                    SyntaxFactory.CarriageReturnLineFeed);

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
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

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

                    newBody = newBody.InsertNodesAfter(
                        baseInvocationStatement,
                        new[] { switchToMainThreadStatement.WithLeadingTrivia(switchToMainThreadStatement.GetLeadingTrivia().Insert(0, SyntaxFactory.LineFeed)) });
                }
                else
                {
                    newBody = newBody.WithStatements(
                        newBody.Statements.Insert(0, switchToMainThreadStatement));
                }

                var initializeAsyncMethodSyntax = initializeMethodSyntax
                    .WithIdentifier(SyntaxFactory.Identifier(Types.AsyncPackage.InitializeAsync))
                    .WithReturnType(Types.Task.TypeSyntax.WithAdditionalAnnotations(Simplifier.Annotation))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(cancellationTokenLocalVarName.Identifier).WithType(Types.CancellationToken.TypeSyntax.WithAdditionalAnnotations(Simplifier.Annotation)),
                        SyntaxFactory.Parameter(progressLocalVarName.Identifier).WithType(Types.IProgress.TypeSyntaxOf(Types.ServiceProgressData.TypeSyntax).WithAdditionalAnnotations(Simplifier.Annotation)))
                    .WithBody(newBody);
                updatedRoot = updatedRoot.ReplaceNode(initializeMethodSyntax, initializeAsyncMethodSyntax);

                // Replace GetService calls with GetServiceAsync
                getServiceInvocationsSyntax = updatedRoot.GetCurrentNodes<InvocationExpressionSyntax>(getServiceInvocationsSyntax).ToList();
                updatedRoot = updatedRoot.ReplaceNodes(
                    getServiceInvocationsSyntax,
                    (orig, node) =>
                    {
                        var invocation = node;
                        if (invocation.Expression is IdentifierNameSyntax methodName)
                        {
                            invocation = invocation.WithExpression(SyntaxFactory.IdentifierName(Types.AsyncPackage.GetServiceAsync));
                        }
                        else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            invocation = invocation.WithExpression(
                                memberAccess.WithName(SyntaxFactory.IdentifierName(Types.AsyncPackage.GetServiceAsync)));
                        }

                        return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.AwaitExpression(invocation))
                            .WithAdditionalAnnotations(Simplifier.Annotation);
                    });

                updatedRoot = await Utils.AddUsingTaskEqualsDirectiveAsync(updatedRoot, cancellationToken);
            }

            var newDocument = context.Document.WithSyntaxRoot(updatedRoot);
            newDocument = await ImportAdder.AddImportsAsync(newDocument, Simplifier.Annotation, cancellationToken: cancellationToken);

            return newDocument;
        }
    }
}
