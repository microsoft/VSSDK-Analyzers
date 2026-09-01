// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Reports references to an older version exposed by <see cref="Types.VisualStudioServices"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VSSDK012UseLatestVisualStudioServicesVersionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
    /// </summary>
    public const string Id = "VSSDK012";

    /// <summary>
    /// A reusable descriptor for diagnostics produced by this analyzer.
    /// </summary>
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: Id,
        title: "Use the latest VisualStudioServices version",
        messageFormat: "Use VisualStudioServices.{1} instead of VisualStudioServices.{0}",
        description: "Use the latest VisualStudioServices version available in the referenced Visual Studio SDK to benefit from the latest service functionality and performance improvements.",
        helpLinkUri: Utils.GetHelpLink(Id),
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
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
            INamedTypeSymbol? visualStudioServices = start.Compilation.GetTypeByMetadataName(Types.VisualStudioServices.FullName);
            if (visualStudioServices is null ||
                !TryGetLatestVersionProperty(visualStudioServices, out IPropertySymbol? latestProperty, out Version? latestVersion))
            {
                return;
            }

            start.RegisterOperationAction(
                Utils.DebuggableWrapper(context => AnalyzePropertyReference(context, visualStudioServices, latestProperty, latestVersion)),
                OperationKind.PropertyReference);
        });
    }

    private static void AnalyzePropertyReference(
        OperationAnalysisContext context,
        INamedTypeSymbol visualStudioServices,
        IPropertySymbol latestProperty,
        Version latestVersion)
    {
        var propertyReference = (IPropertyReferenceOperation)context.Operation;
        for (IOperation? ancestor = propertyReference.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor is INameOfOperation)
            {
                return;
            }
        }

        IPropertySymbol property = propertyReference.Property;
        if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, visualStudioServices) ||
            SymbolEqualityComparer.Default.Equals(property, latestProperty) ||
            !TryGetVersion(property.Name, out Version? referencedVersion) ||
            referencedVersion.CompareTo(latestVersion) >= 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyReference.Syntax.GetLocation(), property.Name, latestProperty.Name));
    }

    private static bool TryGetLatestVersionProperty(
        INamedTypeSymbol visualStudioServices,
        [NotNullWhen(true)] out IPropertySymbol? latestProperty,
        [NotNullWhen(true)] out Version? latestVersion)
    {
        latestProperty = null;
        latestVersion = null;

        foreach (IPropertySymbol property in visualStudioServices.GetMembers().OfType<IPropertySymbol>())
        {
            if (TryGetVersion(property.Name, out Version? version) &&
                (latestVersion is null || version.CompareTo(latestVersion) > 0))
            {
                latestProperty = property;
                latestVersion = version;
            }
        }

        return latestProperty is not null;
    }

    private static bool TryGetVersion(string memberName, [NotNullWhen(true)] out Version? version)
    {
        const string prefix = "VS";
        version = null;
        if (!memberName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        int separatorIndex = memberName.IndexOf('_', prefix.Length);
        string majorText = separatorIndex < 0
            ? memberName.Substring(prefix.Length)
            : memberName.Substring(prefix.Length, separatorIndex - prefix.Length);
        string minorText = separatorIndex < 0 ? "0" : memberName.Substring(separatorIndex + 1);
        if (majorText.Length != 4 ||
            minorText.Length == 0 ||
            minorText.IndexOf('_') >= 0 ||
            !int.TryParse(majorText, NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
            !int.TryParse(minorText, NumberStyles.None, CultureInfo.InvariantCulture, out int minor))
        {
            return false;
        }

        version = new Version(major, minor);
        return true;
    }
}
