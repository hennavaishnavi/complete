﻿/*
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

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class MessageTemplatesShouldBeCorrectTest
{
    private static readonly IEnumerable<MetadataReference> LoggingReferences =
        NuGetMetadataReference.MicrosoftExtensionsLoggingAbstractions()
        .Concat(NuGetMetadataReference.NLog(Constants.NuGetLatestVersion))
        .Concat(NuGetMetadataReference.Serilog(Constants.NuGetLatestVersion));

    private static readonly VerifierBuilder Builder = new VerifierBuilder<MessageTemplatesShouldBeCorrect>()
        .AddReferences(LoggingReferences);

    [TestMethod]
    public void MessageTemplatesShouldBeCorrect_CS() =>
        Builder.AddPaths("MessageTemplatesShouldBeCorrect.cs").Verify();

    [DataTestMethod]
    [DataRow("LogCritical")]
    [DataRow("LogDebug")]
    [DataRow("LogError")]
    [DataRow("LogInformation")]
    [DataRow("LogTrace")]
    [DataRow("LogWarning")]
    public void MessageTemplatesShouldBeCorrect_MicrosoftExtensionsLogging_CS(string methodName) =>
        Builder.AddSnippet($$$"""
            using System;
            using Microsoft.Extensions.Logging;

            public class Program
            {
                public void Method(ILogger logger, string user, int cnt)
                {
                    Console.WriteLine("Login failed for {User", user);                  // Compliant
                    logger.{{{methodName}}}("Login failed for {User}", user);           // Compliant

                    logger.{{{methodName}}}("Login failed for {User", user);            // Noncompliant: Missing closing bracket
                    logger.{{{methodName}}}("Login failed for {{User}", user);          // Noncompliant: Opening bracket is escaped
                    logger.{{{methodName}}}("Login failed for {&User}", user);      // Noncompliant: Only '@' and '$' are allowed as prefix
                    logger.{{{methodName}}}("Login failed for {User-Name}", user);      // Noncompliant: Only chars, numbers, and underscore are allowed for placeholders
                    logger.{{{methodName}}}("Retry attempt {Cnt,r}", cnt);              // Noncompliant: The alignment specifier must be numeric
                    logger.{{{methodName}}}("Retry attempt {Cnt:}", cnt);               // Noncompliant: Empty format specifier is not allowed
                }
            }
            """).Verify();

    [DataTestMethod]
    [DataRow("Debug")]
    [DataRow("Error")]
    [DataRow("Information")]
    [DataRow("Fatal")]
    [DataRow("Warning")]
    [DataRow("Verbose")]
    public void MessageTemplatesShouldBeCorrect_Serilog_CS(string methodName) =>
        Builder.AddSnippet($$"""
            using Serilog;
            using Serilog.Events;

            public class Program
            {
                public void Method(ILogger logger, int arg)
                {
                    logger.{{methodName}}("Arg: {Arg}", arg);       // Compliant
                    logger.{{methodName}}("Arg: {arg}", arg);       // Noncompliant
                }
            }
            """).Verify();

    [DataTestMethod]
    [DataRow("Debug")]
    [DataRow("ConditionalDebug")]
    [DataRow("Error")]
    [DataRow("Fatal")]
    [DataRow("Info")]
    [DataRow("Trace")]
    [DataRow("ConditionalTrace")]
    [DataRow("Warn")]
    public void MessageTemplatesShouldBeCorrect_NLog_CS(string methodName) =>
        Builder.AddSnippet($$"""
            using NLog;

            public class Program
            {
                public void Method(ILogger iLogger, Logger logger, MyLogger myLogger, int arg)
                {
                    logger.{{methodName}}("Arg: {Arg}", arg);       // Compliant
                    logger.{{methodName}}("Arg: {arg}", arg);       // Noncompliant
                }
            }
            public class MyLogger : Logger { }
            """).Verify();
}
