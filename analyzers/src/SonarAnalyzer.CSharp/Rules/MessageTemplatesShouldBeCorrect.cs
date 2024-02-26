/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

using System.Text;
using System.Text.RegularExpressions;
using static Roslyn.Utilities.SonarAnalyzer.Shared.LoggingFrameworkMethods;

namespace SonarAnalyzer.Rules.CSharp;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageTemplatesShouldBeCorrect : SonarDiagnosticAnalyzer
{
    private const string DiagnosticId = "S6674";
    private const string MessageFormat = "Log message template should be syntactically correct";

    private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(c =>
            {
                var invocation = (InvocationExpressionSyntax)c.Node;
                if (TemplateArgument(invocation, c.SemanticModel) is { } argument
                    && GetErrorMessage(argument.Expression.ToString()) is { } errorMessage)
                {
                    c.ReportIssue(Diagnostic.Create(Rule, invocation.GetLocation()));
                }
            },
            SyntaxKind.InvocationExpression);

    record ErrorMessage(string template, int reportFrom, int length);

    private static ErrorMessage GetErrorMessage(string template)
    {
        // unbalanced parentheses
        var openBrackets = template.Count(c => c == '{');
        var closeBrackets = template.Count(c => c == '}');

        if (openBrackets != closeBrackets)
        {
            var reportFrom = FirstIndex('{');
            return new ErrorMessage(template, );
        }

        // Hey {User:       Missing closing bracket
        // Hey {{User}:     Opening bracket is escaped
        // Hey {&User}:     Only '@' and '$' are allowed as prefix
        // Hey {User-Name}: Only chars, numbers, and underscore are allowed for placeholders
        // Hey {User,r}:    The alignment specifier must be numeric
        // Hey {User:}:     Empty format specifier is not allowed
        return "Missing closing bracket";

        int FirstIndex(char target) => Math.Max(template.IndexOf(target), 0);
        int LastIndex(char target) => Math.Max(template.IndexOf(target), template.Length);
    }

    private static ArgumentSyntax TemplateArgument(InvocationExpressionSyntax invocation, SemanticModel model) =>
        TemplateArgument(invocation, model, KnownType.Microsoft_Extensions_Logging_LoggerExtensions, MicrosoftExtensionsLogging, "message")
        ?? TemplateArgument(invocation, model, KnownType.Serilog_Log, Serilog, "messageTemplate")
        ?? TemplateArgument(invocation, model, KnownType.Serilog_ILogger, Serilog, "messageTemplate")
        ?? TemplateArgument(invocation, model, KnownType.NLog_ILoggerExtensions, NLogLoggingMethods, "message")
        ?? TemplateArgument(invocation, model, KnownType.NLog_ILogger, NLogLoggingMethods, "message", checkDerivedTypes: true)
        ?? TemplateArgument(invocation, model, KnownType.NLog_ILoggerBase, NLogILoggerBase, "message", checkDerivedTypes: true);

    private static ArgumentSyntax TemplateArgument(InvocationExpressionSyntax invocation,
                                                   SemanticModel model,
                                                   KnownType type,
                                                   ICollection<string> methods,
                                                   string template,
                                                   bool checkDerivedTypes = false) =>
        methods.Contains(invocation.GetIdentifier().ToString())
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
        && method.HasContainingType(type, checkDerivedTypes)
        && CSharpFacade.Instance.MethodParameterLookup(invocation, method) is { } lookup
        && lookup.TryGetSyntax(template, out var argumentsFound) // Fetch Argument.Expression with IParameterSymbol.Name == templateName
        && argumentsFound.Length == 1
            ? (ArgumentSyntax)argumentsFound[0].Parent
            : null;

}

class ParenthesesCalculator
{
    record ErrorMessage(string template, int reportFrom, int length);

    private static ErrorMessage CalculateParentheses(string template)
    {
        var unbalancedLocations = new List<int>();

        var currentStart = -1;
        var refCount = 0;

        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '{')
            {
                refCount++;
                if (currentStart < 0)
                {
                    currentStart = i;
                }
            }
            else if (template[i] == '}')
            {
                refCount--;
            }
        }

        return null;
    }

    private static ErrorMessage GetErrorMessage(string template)
    {
        // unbalanced parentheses
        var openBrackets = template.Count(c => c == '{');
        var closeBrackets = template.Count(c => c == '}');

        if (openBrackets != closeBrackets)
        {
            var reportFrom = FirstIndex('{');
            return new ErrorMessage(template, );
        }

        return null;

        int FirstIndex(char target) => Math.Max(template.IndexOf(target), 0);
        int LastIndex(char target) => Math.Max(template.IndexOf(target), template.Length);
    }

    private static Errors ParsePlaceholder(string placeholder)
    {
        if (placeholder.Length == 0)
        {
            return Errors.Empty;
        }

        var parts = GetParts(placeholder);
        // TODO: Use safeismatch
        if (!Match(parts.Name, "^[0-9a-zA-Z_]+$"))
        {
            return Errors.BadName;
        }
        else if (!Match(parts.Alignment, "^-?[0-9]+$"))
        {
            return Errors.BadAlignment;
        }
        else if (!Match(parts.Format, @"^[^\}]+$"))
        {
            return Errors.BadFormat;
        }

        return Errors.AllGood;

        bool Match(string pattern, string input) =>
            Regex.IsMatch(input, pattern, RegexOptions.None, RegexConstants.DefaultTimeout);
    }

    private static Parts GetParts(string placeholder)
    {
        var start = placeholder[0] is '@' or '$' ? 1 : 0; // skip prefix

        var name = string.Empty;
        var alignment = string.Empty;
        var format = string.Empty;

        var formatIndex = placeholder.IndexOf(':');
        var alignmentIndex = placeholder.IndexOf(',');
        if (formatIndex >= 0 && alignmentIndex > formatIndex)
        {
            // weird case where there is no alignment and format contains ','
            alignmentIndex = -1;
        }

        // User,42:20
        if (NotFound(alignmentIndex))
        {
            if (NotFound(formatIndex))
            {
                name = placeholder.Substring(start);
            }
            else
            {
                name = placeholder.Substring(start, formatIndex - start);
                format = IsEmpty(formatIndex)
                    ? string.Empty
                    : placeholder.Substring(formatIndex + 1);
            }
        }
        else
        {
            if (NotFound(formatIndex))
            {
                name = placeholder.Substring(start, alignmentIndex - start);
                alignment = IsEmpty(alignmentIndex)
                    ? string.Empty
                    : placeholder.Substring(alignmentIndex + 1);
            }
            else
            {
                name = placeholder.Substring(start, alignmentIndex - start);
                alignment = placeholder.Substring(alignmentIndex + 1, formatIndex - alignmentIndex - 1); // TODO: check boundaries here+next line, for {User,:}
                format = placeholder.Substring(formatIndex + 1);
            }
        }

        return new Parts(name, alignment, format);

        bool NotFound(int index) => index == -1;
        bool IsEmpty(int index) => index == placeholder.Length - 1;
    }

    record Parts(string Name, string Alignment, string Format);

    public enum Errors
    {
        AllGood,
        // Hey {User:       Missing closing bracket
        // Hey {{User}:     Opening bracket is escaped
        UnbalancedBrackets,
        // Hey {} :         Empty placeholder
        Empty,
        // Hey {User-Name}: Only chars, numbers, and underscore are allowed for placeholders
        BadName,
        // Hey {User,r}:    The alignment specifier must be numeric
        BadAlignment,
        // Hey {User:}:     Empty format specifier is not allowed
        BadFormat,
    }
}
