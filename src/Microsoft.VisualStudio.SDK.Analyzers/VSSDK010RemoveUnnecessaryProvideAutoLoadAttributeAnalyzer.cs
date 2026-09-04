// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Reports <see cref="Types.ProvideAutoLoadAttribute"/> usages on packages that do not perform initialization.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VSSDK010RemoveUnnecessaryProvideAutoLoadAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
    /// </summary>
    public const string Id = "VSSDK010";

    /// <summary>
    /// A reusable descriptor for diagnostics produced by this analyzer.
    /// </summary>
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: Id,
        title: "Remove unnecessary ProvideAutoLoad attribute",
        messageFormat: "Remove this ProvideAutoLoad attribute because the package does not override {0}",
        description: "A package that only provides registration attributes does not need to be automatically loaded.",
        helpLinkUri: Utils.GetHelpLink(Id),
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(Descriptor);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

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
            INamedTypeSymbol? packageType = start.Compilation.GetTypeByMetadataName(Types.Package.FullName);
            INamedTypeSymbol? asyncPackageType = start.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName);
            INamedTypeSymbol? provideAutoLoadAttributeType = start.Compilation.GetTypeByMetadataName(Types.ProvideAutoLoadAttribute.FullName);
            IMethodSymbol? initializeMethod = packageType?.GetMembers(Types.Package.Initialize)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Parameters.Length == 0);
            IMethodSymbol? initializeAsyncMethod = asyncPackageType?.GetMembers(Types.AsyncPackage.InitializeAsync)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Parameters.Length == 2);
            IMethodSymbol? onAfterPackageLoadedAsyncMethod = asyncPackageType?.GetMembers(Types.AsyncPackage.OnAfterPackageLoadedAsync)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Parameters.Length == 1);

            if (packageType is not null &&
                asyncPackageType is not null &&
                provideAutoLoadAttributeType is not null &&
                initializeMethod is not null &&
                initializeAsyncMethod is not null)
            {
                start.RegisterSymbolAction(
                    Utils.DebuggableWrapper(symbolContext => AnalyzeNamedType(
                        symbolContext,
                        packageType,
                        asyncPackageType,
                        provideAutoLoadAttributeType,
                        initializeMethod,
                        initializeAsyncMethod,
                        onAfterPackageLoadedAsyncMethod)),
                    SymbolKind.NamedType);
            }
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol packageType,
        INamedTypeSymbol asyncPackageType,
        INamedTypeSymbol provideAutoLoadAttributeType,
        IMethodSymbol initializeMethod,
        IMethodSymbol initializeAsyncMethod,
        IMethodSymbol? onAfterPackageLoadedAsyncMethod)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.IsAbstract || !Utils.IsDerivedFrom(type, packageType))
        {
            return;
        }

        ImmutableArray<AttributeData> provideAutoLoadAttributes = type.GetAttributes()
            .Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, provideAutoLoadAttributeType))
            .ToImmutableArray();
        if (provideAutoLoadAttributes.IsEmpty)
        {
            return;
        }

        IMethodSymbol methodToOverride;
        if (Utils.IsEqualToOrDerivedFrom(type, asyncPackageType))
        {
            methodToOverride = initializeAsyncMethod;
        }
        else
        {
            methodToOverride = initializeMethod;
        }

        if (OverridesMethod(type, methodToOverride) ||
            (Utils.IsEqualToOrDerivedFrom(type, asyncPackageType) &&
                onAfterPackageLoadedAsyncMethod is not null &&
                OverridesMethod(type, onAfterPackageLoadedAsyncMethod)))
        {
            return;
        }

        foreach (AttributeData attribute in provideAutoLoadAttributes)
        {
            Location? location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, methodToOverride.Name));
            }
        }
    }

    private static bool OverridesMethod(INamedTypeSymbol type, IMethodSymbol methodToOverride)
    {
        for (INamedTypeSymbol? currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (IMethodSymbol candidate in currentType.GetMembers(methodToOverride.Name).OfType<IMethodSymbol>())
            {
                for (IMethodSymbol? overriddenMethod = candidate.OverriddenMethod; overriddenMethod is not null; overriddenMethod = overriddenMethod.OverriddenMethod)
                {
                    if (SymbolEqualityComparer.Default.Equals(overriddenMethod, methodToOverride))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
