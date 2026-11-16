// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Reports Visual Studio API references outside the SDK package surface provided to the analyzer.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic ID for references outside the provided Visual Studio SDK surface.
    /// </summary>
    public const string Id = "VSSDK013";

    /// <summary>
    /// The diagnostic ID for a missing or empty Visual Studio SDK surface input.
    /// </summary>
    public const string MissingSurfaceId = "VSSDK014";

    /// <summary>
    /// The diagnostic ID for reflection strings that name APIs outside the provided surface.
    /// </summary>
    public const string ReflectionTypeNameId = "VSSDK015";

    /// <summary>
    /// The diagnostic ID for reflection-based member lookup on Visual Studio SDK types.
    /// </summary>
    public const string ReflectionMemberLookupId = "VSSDK016";

    private const string SurfaceMetadataKey = "build_metadata.AdditionalFiles.VsSdkSurface";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: Id,
        title: "Use APIs in the Visual Studio SDK surface",
        messageFormat: "Visual Studio API '{0}' is in assembly '{1}', which is not included in the referenced SDK package surface",
        description: "Use Visual Studio APIs included in the SDK package surface referenced by the project.",
        helpLinkUri: Utils.GetHelpLink(Id),
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingSurfaceDescriptor = new(
        id: MissingSurfaceId,
        title: "Provide the Visual Studio SDK surface",
        messageFormat: "No non-empty Visual Studio SDK surface AdditionalFile was provided with VsSdkSurface metadata",
        description: "The Visual Studio SDK package must provide its surface list as a tagged AdditionalFile for API compatibility analysis.",
        helpLinkUri: Utils.GetHelpLink(MissingSurfaceId),
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

    private static readonly DiagnosticDescriptor ReflectionTypeNameDescriptor = new(
        id: ReflectionTypeNameId,
        title: "Use reflection with APIs in the Visual Studio SDK surface",
        messageFormat: "Reflection string '{0}' names an assembly or type outside the referenced SDK package surface",
        description: "Avoid loading Visual Studio assemblies and types that are not included in the SDK package surface referenced by the project.",
        helpLinkUri: Utils.GetHelpLink(ReflectionTypeNameId),
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ReflectionMemberLookupDescriptor = new(
        id: ReflectionMemberLookupId,
        title: "Avoid reflection-based member lookup on Visual Studio SDK types",
        messageFormat: "Reflection-based member lookup on Visual Studio type '{0}' can break when the SDK changes",
        description: "Avoid looking up Visual Studio SDK members by name or including non-public members.",
        helpLinkUri: Utils.GetHelpLink(ReflectionMemberLookupId),
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics =
        ImmutableArray.Create(Descriptor, MissingSurfaceDescriptor, ReflectionTypeNameDescriptor, ReflectionMemberLookupDescriptor);

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
            ImmutableHashSet<string> allowedAssemblies = ReadSurfaceAssemblies(start.Options, start.CancellationToken);
            if (allowedAssemblies.Count == 0)
            {
                start.RegisterCompilationEndAction(end => end.ReportDiagnostic(Diagnostic.Create(MissingSurfaceDescriptor, Location.None)));
                return;
            }

            start.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeName(syntaxContext, allowedAssemblies),
                SyntaxKind.IdentifierName,
                SyntaxKind.GenericName);
            start.RegisterOperationAction(
                operationContext => AnalyzeInvocation((IInvocationOperation)operationContext.Operation, operationContext, allowedAssemblies),
                OperationKind.Invocation);
        });
    }

    private static ImmutableHashSet<string> ReadSurfaceAssemblies(AnalyzerOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ImmutableHashSet<string>.Builder assemblies = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdditionalText file in options.AdditionalFiles)
        {
            AnalyzerConfigOptions fileOptions = options.AnalyzerConfigOptionsProvider.GetOptions(file);
            if (!fileOptions.TryGetValue(SurfaceMetadataKey, out string? surfaceName) ||
                string.IsNullOrWhiteSpace(surfaceName))
            {
                continue;
            }

            Microsoft.CodeAnalysis.Text.SourceText? text = file.GetText(cancellationToken);
            if (text is null)
            {
                continue;
            }

            foreach (Microsoft.CodeAnalysis.Text.TextLine line in text.Lines)
            {
                string assemblyName = line.ToString().Trim();
                if (assemblyName.Length > 0 && !assemblyName.StartsWith("#", StringComparison.Ordinal))
                {
                    assemblies.Add(assemblyName);
                }
            }
        }

        return assemblies.ToImmutable();
    }

    private static void AnalyzeName(SyntaxNodeAnalysisContext context, ImmutableHashSet<string> allowedAssemblies)
    {
        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is null && context.Node is IdentifierNameSyntax identifierName)
        {
            symbol = context.SemanticModel.GetAliasInfo(identifierName, context.CancellationToken);
        }

        if (symbol is IAliasSymbol alias)
        {
            symbol = alias.Target;
        }

        IAssemblySymbol? containingAssembly = symbol?.ContainingAssembly;
        if (symbol is null ||
            symbol is INamespaceSymbol ||
            containingAssembly is null ||
            SymbolEqualityComparer.Default.Equals(containingAssembly, context.Compilation.Assembly) ||
            !IsVisualStudioNamespace(symbol.ContainingNamespace) ||
            allowedAssemblies.Contains(containingAssembly.Identity.Name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            context.Node.GetLocation(),
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            containingAssembly.Identity.Name));
    }

    private static bool IsVisualStudioNamespace(INamespaceSymbol containingNamespace)
    {
        string namespaceName = containingNamespace.ToDisplayString();
        return namespaceName.Equals("Microsoft.VisualStudio", StringComparison.Ordinal) ||
            namespaceName.StartsWith("Microsoft.VisualStudio.", StringComparison.Ordinal);
    }

    private static void AnalyzeInvocation(
        IInvocationOperation invocation,
        OperationAnalysisContext context,
        ImmutableHashSet<string> allowedAssemblies)
    {
        IMethodSymbol method = invocation.TargetMethod;
        if (TryGetFirstStringArgument(invocation, out IArgumentOperation? stringArgument, out string? stringValue))
        {
            if (IsTypeNameLookup(method))
            {
                if (NamesTypeOutsideSurface(stringValue, context.Compilation, allowedAssemblies))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ReflectionTypeNameDescriptor, stringArgument.Value.Syntax.GetLocation(), stringValue));
                }
            }
            else if (IsAssemblyLoad(method) && NamesAssemblyOutsideSurface(stringValue, allowedAssemblies))
            {
                context.ReportDiagnostic(Diagnostic.Create(ReflectionTypeNameDescriptor, stringArgument.Value.Syntax.GetLocation(), stringValue));
            }
        }

        if (!IsReflectionMemberLookup(method) ||
            (!HasNonPublicFlag(invocation) && !HasConstantStringArgument(invocation)) ||
            !TryGetReflectedType(invocation.Instance, out ITypeSymbol? reflectedType) ||
            !IsVisualStudioSdkType(reflectedType, context.Compilation.Assembly, allowedAssemblies))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            ReflectionMemberLookupDescriptor,
            invocation.Syntax.GetLocation(),
            reflectedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
    }

    private static bool TryGetFirstStringArgument(
        IInvocationOperation invocation,
        [NotNullWhen(true)] out IArgumentOperation? stringArgument,
        [NotNullWhen(true)] out string? stringValue)
    {
        stringArgument = null;
        stringValue = null;
        IArgumentOperation? firstArgument = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
        if (firstArgument?.Value.ConstantValue is { HasValue: true, Value: string value })
        {
            stringArgument = firstArgument;
            stringValue = value;
            return true;
        }

        return false;
    }

    private static bool HasConstantStringArgument(IInvocationOperation invocation)
    {
        return invocation.Arguments.Any(argument => argument.Value.ConstantValue is { HasValue: true, Value: string });
    }

    private static bool IsTypeNameLookup(IMethodSymbol method)
    {
        string containingType = method.ContainingType.ToDisplayString();
        return (containingType == "System.Type" && method.Name == "GetType") ||
            (containingType == "System.Reflection.Assembly" && method.Name == "GetType");
    }

    private static bool IsAssemblyLoad(IMethodSymbol method)
    {
        return method.ContainingType.ToDisplayString() == "System.Reflection.Assembly" &&
            method.Name.StartsWith("Load", StringComparison.Ordinal);
    }

    private static bool IsReflectionMemberLookup(IMethodSymbol method)
    {
        if (method.ContainingType.ToDisplayString() != "System.Type")
        {
            return false;
        }

        return method.Name is "GetMethod" or "GetMethods" or "GetProperty" or "GetProperties" or
            "GetField" or "GetFields" or "GetMember" or "GetMembers" or "GetConstructor" or "GetConstructors" or
            "InvokeMember";
    }

    private static bool HasNonPublicFlag(IInvocationOperation invocation)
    {
        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            object? constantValue = argument.Value.ConstantValue.HasValue ? argument.Value.ConstantValue.Value : null;
            if ((constantValue is int integerFlags && (integerFlags & (int)BindingFlags.NonPublic) != 0) ||
                (constantValue is BindingFlags flags && (flags & BindingFlags.NonPublic) != 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetReflectedType(IOperation? instance, [NotNullWhen(true)] out ITypeSymbol? reflectedType)
    {
        while (instance is IConversionOperation conversion)
        {
            instance = conversion.Operand;
        }

        if (instance is ITypeOfOperation typeOf)
        {
            reflectedType = typeOf.TypeOperand;
            return true;
        }

        reflectedType = null;
        return false;
    }

    private static bool IsVisualStudioSdkType(
        ITypeSymbol type,
        IAssemblySymbol sourceAssembly,
        ImmutableHashSet<string> allowedAssemblies)
    {
        while (type is IArrayTypeSymbol arrayType)
        {
            type = arrayType.ElementType;
        }

        return type.ContainingAssembly is IAssemblySymbol containingAssembly &&
            !SymbolEqualityComparer.Default.Equals(containingAssembly, sourceAssembly) &&
            IsVisualStudioNamespace(type.ContainingNamespace) &&
            allowedAssemblies.Contains(containingAssembly.Identity.Name);
    }

    private static bool NamesTypeOutsideSurface(
        string value,
        Compilation compilation,
        ImmutableHashSet<string> allowedAssemblies)
    {
        int assemblySeparator = FindTopLevelComma(value);
        string typeName = (assemblySeparator < 0 ? value : value.Substring(0, assemblySeparator)).Trim();
        if (!typeName.StartsWith("Microsoft.VisualStudio.", StringComparison.Ordinal))
        {
            return false;
        }

        if (assemblySeparator >= 0)
        {
            string assemblyDisplayName = value.Substring(assemblySeparator + 1).Trim();
            string assemblyName = assemblyDisplayName.Split(',')[0].Trim();
            return assemblyName.Length == 0 || !allowedAssemblies.Contains(assemblyName);
        }

        INamedTypeSymbol? type = compilation.GetTypeByMetadataName(typeName);
        return type is null || type.ContainingAssembly is null || !allowedAssemblies.Contains(type.ContainingAssembly.Identity.Name);
    }

    private static int FindTopLevelComma(string value)
    {
        int bracketDepth = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '[')
            {
                bracketDepth++;
            }
            else if (value[i] == ']')
            {
                bracketDepth--;
            }
            else if (value[i] == ',' && bracketDepth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool NamesAssemblyOutsideSurface(string value, ImmutableHashSet<string> allowedAssemblies)
    {
        string assemblyName = value.Trim();
        if (assemblyName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 ||
            assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            assemblyName = Path.GetFileNameWithoutExtension(assemblyName);
        }

        int identitySeparator = assemblyName.IndexOf(',');
        if (identitySeparator >= 0)
        {
            assemblyName = assemblyName.Substring(0, identitySeparator).Trim();
        }

        return assemblyName.StartsWith("Microsoft.VisualStudio.", StringComparison.Ordinal) &&
            !allowedAssemblies.Contains(assemblyName);
    }
}
