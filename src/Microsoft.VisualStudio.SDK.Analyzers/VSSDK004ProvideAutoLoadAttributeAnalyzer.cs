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
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Discovers VS packages that derive directly from <see cref="Package"/> instead of <see cref="AsyncPackage"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK004ProvideAutoLoadAttributeAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK004";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Auto loads without background load option will be deprecated in future updates.",
            messageFormat: "Synchronous auto loads will be deprecated in future versions, consider using BackgroundLoad flag and AsyncPackage base class.",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Register for compilation first so that we only activate the analyzer for applicable compilations
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var asyncPackage = compilationContext.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName);
                if (asyncPackage != null)
                {
                    var autoLoadAttribute = compilationContext.Compilation.GetTypeByMetadataName(Types.ProvideAutoLoadAttribute.FullName);
                    var packageAutoLoadFlags = compilationContext.Compilation.GetTypeByMetadataName(Types.PackageAutoLoadFlags.FullName);

                    // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                    compilationContext.RegisterSyntaxNodeAction(Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt, autoLoadAttribute, packageAutoLoadFlags)), SyntaxKind.ClassDeclaration);
                }
            });
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol autoLoadAttributeType, INamedTypeSymbol packageAutoLoadFlagsType)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            var userClassSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);

            // Enumerate all attribute lists and attribute to find ProvideAutoLoad attributes as there can be multiple ones
            foreach (var autoLoadInstance in userClassSymbol?.GetAttributes().Where(a => a.AttributeClass == autoLoadAttributeType))
            {
                var flagsArgument = autoLoadInstance.ConstructorArguments.FirstOrDefault(p => p.Type == packageAutoLoadFlagsType);
                Shell.PackageAutoLoadFlags flagsValue = flagsArgument.IsNull ? PackageAutoLoadFlags.None : (Shell.PackageAutoLoadFlags)flagsArgument.Value;

                // We are looking for any auto load attribute without the BackgroundLoad flag and SkipWhenUIContextRulesActive flag.
                if (!flagsValue.HasFlag(Shell.PackageAutoLoadFlags.BackgroundLoad) &&
                    !flagsValue.HasFlag(Shell.PackageAutoLoadFlags.SkipWhenUIContextRulesActive))
                {
                    var attributeSyntax = autoLoadInstance.ApplicationSyntaxReference.GetSyntax(context.CancellationToken) as AttributeSyntax;
                    Location location = attributeSyntax.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, attributeSyntax.GetLocation()));
                }
            }
        }
    }
}
