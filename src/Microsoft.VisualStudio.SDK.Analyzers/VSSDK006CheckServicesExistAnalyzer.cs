// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Discourages anyone instantiating their own JoinableTaskContext within VS.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK006CheckServicesExistAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK006";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Check services exist",
            messageFormat: "Check whether the result of GetService calls is null.",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(start =>
            {
                var state = new CompilationState(
                    Flatten(
                        start.Compilation.GetTypeByMetadataName(Types.Package.FullName)?.GetMembers(Types.Package.GetService),
                        start.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName)?.GetMembers(Types.AsyncPackage.GetServiceAsync),
                        start.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName)?.GetMembers(Types.Package.GetService),
                        start.Compilation.GetTypeByMetadataName(Types.ServiceProvider.FullName)?.GetMembers(Types.ServiceProvider.GetService),
                        start.Compilation.GetTypeByMetadataName(Types.IServiceProvider.FullName)?.GetMembers(Types.IServiceProvider.GetService),
                        start.Compilation.GetTypeByMetadataName(Types.PackageUtilities.FullName)?.GetMembers(Types.PackageUtilities.QueryService),
                        start.Compilation.GetTypeByMetadataName(Types.IAsyncServiceProvider.FullName)?.GetMembers(Types.IAsyncServiceProvider.GetServiceAsync)),
                    Flatten(
                        start.Compilation.GetTypeByMetadataName(Types.Assumes.FullName)?.GetMembers(Types.Assumes.Present)));
                if (state.ShouldAnalyze)
                {
                    start.RegisterSyntaxNodeAction(state.AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
                }
            });
        }

        private static ImmutableArray<ISymbol> Flatten(params ImmutableArray<ISymbol>?[] symbols) => symbols.Where(m => m.HasValue).SelectMany(m => m.Value).ToImmutableArray();

        private class CompilationState
        {
            /// <summary>
            /// GetService or GetServiceAsync methods.
            /// </summary>
            private readonly ImmutableArray<ISymbol> getServiceMethods;

            /// <summary>
            /// Methods that throw when their first argument is null.
            /// </summary>
            private readonly ImmutableArray<ISymbol> nullThrowingMethods;

            internal CompilationState(ImmutableArray<ISymbol> getServiceMethods, ImmutableArray<ISymbol> nullThrowingMethods)
            {
                this.getServiceMethods = getServiceMethods;
                this.nullThrowingMethods = nullThrowingMethods;
            }

            internal bool ShouldAnalyze => !this.getServiceMethods.IsEmpty;

            internal void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
            {
                var invocationExpression = (InvocationExpressionSyntax)context.Node;
                var invokedMethod = context.SemanticModel.GetSymbolInfo(invocationExpression.Expression, context.CancellationToken).Symbol as IMethodSymbol;
                if (invokedMethod != null && this.getServiceMethods.Contains(invokedMethod.ReducedFrom ?? invokedMethod))
                {
                    bool isTask = Utils.IsTask(invokedMethod.ReturnType);
                    SyntaxNode startWalkFrom = isTask
                        ? (SyntaxNode)Utils.FindAncestor<AwaitExpressionSyntax>(invocationExpression, n => n is MemberAccessExpressionSyntax || n is InvocationExpressionSyntax, (aes, child) => aes.Expression == child)
                        : invocationExpression;
                    if (startWalkFrom == null)
                    {
                        return;
                    }

                    AssignmentExpressionSyntax assignment;
                    VariableDeclaratorSyntax variableDeclarator;
                    if (Utils.FindAncestor<MemberAccessExpressionSyntax>(
                        startWalkFrom,
                        n => n is CastExpressionSyntax || n is ParenthesizedExpressionSyntax || n is AwaitExpressionSyntax,
                        (mae, child) => mae.Expression == child) != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, invocationExpression.Expression.GetLocation()));
                    }
                    else if ((assignment = Utils.FindAncestor<AssignmentExpressionSyntax>(
                        startWalkFrom,
                        n => n is CastExpressionSyntax || n is EqualsValueClauseSyntax || n is AwaitExpressionSyntax || (n is BinaryExpressionSyntax be && be.OperatorToken.IsKind(SyntaxKind.AsKeyword)),
                        (aes, child) => aes.Right == child)) != null)
                    {
                        var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
                        if (leftSymbol != null)
                        {
                            // If the assigned variable is actually a field, scan this block for Assumes.Present
                            var parentBlock = Utils.FindFirstAncestorOfTypes(invocationExpression, typeof(BlockSyntax), typeof(ArrowExpressionClauseSyntax));
                            if (!parentBlock?.DescendantNodes().Any(n => this.IsThrowingNullCheck(n, leftSymbol, context)) ?? true)
                            {
                                // Since we didn't find an Assumes.Present call for this symbol, scan all blocks and expression bodies within this type.
                                var derefs = from member in leftSymbol.ContainingType.GetMembers().OfType<IMethodSymbol>()
                                             from syntaxRef in member.DeclaringSyntaxReferences
                                             let methodSyntax = syntaxRef.GetSyntax(context.CancellationToken) as MethodDeclarationSyntax
                                             where methodSyntax != null
                                             let bodyOrExpression = (SyntaxNode)methodSyntax.Body ?? methodSyntax.ExpressionBody
                                             where bodyOrExpression != null
                                             from dref in this.ScanBlockForDereferencesWithoutNullCheck(context, leftSymbol, bodyOrExpression)
                                             select dref;
                                if (derefs.Any())
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.Left.GetLocation(), derefs));
                                }
                            }
                        }
                    }
                    else if ((variableDeclarator = Utils.FindAncestor<VariableDeclaratorSyntax>(
                        startWalkFrom,
                        n => n is CastExpressionSyntax || n is EqualsValueClauseSyntax || n is AwaitExpressionSyntax || (n is BinaryExpressionSyntax be && be.OperatorToken.IsKind(SyntaxKind.AsKeyword)),
                        (vds, child) => vds.Initializer == child)) != null)
                    {
                        // The GetService call was assigned via an initializer to a new local variable. Search the code block for uses and null checks.
                        var leftSymbol = context.SemanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) as ILocalSymbol;
                        if (leftSymbol != null)
                        {
                            var containingBlock = context.Node.FirstAncestorOrSelf<BlockSyntax>();
                            var derefs = this.ScanBlockForDereferencesWithoutNullCheck(context, leftSymbol, containingBlock);
                            if (derefs.Any())
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclarator.Identifier.GetLocation(), derefs));
                            }
                        }
                    }
                }
            }

            private ImmutableArray<Location> ScanBlockForDereferencesWithoutNullCheck(SyntaxNodeAnalysisContext context, ISymbol symbol, SyntaxNode containingBlockOrExpression)
            {
                if (containingBlockOrExpression == null)
                {
                    throw new ArgumentNullException(nameof(containingBlockOrExpression));
                }

                if (symbol != null)
                {
                    var variableUses = from access in containingBlockOrExpression.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                                       let symbolAccessed = context.SemanticModel.GetSymbolInfo(access.Expression, context.CancellationToken).Symbol
                                       where symbol.Equals(symbolAccessed)
                                       select access;
                    if (!containingBlockOrExpression.DescendantNodes().Any(n => this.IsNonNullCheck(n, symbol, context)))
                    {
                        return variableUses.Select(vu => vu.Expression.GetLocation()).ToImmutableArray();
                    }
                }

                return ImmutableArray<Location>.Empty;
            }

            /// <summary>
            /// Checks whether the given syntax node determines whether the given symbol is null.
            /// </summary>
            private bool IsNonNullCheck(SyntaxNode node, ISymbol symbol, SyntaxNodeAnalysisContext context)
            {
                bool IsSymbol(SyntaxNode n) => symbol.Equals(context.SemanticModel.GetSymbolInfo(n, context.CancellationToken).Symbol);

                if (node is IfStatementSyntax ifStatement &&
                    ifStatement.Condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Any(
                        o => (o.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) || o.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
                            && (o.Left.IsKind(SyntaxKind.NullLiteralExpression) || o.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            && (IsSymbol(o.Left) || IsSymbol(o.Right))))
                {
                    return true;
                }

                if (this.IsThrowingNullCheck(node, symbol, context))
                {
                    return true;
                }

                return false;
            }

            private bool IsThrowingNullCheck(SyntaxNode node, ISymbol symbol, SyntaxNodeAnalysisContext context)
            {
                if (node is InvocationExpressionSyntax invocationExpression &&
                    this.nullThrowingMethods.Contains(context.SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol?.OriginalDefinition))
                {
                    var firstArg = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                    if (firstArg != null && symbol.Equals(context.SemanticModel.GetSymbolInfo(firstArg.Expression, context.CancellationToken).Symbol))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
