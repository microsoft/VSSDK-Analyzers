// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Gets magic strings that describe types and their members.
    /// </summary>
    internal static class Types
    {
        /// <summary>
        /// Describes the "Microsoft.Assumes" type.
        /// </summary>
        internal static class Assumes
        {
            /// <summary>
            /// Gets the simple name of the "Microsoft.Assumes" type.
            /// </summary>
            internal const string TypeName = "Assumes";

            /// <summary>
            /// The name of the "Microsoft.Assumes.Present{T}(T)" method.
            /// </summary>
            internal const string Present = "Present";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.Microsoft;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the Microsoft.VisualStudio.OLE.Interop.IServiceProvider type.
        /// </summary>
        internal static class IOleServiceProvider
        {
            /// <summary>
            /// Gets the simple name of the Microsoft.VisualStudio.OLE.Interop.IServiceProvider type.
            /// </summary>
            internal const string TypeName = "IServiceProvider";

            /// <summary>
            /// The name of the QueryService method.
            /// </summary>
            internal const string QueryService = "QueryService";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioOLEInterop;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the System.IServiceProvider type.
        /// </summary>
        internal static class IServiceProvider
        {
            /// <summary>
            /// Gets the simple name of the System.IServiceProvider type.
            /// </summary>
            internal const string TypeName = "IServiceProvider";

            /// <summary>
            /// The name of the GetService method.
            /// </summary>
            internal const string GetService = "GetService";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.System;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.ServiceProvider" type.
        /// </summary>
        internal static class ServiceProvider
        {
            /// <summary>
            /// Gets the simple name of the "Shell.ServiceProvider" type.
            /// </summary>
            internal const string TypeName = "ServiceProvider";

            /// <summary>
            /// The name of the GetService method.
            /// </summary>
            internal const string GetService = "GetService";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.IAsyncServiceProvider" type.
        /// </summary>
        internal static class IAsyncServiceProvider
        {
            /// <summary>
            /// Gets the simple name of the "Shell.IAsyncServiceProvider" type.
            /// </summary>
            internal const string TypeName = "IAsyncServiceProvider";

            /// <summary>
            /// The name of the "Shell.IAsyncServiceProvider.GetServiceAsync" method.
            /// </summary>
            internal const string GetServiceAsync = "GetServiceAsync";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.AsyncPackage" type.
        /// </summary>
        internal static class AsyncPackage
        {
            /// <summary>
            /// Gets the simple name of the "Shell.AsyncPackage" type.
            /// </summary>
            internal const string TypeName = "AsyncPackage";

            /// <summary>
            /// The name of the InitializeAsync method.
            /// </summary>
            internal const string InitializeAsync = "InitializeAsync";

            /// <summary>
            /// The name of the GetServiceAsync method.
            /// </summary>
            internal const string GetServiceAsync = "GetServiceAsync";

            /// <summary>
            /// The name of the GetAsyncToolWindowFactory method.
            /// </summary>
            internal const string GetAsyncToolWindowFactory = "GetAsyncToolWindowFactory";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.Package" type.
        /// </summary>
        internal static class Package
        {
            /// <summary>
            /// Gets the simple name of the "Shell.Package" type.
            /// </summary>
            internal const string TypeName = "Package";

            /// <summary>
            /// The name of the Initialize method.
            /// </summary>
            internal const string Initialize = "Initialize";

            /// <summary>
            /// The name of the GetService method.
            /// </summary>
            internal const string GetService = "GetService";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.PackageUtilities" type.
        /// </summary>
        internal static class PackageUtilities
        {
            /// <summary>
            /// Gets the simple name of the "Shell.PackageUtilities" type.
            /// </summary>
            internal const string TypeName = "PackageUtilities";

            /// <summary>
            /// The name of the QueryService method.
            /// </summary>
            internal const string QueryService = "QueryService";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the <see cref="System.Threading.CancellationToken"/> type.
        /// </summary>
        internal static class CancellationToken
        {
            /// <summary>
            /// Gets the simple name of the <see cref="System.Threading.CancellationToken"/> type.
            /// </summary>
            internal const string TypeName = "CancellationToken";

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
            internal const string TypeName = "Task";

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
        /// Describes the "Shell.ThreadHelper" type.
        /// </summary>
        internal static class ThreadHelper
        {
            /// <summary>
            /// Gets the simple name of the "Shell.ThreadHelper" type.
            /// </summary>
            internal const string TypeName = "ThreadHelper";

            /// <summary>
            /// The name of the "Shell.ThreadHelper.JoinableTaskFactory" property.
            /// </summary>
            internal const string JoinableTaskFactory = "JoinableTaskFactory";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Threading.JoinableTaskContext" type.
        /// </summary>
        internal static class JoinableTaskContext
        {
            /// <summary>
            /// Gets the simple name of the "Threading.JoinableTaskContext" type.
            /// </summary>
            internal const string TypeName = "JoinableTaskContext";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioThreading;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Threading.JoinableTaskFactory" type.
        /// </summary>
        internal static class JoinableTaskFactory
        {
            /// <summary>
            /// Gets the simple name of the "Threading.JoinableTaskFactory" type.
            /// </summary>
            internal const string TypeName = "JoinableTaskFactory";

            /// <summary>
            /// The name of the <see cref="JoinableTaskFactory.SwitchToMainThreadAsync"/> method.
            /// </summary>
            internal const string SwitchToMainThreadAsync = "SwitchToMainThreadAsync";

            /// <summary>
            /// The name of the <see cref="JoinableTaskFactory.RunAsync"/> method.
            /// </summary>
            internal const string RunAsync = "RunAsync";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioThreading;
        }

        /// <summary>
        /// Describes the "Threading.JoinableTask" type.
        /// </summary>
        internal static class JoinableTask
        {
            /// <summary>
            /// Gets the simple name of the "Threading.JoinableTask" type.
            /// </summary>
            internal const string TypeName = "JoinableTask";

            /// <summary>
            /// The name of the "Threading.JoinableTask.Join" method.
            /// </summary>
            internal const string Join = "Join";

            /// <summary>
            /// The name of the "Threading.JoinableTask.JoinAsync" method.
            /// </summary>
            internal const string JoinAsync = "JoinAsync";
        }

        /// <summary>
        /// Describes the <see cref="System.IProgress{T}"/> type.
        /// </summary>
        internal static class IProgress
        {
            /// <summary>
            /// Gets the simple name of the <see cref="System.IProgress{T}"/> type.
            /// </summary>
            internal const string TypeName = "IProgress";

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
        /// Describes the "Shell.ServiceProgressData" type.
        /// </summary>
        internal static class ServiceProgressData
        {
            /// <summary>
            /// Gets the simple name of the "Shell.ServiceProgressData" type.
            /// </summary>
            internal const string TypeName = "ServiceProgressData";

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
        /// Describes the "Shell.PackageRegistrationAttribute" type.
        /// </summary>
        internal static class PackageRegistrationAttribute
        {
            /// <summary>
            /// Gets the simple name of the "Shell.PackageRegistrationAttribute" type.
            /// </summary>
            internal const string TypeName = "PackageRegistrationAttribute";

            /// <summary>
            /// Gets the name of the "Shell.PackageRegistrationAttribute.AllowsBackgroundLoading" property.
            /// </summary>
            internal const string AllowsBackgroundLoading = "AllowsBackgroundLoading";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.ProvideToolWindowAttribute" type.
        /// </summary>
        internal static class ProvideToolWindowAttribute
        {
            /// <summary>
            /// Gets the simple name of the "Shell.ProvideToolWindowAttribute" type.
            /// </summary>
            internal const string TypeName = "ProvideToolWindowAttribute";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.ProvideAutoLoadAttribute" type.
        /// </summary>
        internal static class ProvideAutoLoadAttribute
        {
            /// <summary>
            /// Gets the simple name of the "Shell.ProvideAutoLoadAttribute" type.
            /// </summary>
            internal const string TypeName = "ProvideAutoLoadAttribute";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// Describes the "Shell.PackageAutoLoadFlags" type.
        /// </summary>
        internal static class PackageAutoLoadFlags
        {
            /// <summary>
            /// Gets the simple name of the "Shell.PackageAutoLoadFlags" type.
            /// </summary>
            internal const string TypeName = "PackageAutoLoadFlags";

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;

            /// <summary>
            /// Copy of auto load flag values from "Shell.PackageAutoLoadFlags".
            /// </summary>
            internal enum Values
            {
                /// <summary>
                /// Indicates synchronous load in all versions of Visual Studio.
                /// </summary>
                None = 0,

                /// <summary>
                /// Indicates auto load request should be ignored when Visual Studio has UIContextRules feature.
                /// </summary>
                SkipWhenUIContextRulesActive = 1,

                /// <summary>
                /// Indicates auto load should be requested asynchronously.
                /// </summary>
                BackgroundLoad = 2,
            }

            /// <summary>
            /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
            /// </summary>
            internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

            /// <summary>
            /// Gets the fully-qualified name of this type as a string.
            /// </summary>
            internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
        }

        /// <summary>
        /// MEFv2 types (those under the System.ComponentModel.Composition namespace).
        /// </summary>
        internal static class MEFv1
        {
            /// <summary>
            /// Describes the <see cref="System.ComponentModel.Composition.ExportAttribute"/> type.
            /// </summary>
            internal static class ExportAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.ComponentModel.Composition.ExportAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.ComponentModel.Composition.ExportAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComponentModelComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }

            /// <summary>
            /// Describes the <see cref="System.ComponentModel.Composition.ImportingConstructorAttribute"/> type.
            /// </summary>
            internal static class ImportingConstructorAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.ComponentModel.Composition.ImportingConstructorAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.ComponentModel.Composition.ImportingConstructorAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComponentModelComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }

            /// <summary>
            /// Describes the <see cref="System.ComponentModel.Composition.IPartImportsSatisfiedNotification"/> type.
            /// </summary>
            internal static class IPartImportsSatisfiedNotification
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.ComponentModel.Composition.IPartImportsSatisfiedNotification"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.ComponentModel.Composition.IPartImportsSatisfiedNotification);

                /// <summary>
                /// Gets the method name of <see cref="System.ComponentModel.Composition.IPartImportsSatisfiedNotification.OnImportsSatisfied"/>.
                /// </summary>
                internal const string OnImportsSatisfied = "OnImportsSatisfied";

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComponentModelComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;

                /// <summary>
                /// Gets the full name of <see cref="System.ComponentModel.Composition.IPartImportsSatisfiedNotification.OnImportsSatisfied"/>.
                /// </summary>
                internal static string OnImportsSatisfiedFullName { get; } = string.Join(".", Namespace) + "." + TypeName + "." + OnImportsSatisfied;
            }

            /// <summary>
            /// Describes the <see cref="System.ComponentModel.Composition.InheritedExportAttribute"/> type.
            /// </summary>
            internal static class InheritedExportAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.ComponentModel.Composition.InheritedExportAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.ComponentModel.Composition.InheritedExportAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComponentModelComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }
        }

        /// <summary>
        /// MEFv2 types (those under the System.Composition namespace).
        /// </summary>
        internal static class MEFv2
        {
            /// <summary>
            /// Describes the <see cref="System.Composition.ExportAttribute"/> type.
            /// </summary>
            internal static class ExportAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.Composition.ExportAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.Composition.ExportAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }

            /// <summary>
            /// Describes the <see cref="System.Composition.ImportingConstructorAttribute"/> type.
            /// </summary>
            internal static class ImportingConstructorAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.Composition.ImportingConstructorAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.Composition.ImportingConstructorAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }

            /// <summary>
            /// Describes the <see cref="System.Composition.OnImportsSatisfiedAttribute"/> type.
            /// </summary>
            internal static class OnImportsSatisfiedAttribute
            {
                /// <summary>
                /// Gets the simple name of the <see cref="System.Composition.OnImportsSatisfiedAttribute"/> type.
                /// </summary>
                internal const string TypeName = nameof(System.Composition.OnImportsSatisfiedAttribute);

                /// <summary>
                /// Gets an array of the nesting namespaces for this type.
                /// </summary>
                internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemComponentModelComposition;

                /// <summary>
                /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/> for this type.
                /// </summary>
                internal static TypeSyntax TypeSyntax { get; } = Utils.QualifyName(Namespace, SyntaxFactory.IdentifierName(TypeName));

                /// <summary>
                /// Gets the fully-qualified name of this type as a string.
                /// </summary>
                internal static string FullName { get; } = string.Join(".", Namespace) + "." + TypeName;
            }
        }
    }
}
