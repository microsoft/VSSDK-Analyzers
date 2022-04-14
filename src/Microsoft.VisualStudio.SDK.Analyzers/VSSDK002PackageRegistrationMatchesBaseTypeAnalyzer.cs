// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
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
        /// The name of the property in the <see cref="Diagnostic"/> that we create that contains either <see cref="Types.Package.TypeName"/> or <see cref="Types.AsyncPackage.TypeName"/>
        /// to reflect the base type of the package.
        /// </summary>
        internal const string BaseTypeDiagnosticPropertyName = "BaseType";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "PackageRegistration matches Package",
            messageFormat: "The PackageRegistrationAttribute.AllowsBackgroundLoading should be set to true if and only if the package derives from AsyncPackage",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Error,
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
                INamedTypeSymbol? packageType = compilationContext.Compilation.GetTypeByMetadataName(Types.Package.FullName)?.OriginalDefinition;
                INamedTypeSymbol? asyncPackageType = compilationContext.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName)?.OriginalDefinition;
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
            BaseTypeSyntax? baseType = declaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null)
            {
                return;
            }

            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type, context.CancellationToken).Symbol?.OriginalDefinition as ITypeSymbol;

            if (Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, packageType))
            {
                INamedTypeSymbol packageRegistrationType = context.Compilation.GetTypeByMetadataName(Types.PackageRegistrationAttribute.FullName);
                INamedTypeSymbol userClassSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
                AttributeData? packageRegistrationInstance = userClassSymbol?.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, packageRegistrationType));
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
                    if (attributeSyntax is object)
                    {
                        AttributeArgumentSyntax? allowBackgroundLoadingSyntax = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name?.Identifier.Text == Types.PackageRegistrationAttribute.AllowsBackgroundLoading);
                        Location location = allowBackgroundLoadingSyntax?.GetLocation() ?? attributeSyntax.GetLocation();
                        ImmutableDictionary<string, string> properties = ImmutableDictionary.Create<string, string>()
                            .Add(BaseTypeDiagnosticPropertyName, isAsyncPackageBaseType ? Types.AsyncPackage.TypeName : Types.Package.TypeName);
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, properties));
                    }
                }
            }
        }
    }
}
