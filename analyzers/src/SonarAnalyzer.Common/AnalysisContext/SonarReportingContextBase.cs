/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

namespace SonarAnalyzer.AnalysisContext;

public class SonarDiagnostic : Diagnostic
{
    public override DiagnosticDescriptor Descriptor { get; }
    public override string Id { get; }
    public override DiagnosticSeverity Severity { get; }
    public override int WarningLevel { get; }
    public override bool IsSuppressed { get; }
    public override Location Location { get; }
    public override IReadOnlyList<Location> AdditionalLocations { get; }
    public string Message { get; }

    public override ImmutableDictionary<string, string> Properties { get; }

    public SonarDiagnostic(DiagnosticDescriptor descriptor, Location location, IReadOnlyList<Location> additionalLocations, string message, ImmutableDictionary<string, string> properties)
    {
        Properties = properties;
        Descriptor = descriptor;
        Id = descriptor.Id;
        Severity = descriptor.DefaultSeverity;
        WarningLevel = GetDefaultWarningLevel(descriptor.DefaultSeverity);
        IsSuppressed = false;
        Location = location;
        AdditionalLocations = additionalLocations;
        Message = message;
    }

    public override string GetMessage(IFormatProvider formatProvider = null) => Message;

    public override int GetHashCode() => HashCode.Combine(Descriptor, Message, Location, Severity, WarningLevel);

    public override bool Equals(object obj) => Equals(obj as Diagnostic);

    public override bool Equals(Diagnostic obj)
    {
        if (!(obj is SonarDiagnostic other))
        {
            return false;
        }

        return Descriptor.Equals(other.Descriptor) &&
               Message == other.Message &&
               Location == other.Location &&
               Severity == other.Severity &&
               WarningLevel == other.WarningLevel;
    }

    // Copied from Diagnostic.GetDefaultWarningLevel
    private static int GetDefaultWarningLevel(DiagnosticSeverity severity)
    {
        if (severity == DiagnosticSeverity.Warning)
        {
            return 1;
        }

        return severity == DiagnosticSeverity.Error ? 0 : 4;
    }
}

public abstract class SonarReportingContextBase<TContext> : SonarAnalysisContextBase<TContext>
{
    private protected abstract ReportingContext CreateReportingContext(Diagnostic diagnostic);

    protected SonarReportingContextBase(SonarAnalysisContext analysisContext, TContext context) : base(analysisContext, context) { }

    private Diagnostic EnsureDiagnosticLocation(Diagnostic diagnostic)
    {
        var mappedLocation = diagnostic.Location.EnsureMappedLocation();

        if (!mappedLocation.GetMappedLineSpan().HasMappedPath)
        {
            return diagnostic;
        }

        return new SonarDiagnostic(diagnostic.Descriptor,
            mappedLocation,
            diagnostic.AdditionalLocations.Select(l => l.EnsureMappedLocation()).ToImmutableList(),
            diagnostic.GetMessage(),
            diagnostic.Properties);
    }

    protected void ReportIssueCore(Diagnostic diagnostic)
    {
        diagnostic = EnsureDiagnosticLocation(diagnostic);
        if (!diagnostic.Location.IsWithinGeneratedContent() && HasMatchingScope(diagnostic.Descriptor) && SonarAnalysisContext.LegacyIsRegisteredActionEnabled(diagnostic.Descriptor, diagnostic.Location?.SourceTree))
        {
            var reportingContext = CreateReportingContext(diagnostic);
            if (reportingContext is { Compilation: { } compilation, Diagnostic.Location: { Kind: LocationKind.SourceFile, SourceTree: { } tree } } && !compilation.ContainsSyntaxTree(tree))
            {
                Debug.Fail("Primary location should be part of the compilation. An AD0001 is raised if this is not the case.");
                return;
            }
            // This is the current way SonarLint will handle how and what to report.
            if (SonarAnalysisContext.ReportDiagnostic is not null)
            {
                Debug.Assert(SonarAnalysisContext.ShouldDiagnosticBeReported == null, "Not expecting SonarLint to set both the old and the new delegates.");
                SonarAnalysisContext.ReportDiagnostic(reportingContext);
                return;
            }
            // Standalone NuGet, Scanner run and SonarLint < 4.0 used with latest NuGet
            if (!VbcHelper.IsTriggeringVbcError(reportingContext.Diagnostic)
                && (SonarAnalysisContext.ShouldDiagnosticBeReported?.Invoke(reportingContext.SyntaxTree, reportingContext.Diagnostic) ?? true))
            {
                reportingContext.ReportDiagnostic(reportingContext.Diagnostic);
            }
        }
    }
}

/// <summary>
/// Base class for reporting contexts that are executed on a known Tree. The decisions about generated code and unchanged files are taken during action registration.
/// </summary>
public abstract class SonarTreeReportingContextBase<TContext> : SonarReportingContextBase<TContext>
{
    public abstract SyntaxTree Tree { get; }

    protected SonarTreeReportingContextBase(SonarAnalysisContext analysisContext, TContext context) : base(analysisContext, context) { }

    public void ReportIssue(Diagnostic diagnostic) =>
        ReportIssueCore(diagnostic);
}

/// <summary>
/// Base class for reporting contexts that are common for the entire compilation. Specific tree is not known before the action is executed.
/// </summary>
public abstract class SonarCompilationReportingContextBase<TContext> : SonarReportingContextBase<TContext>
{
    protected SonarCompilationReportingContextBase(SonarAnalysisContext analysisContext, TContext context) : base(analysisContext, context) { }

    public void ReportIssue(GeneratedCodeRecognizer generatedCodeRecognizer, Diagnostic diagnostic)
    {
        if (ShouldAnalyzeTree(diagnostic.Location.SourceTree, generatedCodeRecognizer))
        {
            ReportIssueCore(diagnostic);
        }
    }
}
