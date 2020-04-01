// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Discovers VS packages that derive directly from <see cref="Package"/> instead of <see cref="AsyncPackage"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK001DeriveFromAsyncPackageAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK001";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Derive from AsyncPackage",
            messageFormat: "Your Package-derived class should derive from AsyncPackage instead.",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
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
                INamedTypeSymbol packageType = compilationContext.Compilation.GetTypeByMetadataName(Types.Package.FullName);
                INamedTypeSymbol asyncPackage = compilationContext.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName);
                if (asyncPackage != null)
                {
                    // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                    compilationContext.RegisterSyntaxNodeAction(Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt, packageType)), SyntaxKind.ClassDeclaration);
                }
            });
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol packageType)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            BaseTypeSyntax? baseType = declaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null)
            {
                return;
            }

            SymbolInfo baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type, context.CancellationToken);
            if (baseTypeSymbol.Symbol?.OriginalDefinition == packageType.OriginalDefinition)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptor,
                    baseType.GetLocation()));
            }
        }
    }
}
