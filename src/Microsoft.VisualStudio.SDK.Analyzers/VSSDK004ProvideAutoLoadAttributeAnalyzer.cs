// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Discovers <see cref="Microsoft.VisualStudio.Shell.ProvideAutoLoadAttribute"/> usages without BackgroundLoad flag.
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
            title: "Use BackgroundLoad flag in ProvideAutoLoad attribute for asynchronous auto load",
            messageFormat: "Synchronous auto loads will be deprecated in future versions, consider using BackgroundLoad flag and AsyncPackage base class",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Performance",
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
                INamedTypeSymbol? package = compilationContext.Compilation.GetTypeByMetadataName(Types.Package.FullName);
                INamedTypeSymbol? asyncPackage = compilationContext.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName);
                if (asyncPackage != null && package != null)
                {
                    INamedTypeSymbol? autoLoadAttribute = compilationContext.Compilation.GetTypeByMetadataName(Types.ProvideAutoLoadAttribute.FullName);
                    INamedTypeSymbol? packageAutoLoadFlags = compilationContext.Compilation.GetTypeByMetadataName(Types.PackageAutoLoadFlags.FullName);
                    if (autoLoadAttribute != null && packageAutoLoadFlags != null)
                    {
                        // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                        compilationContext.RegisterSyntaxNodeAction(Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt, autoLoadAttribute, packageAutoLoadFlags, package, asyncPackage)), SyntaxKind.ClassDeclaration);
                    }
                }
            });
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol autoLoadAttributeType, INamedTypeSymbol packageAutoLoadFlagsType, INamedTypeSymbol packageType, INamedTypeSymbol asyncPackageType)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            INamedTypeSymbol? userClassSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);

            BaseTypeSyntax? baseType = declaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null)
            {
                return;
            }

            // Don't evaluate if base type is not Package
            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type, context.CancellationToken).Symbol?.OriginalDefinition as ITypeSymbol;
            if (!Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, packageType))
            {
                return;
            }

            bool isBaseTypeAsyncPackage = Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, asyncPackageType);

            // Enumerate all attribute lists and attribute to find ProvideAutoLoad attributes as there can be multiple ones
            if (userClassSymbol is object)
            {
                foreach (AttributeData autoLoadInstance in userClassSymbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, autoLoadAttributeType)))
                {
                    TypedConstant flagsArgument = autoLoadInstance.ConstructorArguments.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Type, packageAutoLoadFlagsType));
                    Types.PackageAutoLoadFlags.Values flagsValue = flagsArgument.IsNull ? Types.PackageAutoLoadFlags.Values.None : (Types.PackageAutoLoadFlags.Values)flagsArgument.Value!;

                    // Check if AutoLoad attribute applies to VS versions with AsyncPackage support
                    if (flagsValue.HasFlag(Types.PackageAutoLoadFlags.Values.SkipWhenUIContextRulesActive))
                    {
                        continue;
                    }

                    // Check if BackgroundLoad flag is present and base class is AsyncPackage
                    if (!(flagsValue.HasFlag(Types.PackageAutoLoadFlags.Values.BackgroundLoad) && isBaseTypeAsyncPackage))
                    {
                        var attributeSyntax = (AttributeSyntax?)autoLoadInstance.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
                        Location? location = attributeSyntax?.GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, attributeSyntax?.GetLocation()));
                    }
                }
            }
        }
    }
}
