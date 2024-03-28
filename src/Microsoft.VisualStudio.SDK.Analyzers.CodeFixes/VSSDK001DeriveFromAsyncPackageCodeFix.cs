// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
            Diagnostic diagnostic = context.Diagnostics.First();

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            BaseTypeSyntax? baseTypeSyntax = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            ClassDeclarationSyntax? classDeclarationSyntax = baseTypeSyntax?.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if (classDeclarationSyntax != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Convert to async package", ct => this.ConvertToAsyncPackageAsync(context, diagnostic, ct), "only one"),
                    diagnostic);
            }
        }

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> ConvertToAsyncPackageAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = (await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            Compilation? compilation = await context.Document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            Assumes.NotNull(compilation);
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            BaseTypeSyntax? baseTypeSyntax = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeSyntax>();
            ClassDeclarationSyntax? classDeclarationSyntax = baseTypeSyntax?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            Assumes.NotNull(classDeclarationSyntax);

            MethodDeclarationSyntax? initializeMethodSyntax = classDeclarationSyntax.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)) && method.Identifier.Text == Types.Package.Initialize);
            InvocationExpressionSyntax? baseInitializeInvocationSyntax = initializeMethodSyntax?.Body?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(ies => ies.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name?.Identifier.Text == Types.Package.Initialize && memberAccess.Expression is BaseExpressionSyntax);
            var getServiceInvocationsSyntax = new List<InvocationExpressionSyntax>();
            AttributeSyntax? packageRegistrationSyntax = null;
            {
                INamedTypeSymbol? userClassSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax, context.CancellationToken);
                INamedTypeSymbol? packageRegistrationType = compilation.GetTypeByMetadataName(Types.PackageRegistrationAttribute.FullName);
                AttributeData? packageRegistrationInstance = userClassSymbol?.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, packageRegistrationType));
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
            var nodesToTrack = new List<SyntaxNode?>
            {
                baseTypeSyntax,
                initializeMethodSyntax,
                baseInitializeInvocationSyntax,
                packageRegistrationSyntax,
            };
            nodesToTrack.AddRange(getServiceInvocationsSyntax);
            nodesToTrack.RemoveAll(n => n == null);
            SyntaxNode updatedRoot = root!.TrackNodes(nodesToTrack!);

            // Replace the Package base type with AsyncPackage
            baseTypeSyntax = updatedRoot.GetCurrentNode(baseTypeSyntax!);
            Assumes.NotNull(baseTypeSyntax);
            SimpleBaseTypeSyntax asyncPackageBaseTypeSyntax = SyntaxFactory.SimpleBaseType(Types.AsyncPackage.TypeSyntax.WithAdditionalAnnotations(Simplifier.Annotation))
                .WithLeadingTrivia(baseTypeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(baseTypeSyntax.GetTrailingTrivia());
            updatedRoot = updatedRoot.ReplaceNode(baseTypeSyntax, asyncPackageBaseTypeSyntax);

            // Update the PackageRegistration attribute
            if (packageRegistrationSyntax != null)
            {
                LiteralExpressionSyntax trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                packageRegistrationSyntax = updatedRoot.GetCurrentNode(packageRegistrationSyntax);
                Assumes.NotNull(packageRegistrationSyntax);
                AttributeArgumentSyntax? allowsBackgroundLoadingSyntax = packageRegistrationSyntax.ArgumentList?.Arguments.FirstOrDefault(a => a.NameEquals?.Name?.Identifier.Text == Types.PackageRegistrationAttribute.AllowsBackgroundLoading);
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
                IdentifierNameSyntax cancellationTokenLocalVarName = SyntaxFactory.IdentifierName("cancellationToken");
                IdentifierNameSyntax progressLocalVarName = SyntaxFactory.IdentifierName("progress");
                initializeMethodSyntax = updatedRoot.GetCurrentNode(initializeMethodSyntax);
                Assumes.NotNull(initializeMethodSyntax);
                BlockSyntax? newBody = initializeMethodSyntax.Body;
                Assumes.NotNull(newBody);

                SyntaxTriviaList leadingTrivia = SyntaxFactory.TriviaList(
                    SyntaxFactory.Comment(@"// When initialized asynchronously, we *may* be on a background thread at this point."),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment(@"// Do any initialization that requires the UI thread after switching to the UI thread."),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment(@"// Otherwise, remove the switch to the UI thread if you don't need it."),
                    SyntaxFactory.CarriageReturnLineFeed);

                ExpressionStatementSyntax switchToMainThreadStatement = SyntaxFactory.ExpressionStatement(
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
                    AwaitExpressionSyntax baseInitializeAsyncInvocationSyntax = SyntaxFactory.AwaitExpression(
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
                    baseInitializeInvocationSyntax = initializeMethodSyntax.GetCurrentNode(baseInitializeInvocationSyntax);
                    Assumes.NotNull(baseInitializeInvocationSyntax);
                    newBody = newBody.ReplaceNode(baseInitializeInvocationSyntax, baseInitializeAsyncInvocationSyntax);
                    StatementSyntax baseInvocationStatement = newBody.GetAnnotatedNodes(baseInitializeAsyncInvocationBookmark).First().FirstAncestorOrSelf<StatementSyntax>()!;

                    newBody = newBody.InsertNodesAfter(
                        baseInvocationStatement,
                        new[] { switchToMainThreadStatement.WithLeadingTrivia(switchToMainThreadStatement.GetLeadingTrivia().Insert(0, SyntaxFactory.LineFeed)) });
                }
                else
                {
                    newBody = newBody.WithStatements(
                        newBody.Statements.Insert(0, switchToMainThreadStatement));
                }

                MethodDeclarationSyntax initializeAsyncMethodSyntax = initializeMethodSyntax
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
                        InvocationExpressionSyntax invocation = node;
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

            Document newDocument = context.Document.WithSyntaxRoot(updatedRoot);
            newDocument = await ImportAdder.AddImportsAsync(newDocument, Simplifier.Annotation, cancellationToken: cancellationToken);

            return newDocument;
        }
    }
}
