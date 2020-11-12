﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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

// ToDo: Add WPF support in unit tests when targeting .Net Core
// https://github.com/SonarSource/sonar-dotnet/issues/3425
#if NETFRAMEWORK

extern alias csharp;
using csharp::SonarAnalyzer.Rules.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ConstructorArgumentValueShouldExistTest
    {

        [TestMethod]
        [TestCategory("Rule")]
        public void ConstructorArgumentValueShouldExist_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\ConstructorArgumentValueShouldExist.cs",
                new ConstructorArgumentValueShouldExist(),
                additionalReferences: MetadataReferenceFacade.GetSystemXaml());

        [TestMethod]
        [TestCategory("Rule")]
        public void ConstructorArgumentValueShouldExist_CS_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\ConstructorArgumentValueShouldExist.CSharp9.cs",
                new ConstructorArgumentValueShouldExist(),
                MetadataReferenceFacade.GetSystemXaml());

        [TestMethod]
        [TestCategory("Rule")]
        public void ConstructorArgumentValueShouldExist_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\ConstructorArgumentValueShouldExist.vb",
                new SonarAnalyzer.Rules.VisualBasic.ConstructorArgumentValueShouldExist(),
                additionalReferences: MetadataReferenceFacade.GetSystemXaml());
    }
}
#endif
