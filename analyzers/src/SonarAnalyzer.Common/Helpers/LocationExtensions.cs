﻿/*
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

namespace SonarAnalyzer.Helpers
{
    public static class LocationExtensions
    {
        public static Location EnsureMappedLocation(this Location location)
        {
            if (location is null || !GeneratedCodeRecognizer.IsRazorGeneratedFile(location.SourceTree))
            {
                return location;
            }

            var lineSpan = location.GetMappedLineSpan();

            return Location.Create(lineSpan.Path, location.SourceSpan, lineSpan.Span);
        }

        public static bool IsWithinGeneratedContent(this Location location)
            => location is not null && GeneratedCodeRecognizer.HasGeneratedFileName(location.SourceTree);
    }
}
