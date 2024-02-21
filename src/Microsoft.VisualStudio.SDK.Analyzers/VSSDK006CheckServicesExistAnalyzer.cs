// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
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
            messageFormat: "Check whether the result of GetService calls is null",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
                    start.RegisterOperationAction(Utils.DebuggableWrapper(state.AnalyzeInvocationExpression), OperationKind.Invocation);
                }
            });
        }

        private static ImmutableArray<ISymbol> Flatten(params ImmutableArray<ISymbol>?[] symbols) => symbols.Where(m => m.HasValue).SelectMany(m => m!.Value).ToImmutableArray();

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

            internal void AnalyzeInvocationExpression(OperationAnalysisContext context)
            {
                var invocationExpression = (IInvocationOperation)context.Operation;
                IMethodSymbol invokedMethod = invocationExpression.TargetMethod;
                if (invokedMethod.IsGenericMethod)
                {
                    invokedMethod = invokedMethod.OriginalDefinition;
                }

                if (this.getServiceMethods.Contains(invokedMethod.ReducedFrom ?? invokedMethod))
                {
                    bool isTask = Utils.IsTask(invokedMethod.ReturnType);
                    SyntaxNode? startWalkFrom = isTask
                        ? (SyntaxNode?)Utils.FindAncestor<AwaitExpressionSyntax>(invocationExpression.Syntax, n => n is MemberAccessExpressionSyntax || n is InvocationExpressionSyntax, (aes, child) => aes.Expression == child)
                        : invocationExpression.Syntax;
                    if (startWalkFrom == null)
                    {
                        return;
                    }

                    AssignmentExpressionSyntax? assignment;
                    VariableDeclaratorSyntax? variableDeclarator;
                    if (Utils.FindAncestor<MemberAccessExpressionSyntax>(
                        startWalkFrom,
                        n => n is CastExpressionSyntax || n is ParenthesizedExpressionSyntax || n is AwaitExpressionSyntax,
                        (mae, child) => mae.Expression == child) != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, ((InvocationExpressionSyntax)invocationExpression.Syntax).Expression.GetLocation()));
                    }
                    else if ((assignment = Utils.FindAncestor<AssignmentExpressionSyntax>(
                        startWalkFrom,
                        n => n is CastExpressionSyntax || n is EqualsValueClauseSyntax || n is AwaitExpressionSyntax || (n is BinaryExpressionSyntax be && be.OperatorToken.IsKind(SyntaxKind.AsKeyword)),
                        (aes, child) => aes.Right == child)) != null)
                    {
                        SemanticModel semanticModel = context.Compilation.GetSemanticModel(assignment.SyntaxTree);
                        ISymbol? leftSymbol = semanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
                        if (leftSymbol is object)
                        {
                            // If the assigned variable is actually a field, scan this block for Assumes.Present
                            SyntaxNode? parentBlock = Utils.FindFirstAncestorOfTypes(invocationExpression.Syntax, typeof(BlockSyntax), typeof(ArrowExpressionClauseSyntax));
                            if (!parentBlock?.DescendantNodes().Any(n => this.IsThrowingNullCheck(n, leftSymbol, semanticModel, context.CancellationToken)) ?? true)
                            {
                                // Since we didn't find an Assumes.Present call for this symbol,
                                //    if this is a field or property, scan all blocks and expression bodies within this type.
                                //    otherwise just scan the blocks under this one.
                                System.Collections.Generic.IEnumerable<Location> derefs;
                                if (leftSymbol is IFieldSymbol || leftSymbol is IPropertySymbol)
                                {
                                    derefs = from member in leftSymbol.ContainingType.GetMembers().OfType<IMethodSymbol>()
                                             from syntaxRef in member.DeclaringSyntaxReferences
                                             let methodSyntax = syntaxRef.GetSyntax(context.CancellationToken) as MethodDeclarationSyntax
                                             where methodSyntax != null
                                             let bodyOrExpression = (SyntaxNode?)methodSyntax.Body ?? methodSyntax.ExpressionBody
                                             where bodyOrExpression != null
                                             from dref in this.ScanBlockForDereferencesWithoutNullCheck(context.Compilation.GetSemanticModel(bodyOrExpression.SyntaxTree), leftSymbol, bodyOrExpression, context.CancellationToken)
                                             select dref;
                                }
                                else if (parentBlock is object)
                                {
                                    derefs = this.ScanBlockForDereferencesWithoutNullCheck(semanticModel, leftSymbol, parentBlock, context.CancellationToken);
                                }
                                else
                                {
                                    derefs = Enumerable.Empty<Location>();
                                }

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
                        SemanticModel semanticModel = context.Compilation.GetSemanticModel(context.Operation.Syntax.SyntaxTree);

                        // The GetService call was assigned via an initializer to a new local variable. Search the code block for uses and null checks.
                        var leftSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) as ILocalSymbol;
                        if (leftSymbol != null)
                        {
                            BlockSyntax? containingBlock = context.Operation.Syntax.FirstAncestorOrSelf<BlockSyntax>();
                            if (containingBlock != null)
                            {
                                ImmutableArray<Location> derefs = this.ScanBlockForDereferencesWithoutNullCheck(semanticModel, leftSymbol, containingBlock, context.CancellationToken);
                                if (derefs.Any())
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclarator.Identifier.GetLocation(), derefs));
                                }
                            }
                        }
                    }
                }
            }

            private ImmutableArray<Location> ScanBlockForDereferencesWithoutNullCheck(SemanticModel semanticModel, ISymbol symbol, SyntaxNode containingBlockOrExpression, CancellationToken cancellationToken)
            {
                if (containingBlockOrExpression == null)
                {
                    throw new ArgumentNullException(nameof(containingBlockOrExpression));
                }

                if (symbol != null)
                {
                    System.Collections.Generic.IEnumerable<MemberAccessExpressionSyntax> variableUses =
                        from access in containingBlockOrExpression.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                        let symbolAccessed = semanticModel.GetSymbolInfo(access.Expression, cancellationToken).Symbol
                        where SymbolEqualityComparer.Default.Equals(symbol, symbolAccessed)
                        select access;
                    if (!containingBlockOrExpression.DescendantNodes().Any(n => this.IsNonNullCheck(n, symbol, semanticModel, cancellationToken)))
                    {
                        return variableUses.Select(vu => vu.Expression.GetLocation()).ToImmutableArray();
                    }
                }

                return ImmutableArray<Location>.Empty;
            }

            /// <summary>
            /// Checks whether the given syntax node determines whether the given symbol is null.
            /// </summary>
            private bool IsNonNullCheck(SyntaxNode node, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                bool IsSymbol(SyntaxNode n) => SymbolEqualityComparer.Default.Equals(symbol, semanticModel.GetSymbolInfo(n, cancellationToken).Symbol);
                bool IsEqualsOrExclamationEqualsCheck(BinaryExpressionSyntax o) => (o.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) || o.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
                                                                                    && (o.Left.IsKind(SyntaxKind.NullLiteralExpression) || o.Right.IsKind(SyntaxKind.NullLiteralExpression))
                                                                                    && (IsSymbol(o.Left) || IsSymbol(o.Right));
                bool IsPatternMatchTypeCheck(BinaryExpressionSyntax o) => o.OperatorToken.IsKind(SyntaxKind.IsKeyword)
                                                                          && (o.Right.IsKind(SyntaxKind.IdentifierName) || o.Right.IsKind(SyntaxKind.PredefinedType))
                                                                          && IsSymbol(o.Left);
                bool IsPatternMatchNullCheck(IsPatternExpressionSyntax o) => o.Pattern is ConstantPatternSyntax pattern
                                                                             && pattern.Expression.IsKind(SyntaxKind.NullLiteralExpression)
                                                                             && IsSymbol(o.Expression);

                if (node is IfStatementSyntax ifStatement)
                {
                    if (ifStatement.Condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Any(
                          o => IsEqualsOrExclamationEqualsCheck(o) || IsPatternMatchTypeCheck(o))
                        || ifStatement.Condition.DescendantNodesAndSelf().OfType<IsPatternExpressionSyntax>().Any(
                          o => IsPatternMatchNullCheck(o)))
                    {
                        return true;
                    }
                }

                if (this.IsThrowingNullCheck(node, symbol, semanticModel, cancellationToken))
                {
                    return true;
                }

                return false;
            }

            private bool IsThrowingNullCheck(SyntaxNode node, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (node is InvocationExpressionSyntax invocationExpression &&
                    semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol?.OriginalDefinition is { } item &&
                    this.nullThrowingMethods.Contains(item))
                {
                    ArgumentSyntax? firstArg = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                    if (firstArg != null && SymbolEqualityComparer.Default.Equals(symbol, semanticModel.GetSymbolInfo(firstArg.Expression, cancellationToken).Symbol))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
