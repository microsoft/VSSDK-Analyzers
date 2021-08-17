// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Tasks created from <see cref="ThreadHelper.JoinableTaskFactory"/> must be awaited or joined.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK007ThreadHelperJTFRunAsync : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK007";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new(
            id: Id,
            title: "ThreadHelper.JoinableTaskFactory.RunAsync",
            messageFormat: "Await/join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync.",
            description: "ThreadHelper.JoinableTaskFactory.RunAsync is not safe to use for fire-and-forget tasks inside Visual Studio. Either await/join the JoinableTask, or use the JoinableTaskFactory instance from AsyncPackage or ToolkitThreadHelper.",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// A cached array for the <see cref="SupportedDiagnostics"/> property.
        /// </summary>
        private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(Descriptor);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Register for compilation first so that we only activate the analyzer for applicable compilations
            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(Utils.DebuggableWrapper(ctxt => AnalyzeInvocationExpression(ctxt)), SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            // We don't care about awaited calls.
            if (IsAwaited(invocationExpr))
            {
                return;
            }

            // Is a member named `RunAsync` being invoked? (Confirm it's the right one later.)
            if (invocationExpr.Expression is not MemberAccessExpressionSyntax runAsyncExpr ||
                runAsyncExpr.Name.Identifier.ToString() != Types.JoinableTaskFactory.RunAsync)
            {
                return;
            }

            // Do we have a `JoinableTaskFactory.RunAsync` expression?
            if (runAsyncExpr.Expression is not MemberAccessExpressionSyntax jtfExpr ||
                jtfExpr.Name.Identifier.ToString() != Types.JoinableTaskFactory.TypeName)
            {
                return;
            }

            // Do we have a `ThreadHelper.JoinableTaskFactory.RunAsync` expression?
            if (jtfExpr.Expression is not IdentifierNameSyntax threadHelperExpr ||
                threadHelperExpr.ToString() != Types.ThreadHelper.TypeName)
            {
                return;
            }

            // Verify `ThreadHelper` is the correct type in the expected namespace.
            ISymbol? threadHelperSymbol = context
                .SemanticModel
                .GetSymbolInfo(threadHelperExpr, context.CancellationToken)
                .Symbol;

            if (threadHelperSymbol == null || !threadHelperSymbol.BelongsToNamespace(Types.ThreadHelper.Namespace))
            {
                return;
            }

            // Get the symbol for the `RunAsync` method and verify it belongs to the correct JoinableTaskFactory
            // class in the expected namespace.
            var isRunAsyncMethodOnJTF =
                context
                .SemanticModel
                .GetSymbolInfo(invocationExpr, context.CancellationToken)
                .Symbol is IMethodSymbol runAsyncSymbol &&
                runAsyncSymbol.Name == Types.JoinableTaskFactory.RunAsync &&
                runAsyncSymbol.ContainingType.Name == Types.JoinableTaskFactory.TypeName &&
                runAsyncSymbol.ContainingType.BelongsToNamespace(Types.JoinableTaskFactory.Namespace);

            if (!isRunAsyncMethodOnJTF)
            {
                return;
            }

            // No diagnostic if the RunAsync invocation is immediately synchronously joined with Join(),
            // e.g.  ThreadHelper.JoinableTaskFactory.RunAsync(...).Join();
            if (IsSynchronouslyJoined(context, invocationExpr))
            {
                return;
            }

            // Is the JoinableTask returned from RunAsync assigned to a variable? If so, search the enclosing block
            // and check if that variable is awaited/joined.
            if (IsJoinableTaskAssigned(invocationExpr, out SyntaxNode? assignedTo, out SyntaxToken? assignedToToken) &&
                assignedTo != null &&
                assignedToToken != null)
            {
                SyntaxNode? enclosingBlock = GetEnclosingBlock(assignedTo);
                if (enclosingBlock != null)
                {
                    if (IsVariableAwaitedOrJoined(context, assignedToToken.Value.ValueText, enclosingBlock))
                    {
                        return;
                    }
                }
            }

            // Report the diagnostic
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, runAsyncExpr.Name.GetLocation()));
        }

        /// <summary>
        /// Determines whether the given invocation expression is part of an await expression.
        /// </summary>
        private static bool IsAwaited(InvocationExpressionSyntax invocationExpr)
        {
            AwaitExpressionSyntax? foundAwait = Utils.FindAncestor<AwaitExpressionSyntax>(
                invocationExpr,
                n => n is MemberAccessExpressionSyntax || n is InvocationExpressionSyntax,
                (aes, child) => aes.Expression == child);

            return foundAwait != null;
        }

        /// <summary>
        /// Determines whether the given invocation expression includes a <see cref="Threading.JoinableTask.Join"/> method.
        /// </summary>
        private static bool IsSynchronouslyJoined(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpr)
        {
            if (invocationExpr.Parent == null ||
                invocationExpr.Parent is not MemberAccessExpressionSyntax ||
                context.SemanticModel.GetSymbolInfo(invocationExpr.Parent, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol ||
                methodSymbol.Name != Types.JoinableTask.Join)
            {
                return false;
            }

            return
                methodSymbol.ContainingType.Name == Types.JoinableTask.TypeName &&
                methodSymbol.ContainingType.BelongsToNamespace(Types.JoinableTaskFactory.Namespace);
        }

        /// <summary>
        /// Determines whether the <see cref="Threading.JoinableTask"/> returned from the RunAsync invocation is assigned to a variable, and if so
        /// returns the variable's syntax node and identifier token.
        /// </summary>
        private static bool IsJoinableTaskAssigned(InvocationExpressionSyntax invocationExpr, out SyntaxNode? assignedToNode, out SyntaxToken? assignedToToken)
        {
            if (invocationExpr.Parent == null)
            {
                assignedToNode = null;
                assignedToToken = null;
                return false;
            }

            // Look for assignment to existing variable/field:  myVarOrField = ThreadHelper.JoinableTaskFactory.RunAsync(...)
            if (invocationExpr.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var assignmentExpr = (AssignmentExpressionSyntax)invocationExpr.Parent;
                assignedToNode = assignmentExpr.Left;
                assignedToToken = ((IdentifierNameSyntax)assignedToNode).Identifier;
                return true;
            }

            // Look for assignment to new variable:  var myTask = ThreadHelper.JoinableTaskFactory.RunAsync(...)
            SyntaxNode node = invocationExpr.Parent;
            while (node != null)
            {
                if (node.IsKind(SyntaxKind.VariableDeclarator))
                {
                    assignedToNode = node;
                    assignedToToken = ((VariableDeclaratorSyntax)assignedToNode).Identifier;
                    return true;
                }

                node = node.Parent;
            }

            assignedToNode = null;
            assignedToToken = null;
            return false;
        }

        /// <summary>
        /// Return the enclosing scope block that contains the given node.
        /// </summary>
        private static SyntaxNode? GetEnclosingBlock(SyntaxNode node)
        {
            while (node != null)
            {
                if (node.IsKind(SyntaxKind.Block))
                {
                    return node;
                }

                node = node.Parent;
            }

            return null;
        }

        /// <summary>
        /// Determine whether the given variable is awaited or joined within the specified scope block.
        /// </summary>
        private static bool IsVariableAwaitedOrJoined(SyntaxNodeAnalysisContext context, string variableName, SyntaxNode enclosingBlock)
        {
            // No diagnostic for:  await task;
            IEnumerable<AwaitExpressionSyntax>? awaitedList = from awaitExpr in enclosingBlock.DescendantNodes().OfType<AwaitExpressionSyntax>()
                              where awaitExpr.ChildNodes().OfType<IdentifierNameSyntax>().Count() == 1 &&
                                    awaitExpr.ChildNodes().OfType<IdentifierNameSyntax>().Single().Identifier.ValueText == variableName
                              select awaitExpr;

            if (awaitedList.Any())
            {
                return true;
            }

            // No diagnostic for:  task.Join();
            IEnumerable<IdentifierNameSyntax>? methodCallList = from varMethodCall in enclosingBlock.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                                 where varMethodCall.ChildNodes().OfType<IdentifierNameSyntax>().Count() == 2 &&
                                       varMethodCall.ChildNodes().OfType<IdentifierNameSyntax>().First().Identifier.ValueText == variableName
                                 select varMethodCall.ChildNodes().OfType<IdentifierNameSyntax>().ElementAt(1);

            foreach (IdentifierNameSyntax? method in methodCallList)
            {
                if (method.Identifier.ValueText == Types.JoinableTask.Join)
                {
                    return true;
                }
            }

            // No diagnostic for:  await task.JoinAsync();
            IEnumerable<AwaitExpressionSyntax>? awaitedJoinAsyncList = from awaitExpr in enclosingBlock.DescendantNodes().OfType<AwaitExpressionSyntax>()
                                       where VariableAwaitsJoinAsyncMethod(variableName, awaitExpr)
                                       select awaitExpr;

            if (awaitedJoinAsyncList.Any())
            {
                return true;
            }

            // Find any methods that the JoinableTask variable is passed to in case any of these methods performs the join,
            // and recursively search them to determine if they perform the await/join.
            IEnumerable<InvocationExpressionSyntax>? passedToMethodList = enclosingBlock.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (InvocationExpressionSyntax? passedToMethodInvocation in passedToMethodList)
            {
                if (VariablePassedAsArgumentToInvokedMethod(variableName, passedToMethodInvocation, out IdentifierNameSyntax? methodName, out var argIndex) &&
                    methodName != null &&
                    argIndex != null)
                {
                    if (context.SemanticModel.GetSymbolInfo(methodName, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
                    {
                        return false;
                    }

                    if (argIndex < methodSymbol.Parameters.Length)
                    {
                        IParameterSymbol? param = methodSymbol.Parameters[argIndex.Value];
                        SyntaxNode? methodEnclosingBlock = GetMethodDeclarationBlockNode(methodSymbol);
                        if (methodEnclosingBlock == null)
                        {
                            return false;
                        }

                        // Is the parameter awaited/joined inside the method?
                        if (IsVariableAwaitedOrJoined(context, param.Name, methodEnclosingBlock))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determine whether the given variable calls the <see cref="Threading.JoinableTask.JoinAsync"/> method in an await expression.
        /// </summary>
        private static bool VariableAwaitsJoinAsyncMethod(string variableName, AwaitExpressionSyntax awaitExpr)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return false;
            }

            IEnumerable<MemberAccessExpressionSyntax>? memberAccessList = awaitExpr.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            if (memberAccessList.Count() != 1)
            {
                return false;
            }

            MemberAccessExpressionSyntax? memberAccess = memberAccessList.First();

            IEnumerable<IdentifierNameSyntax>? childIdentifiers = memberAccess.ChildNodes().OfType<IdentifierNameSyntax>();
            if (!childIdentifiers.Any())
            {
                return false;
            }

            if (childIdentifiers.First().Identifier.ValueText != variableName)
            {
                return false;
            }

            return childIdentifiers.ElementAt(1).Identifier.ValueText == Types.JoinableTask.JoinAsync;
        }

        /// <summary>
        /// Determine whether the given variable is passed as an argument to an invoked method, and if so return the
        /// method name and the zero-based index of the argument.
        /// </summary>
        private static bool VariablePassedAsArgumentToInvokedMethod(
            string variableName,
            InvocationExpressionSyntax invocationExpr,
            out IdentifierNameSyntax? methodName,
            out int? argumentIndex)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                methodName = null;
                argumentIndex = 0;
                return false;
            }

            ArgumentListSyntax? argList = invocationExpr.ChildNodes().OfType<ArgumentListSyntax>().FirstOrDefault();
            if (argList == null)
            {
                methodName = null;
                argumentIndex = 0;
                return false;
            }

            var index = 0;
            foreach (ArgumentSyntax? arg in argList.ChildNodes().OfType<ArgumentSyntax>())
            {
                foreach (IdentifierNameSyntax? identiferName in arg.ChildNodes().OfType<IdentifierNameSyntax>())
                {
                    if (identiferName.Identifier.ValueText == variableName)
                    {
                        methodName = invocationExpr.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                        argumentIndex = index;
                        return true;
                    }
                }

                ++index;
            }

            methodName = null;
            argumentIndex = 0;
            return false;
        }

        /// <summary>
        /// Get the method declaration node for the given method symbol.
        /// </summary>
        private static SyntaxNode? GetMethodDeclarationNode(IMethodSymbol methodSymbol)
        {
            SyntaxReference? syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            return syntaxReference?.GetSyntax();
        }

        /// <summary>
        /// Get a method declaration's block node.
        /// </summary>
        private static SyntaxNode? GetMethodDeclarationBlockNode(IMethodSymbol methodSymbol)
        {
            SyntaxNode? methodDeclNode = GetMethodDeclarationNode(methodSymbol);
            return methodDeclNode?.ChildNodes().OfType<BlockSyntax>().FirstOrDefault();
        }
    }
}
