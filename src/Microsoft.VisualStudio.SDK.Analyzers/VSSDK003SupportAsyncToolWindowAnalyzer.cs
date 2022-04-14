// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Identifies cases where a <see cref="ToolWindowPane" /> is proffered by a VS package without supporting asynchronous initialization.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK003SupportAsyncToolWindowAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK003";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Support async tool windows",
            messageFormat: "Tool windows should support async construction",
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
                INamedTypeSymbol? asyncPackageType = compilationContext.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName)?.OriginalDefinition;
                if (asyncPackageType is object)
                {
                    INamedTypeSymbol? provideToolWindowAttribute = compilationContext.Compilation.GetTypeByMetadataName(Types.ProvideToolWindowAttribute.FullName)?.OriginalDefinition;
                    bool targetVSSupportsAsyncToolWindows = asyncPackageType.MemberNames.Any(n => n == Types.AsyncPackage.GetAsyncToolWindowFactory);
                    if (targetVSSupportsAsyncToolWindows && provideToolWindowAttribute is object)
                    {
                        // Reuse the type symbols we looked up so that we don't have to look them up for every single class declaration.
                        compilationContext.RegisterSyntaxNodeAction(
                            Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt, asyncPackageType, provideToolWindowAttribute)),
                            SyntaxKind.ClassDeclaration);
                    }
                }
            });
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol asyncPackageType, INamedTypeSymbol provideToolWindowAttributeType)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            BaseTypeSyntax? baseType = declaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null)
            {
                return;
            }

            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type, context.CancellationToken).Symbol?.OriginalDefinition as ITypeSymbol;

            if (!Utils.IsEqualToOrDerivedFrom(baseTypeSymbol, asyncPackageType))
            {
                return;
            }

            INamedTypeSymbol userClassSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
            AttributeData? packageRegistrationInstance = userClassSymbol?.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, provideToolWindowAttributeType));
            TypedConstant? firstParameter = packageRegistrationInstance?.ConstructorArguments.FirstOrDefault();
            if (firstParameter.HasValue && firstParameter.Value.Kind == TypedConstantKind.Type && firstParameter.Value.Value is INamedTypeSymbol typeOfUserToolWindow)
            {
                // If the tool window has a constructor that takes a parameter,
                // then the tool window is probably created asynchronously, because you
                // cannot easily pass a parameter when creating a synchronous tool window.
                bool toolWindowHasCtorWithOneParameter = typeOfUserToolWindow.GetMembers(ConstructorInfo.ConstructorName).OfType<IMethodSymbol>().Any(c => c.Parameters.Length == 1);
                if (toolWindowHasCtorWithOneParameter)
                {
                    return;
                }

                // If the `GetAsyncToolWindowFactory` method has been overridden,
                // then it's highly likely that the tool window will be created asynchronously.
                var packageSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken)?.OriginalDefinition as ITypeSymbol;
                if (this.IsGetAsyncToolWindowFactoryOverridden(packageSymbol, asyncPackageType))
                {
                    return;
                }

                if (packageRegistrationInstance!.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is AttributeSyntax attributeSyntax)
                {
                    AttributeArgumentSyntax firstArgumentSyntax = attributeSyntax.ArgumentList.Arguments.First();
                    Location diagnosticLocation = firstArgumentSyntax.GetLocation();
                    if (firstArgumentSyntax.Expression is TypeOfExpressionSyntax typeOfArg)
                    {
                        diagnosticLocation = typeOfArg.Type.GetLocation();
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, diagnosticLocation));
                }
            }
        }

        private bool IsGetAsyncToolWindowFactoryOverridden(ITypeSymbol? packageTypeSymbol, INamedTypeSymbol asyncPackageType)
        {
            // Step up through the type hierarchy of the package class until we reach
            // the `AsyncPackage` type. Once we reach the `AsyncPackage` type, then the
            // `GetAsyncToolWindowFactory` method cannot be overridden.
            while (!SymbolEqualityComparer.Default.Equals(packageTypeSymbol?.OriginalDefinition, asyncPackageType))
            {
                IMethodSymbol? getAsyncToolWindowFactoryMethod = packageTypeSymbol?.GetMembers(Types.AsyncPackage.GetAsyncToolWindowFactory).OfType<IMethodSymbol>().FirstOrDefault();

                if (getAsyncToolWindowFactoryMethod != null && getAsyncToolWindowFactoryMethod.IsOverride)
                {
                    return true;
                }

                packageTypeSymbol = packageTypeSymbol?.BaseType;
            }

            return false;
        }
    }
}
