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
    /// Identifies cases where an <see cref="AsyncPackage"/> isn't tagged as such in the <see cref="PackageRegistrationAttribute"/>,
    /// or a <see cref="Package"/> is tagged as async inappropriately.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK002";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "PackageRegistration matches Package",
            messageFormat: "The PackageRegistrationAttribute.AllowsBackgroundLoading should be set to true if and only if the package derives from AsyncPackage.",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Error,
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
                var packageType = compilationContext.Compilation.GetTypeByMetadataName(typeof(Package).FullName)?.OriginalDefinition;
                var asyncPackageType = compilationContext.Compilation.GetTypeByMetadataName(typeof(AsyncPackage).FullName)?.OriginalDefinition;
                if (packageType != null && asyncPackageType != null)
                {
                    // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                    compilationContext.RegisterSyntaxNodeAction(
                        Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt, packageType, asyncPackageType)),
                        SyntaxKind.ClassDeclaration);
                }
            });
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol packageType, INamedTypeSymbol asyncPackageType)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            var baseType = declaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null)
            {
                return;
            }

            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type, context.CancellationToken).Symbol?.OriginalDefinition as ITypeSymbol;

            if (Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, packageType))
            {
                var packageRegistrationType = context.Compilation.GetTypeByMetadataName(Types.PackageRegistrationAttribute.FullName);
                var userClassSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
                var packageRegistrationInstance = userClassSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass == packageRegistrationType);
                if (packageRegistrationInstance == null)
                {
                    return;
                }

                if (!(packageRegistrationInstance.NamedArguments.FirstOrDefault(kv => kv.Key == Types.PackageRegistrationAttribute.AllowsBackgroundLoading).Value.Value is bool allowsBackgroundLoading))
                {
                    allowsBackgroundLoading = false;
                }

                bool isAsyncPackageBaseType = Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, asyncPackageType);

                if (isAsyncPackageBaseType != allowsBackgroundLoading)
                {
                    var attributeSyntax = packageRegistrationInstance.ApplicationSyntaxReference.GetSyntax(context.CancellationToken) as AttributeSyntax;
                    var allowBackgroundLoadingSyntax = attributeSyntax?.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name?.Identifier.Text == Types.PackageRegistrationAttribute.AllowsBackgroundLoading);
                    Location relevantLocation = allowBackgroundLoadingSyntax?.GetLocation() ?? baseType.GetLocation();

                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        relevantLocation));
                }
            }
        }
    }
}
