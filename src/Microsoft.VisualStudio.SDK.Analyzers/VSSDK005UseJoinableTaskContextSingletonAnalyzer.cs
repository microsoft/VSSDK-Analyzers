// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Discourages anyone instantiating their own JoinableTaskContext within VS.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK005UseJoinableTaskContextSingletonAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK005";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Avoid instantiating JoinableTaskContext",
            messageFormat: "Use the ThreadHelper.JoinableTaskContext singleton rather than instantiating your own to avoid deadlocks",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Error,
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

            // Register for compilation first so that we only activate the analyzer for applicable compilations
            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? joinableTaskContext = compilationContext.Compilation.GetTypeByMetadataName(Types.JoinableTaskContext.FullName)?.OriginalDefinition;
                if (joinableTaskContext != null)
                {
                    // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                    compilationContext.RegisterSyntaxNodeAction(
                        Utils.DebuggableWrapper(ctxt => this.AnalyzeObjectCreation(ctxt, joinableTaskContext)),
                        SyntaxKind.ObjectCreationExpression);
                }
            });
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext ctxt, INamedTypeSymbol joinableTaskContext)
        {
            var creationSyntax = (ObjectCreationExpressionSyntax)ctxt.Node;
            ISymbol? symbolBeingInstantiated = ctxt.SemanticModel.GetSymbolInfo(creationSyntax.Type).Symbol;
            if (SymbolEqualityComparer.Default.Equals(joinableTaskContext, symbolBeingInstantiated))
            {
                ctxt.ReportDiagnostic(Diagnostic.Create(Descriptor, creationSyntax.GetLocation()));
            }
        }
    }
}
