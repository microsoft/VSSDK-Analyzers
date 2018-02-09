// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Gets magic strings that describe types and their members.
    /// </summary>
    internal static class Types
    {
        /// <summary>
        /// Describes the <see cref="Shell.AsyncPackage"/> type.
        /// </summary>
        internal static class AsyncPackage
        {
            /// <summary>
            /// Gets the simple name of the <see cref="Shell.AsyncPackage"/> type.
            /// </summary>
            internal const string TypeName = nameof(Shell.AsyncPackage);

            /// <summary>
            /// The name of the Initialize method.
            /// </summary>
            internal const string Initialize = "Initialize";

            /// <summary>
            /// The name of the InitializeAsync method.
            /// </summary>
            internal const string InitializeAsync = "InitializeAsync";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));
        }

        /// <summary>
        /// Describes the <see cref="System.Threading.CancellationToken"/> type.
        /// </summary>
        internal static class CancellationToken
        {
            /// <summary>
            /// Gets the simple name of the <see cref="System.Threading.CancellationToken"/> type.
            /// </summary>
            internal const string TypeName = nameof(System.Threading.CancellationToken);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemThreading;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));
        }

        /// <summary>
        /// Describes the <see cref="System.Threading.Tasks.Task"/> type.
        /// </summary>
        internal static class Task
        {
            /// <summary>
            /// Gets the simple name of the <see cref="System.Threading.Tasks.Task"/> type.
            /// </summary>
            internal const string TypeName = nameof(System.Threading.Tasks.Task);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemThreadingTasks;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));
        }

        /// <summary>
        /// Describes the <see cref="Shell.ThreadHelper"/> type.
        /// </summary>
        internal static class ThreadHelper
        {
            /// <summary>
            /// Gets the simple name of the <see cref="Shell.ThreadHelper"/> type.
            /// </summary>
            internal const string TypeName = nameof(Shell.ThreadHelper);

            /// <summary>
            /// The name of the <see cref="Shell.ThreadHelper.JoinableTaskFactory"/> property.
            /// </summary>
            internal const string JoinableTaskFactory = nameof(Shell.ThreadHelper.JoinableTaskFactory);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));
        }

        /// <summary>
        /// Describes the <see cref="Threading.JoinableTaskFactory"/> type.
        /// </summary>
        internal static class JoinableTaskFactory
        {
            /// <summary>
            /// The name of the <see cref="JoinableTaskFactory.SwitchToMainThreadAsync"/> method.
            /// </summary>
            internal const string SwitchToMainThreadAsync = nameof(JoinableTaskFactory.SwitchToMainThreadAsync);
        }

        /// <summary>
        /// Describes the <see cref="System.IProgress{T}"/> type.
        /// </summary>
        internal static class IProgress
        {
            /// <summary>
            /// Gets the simple name of the <see cref="System.IProgress{T}"/> type.
            /// </summary>
            internal const string TypeName = nameof(System.IProgress<int>);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.System;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            /// <param name="typeArgument">The type argument for the <see cref="System.IProgress{T}"/> type.</param>
            /// <returns>The type syntax.</returns>
            internal static TypeSyntax TypeSyntaxOf(TypeSyntax typeArgument)
            {
                return Utils.QualifyName(
                    Namespace,
                    SyntaxFactory.GenericName(TypeName).AddTypeArgumentListArguments(typeArgument));
            }
        }

        /// <summary>
        /// Describes the <see cref="Shell.ServiceProgressData"/> type.
        /// </summary>
        internal static class ServiceProgressData
        {
            /// <summary>
            /// Gets the simple name of the <see cref="Shell.ServiceProgressData"/> type.
            /// </summary>
            internal const string TypeName = nameof(Shell.ServiceProgressData);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));
        }
    }
}
