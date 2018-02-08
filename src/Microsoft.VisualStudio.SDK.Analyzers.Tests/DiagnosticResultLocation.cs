// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers.Tests
{
    using System;

    /// <summary>
    /// Location where the diagnostic appears, as determined by path, line number, and column number.
    /// </summary>
    public struct DiagnosticResultLocation
    {
        public string Path;
        public int Line;
        public int Column;
        public int EndLine;
        public int EndColumn;

        public DiagnosticResultLocation(string path, int line, int column, int endLine = -1, int endColumn = -1)
        {
            if (line < 0 && column < 0)
            {
                throw new ArgumentException("At least one of line and column must be > 0");
            }

            if (line < -1 || column < -1)
            {
                throw new ArgumentException("Both line and column must be >= -1");
            }

            this.Path = path;
            this.Line = line;
            this.Column = column;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }
    }
}
