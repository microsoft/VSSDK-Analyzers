// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Simplification;

    /// <summary>
    /// Internal utilities for use by analyzers.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Wraps an analyzer processor with a Debugger.Launch call in an exception filter for debug builds.
        /// </summary>
        /// <param name="handler">The handler to wrap.</param>
        /// <returns>The debug-ready handler.</returns>
        internal static Action<SyntaxNodeAnalysisContext> DebuggableWrapper(Action<SyntaxNodeAnalysisContext> handler)
        {
            return ctxt =>
            {
                try
                {
                    handler(ctxt);
                }
                catch (Exception ex) when (LaunchDebuggerExceptionFilter())
                {
                    throw new Exception($"Analyzer failure while processing syntax at {ctxt.Node.SyntaxTree.FilePath}({ctxt.Node.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1},{ctxt.Node.GetLocation()?.GetLineSpan().StartLinePosition.Character + 1}): {ex.GetType()} {ex.Message}. Syntax: {ctxt.Node}", ex);
                }
            };
        }

        /// <summary>
        /// Wraps an analyzer processor with a Debugger.Launch call in an exception filter for debug builds.
        /// </summary>
        /// <param name="handler">The handler to wrap.</param>
        /// <returns>The debug-ready handler.</returns>
        internal static Action<SymbolAnalysisContext> DebuggableWrapper(Action<SymbolAnalysisContext> handler)
        {
            return ctxt =>
            {
                try
                {
                    handler(ctxt);
                }
                catch (Exception ex) when (LaunchDebuggerExceptionFilter())
                {
                    throw new Exception($"Analyzer failure while processing symbol {ctxt.Symbol} at {ctxt.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath}({ctxt.Symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line},{ctxt.Symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Character}): {ex.GetType()} {ex.Message}", ex);
                }
            };
        }

        /// <summary>
        /// Gets the help link to use for each analyzer.
        /// </summary>
        /// <param name="analyzerId">The <see cref="DiagnosticDescriptor.Id"/> for the diagnostic to get help on.</param>
        /// <returns>The absolute URL with documentation on the specified analyzer.</returns>
        internal static string GetHelpLink(string analyzerId)
        {
            return $"https://github.com/Microsoft/VSSDK-Analyzers/blob/master/doc/{analyzerId}.md";
        }

        /// <summary>
        /// Gets a value indicating whether one type is equal to or derives from another type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="baseType">The type to compare with, that may be a base type of <paramref name="type"/></param>
        /// <returns><c>true</c> if <paramref name="baseType"/> is a base type or equal to <paramref name="type"/>.</returns>
        internal static bool IsEqualToOrDerivedFrom(ITypeSymbol type, ITypeSymbol baseType)
        {
            return type?.OriginalDefinition == baseType || IsDerivedFrom(type, baseType);
        }

        /// <summary>
        /// Gets a value indicating whether one type derives from another type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="baseType">The type to compare with, that may be a base type of <paramref name="type"/></param>
        /// <returns><c>true</c> if <paramref name="baseType"/> is a base type of <paramref name="type"/>.</returns>
        internal static bool IsDerivedFrom(ITypeSymbol type, ITypeSymbol baseType)
        {
            type = type?.BaseType;
            while (type != null)
            {
                if (type.OriginalDefinition == baseType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Produces the syntax necessary to qualify a simple name.
        /// </summary>
        /// <param name="qualifiers">The qualifiers (e.g. the namespace that qualifies a type).</param>
        /// <param name="simpleName">The simple type name.</param>
        /// <returns>The qualified type name.</returns>
        internal static NameSyntax QualifyName(IReadOnlyList<string> qualifiers, SimpleNameSyntax simpleName)
        {
            if (qualifiers == null)
            {
                throw new ArgumentNullException(nameof(qualifiers));
            }

            if (simpleName == null)
            {
                throw new ArgumentNullException(nameof(simpleName));
            }

            if (qualifiers.Count == 0)
            {
                throw new ArgumentException("At least one qualifier required.");
            }

            NameSyntax result = SyntaxFactory.IdentifierName(qualifiers[0]);
            for (int i = 1; i < qualifiers.Count; i++)
            {
                var rightSide = SyntaxFactory.IdentifierName(qualifiers[i]);
                result = SyntaxFactory.QualifiedName(result, rightSide);
            }

            return SyntaxFactory.QualifiedName(result, simpleName)
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private static bool LaunchDebuggerExceptionFilter()
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            return true;
        }
    }
}