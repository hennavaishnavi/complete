﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace SonarAnalyzer.SourceGenerators
{
    [Generator]
    [ExcludeFromCodeCoverage]
    public class RuleCatalogGenerator : ISourceGenerator
    {
        private const string SonarWayFileName = "Sonar_way_profile.json";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Not needed
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                throw new NotSupportedException("Cannot find ProjectDir");
            }
            var directorySuffix = Path.GetFileName(projectDir.TrimEnd(Path.DirectorySeparatorChar)) switch
            {
                "SonarAnalyzer.CSharp" => "cs",
                "SonarAnalyzer.VisualBasic" => "vbnet",
                _ => throw new ArgumentException($"Unexpected projectDir: {projectDir}")
            };
            var rspecDirectory = Path.Combine(projectDir, "..", "..", "rspec", directorySuffix);
            var sonarWay = ParseSonarWay(File.ReadAllText(Path.Combine(rspecDirectory, SonarWayFileName)));
            context.AddSource($"RuleCatalog.{directorySuffix}.g.cs", GenerateSource(RuleDescriptorArguments(rspecDirectory, sonarWay)));
        }

        private static IEnumerable<string[]> RuleDescriptorArguments(string rspecDirectory, ISet<string> sonarWay)
        {
            foreach (var jsonPath in Directory.GetFiles(rspecDirectory, "*.json").Where(x => Path.GetFileName(x) != SonarWayFileName))
            {
                var json = JObject.Parse(File.ReadAllText(jsonPath));
                var id = Path.GetFileName(jsonPath).Split('_').First();
                var html = File.ReadAllText(Path.ChangeExtension(jsonPath, ".html"));
                yield return new[]
                {
                    Encode(id),
                    Encode(json.Value<string>("title")),
                    Encode(json.Value<string>("type")),
                    Encode(json.Value<string>("defaultSeverity")),
                    $"SourceScope.{json.Value<string>("scope")}",
                    sonarWay.Contains(id).ToString().ToLower(),
                    Encode(FirstParagraphText(id, html))
                };
            }
        }

        private static string GenerateSource(IEnumerable<string[]> rulesArguments)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
        @"// <auto-generated/>

using System.Collections.Generic;
using SonarAnalyzer.Common;

namespace SonarAnalyzer
{
    internal static class RuleCatalog
    {
        public static Dictionary<string, RuleDescriptor> Rules { get; } = new()
        {");
            foreach (var arguments in rulesArguments)
            {
                sb.AppendLine($@"{{ {arguments[0]}, new({string.Join(", ", arguments)}) }},");
            }
            sb.AppendLine(
@"      };
    }
}");
            return sb.ToString();
        }

        private static string Encode(string value) =>
            $@"@""{value.Replace(@"""", @"""""")}""";

        private static string FirstParagraphText(string id, string html)
        {
            var match = Regex.Match(html, "<p>(?<Text>.*?)</p>", RegexOptions.Singleline);
            if (match.Success)
            {
                var text = match.Groups["Text"].Value;
                text = Regex.Replace(text, "<[^>]*>", string.Empty);
                text = text.Replace("\n", " ").Replace("\r", " ");
                text = Regex.Replace(text, @"\s{2,}", " ");
                return WebUtility.HtmlDecode(text);
            }
            else
            {
                throw new NotSupportedException($"Description of rule {id} does not contain any HTML <p>paragraphs</p>.");
            }
        }

        private static HashSet<string> ParseSonarWay(string json) =>
            new(JObject.Parse(json)["ruleKeys"].Values<string>());
    }
}
