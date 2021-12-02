﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.Helpers;
using StyleCop.Analyzers.Lightup;
using ProcessFunc = System.Func<SonarAnalyzer.SymbolicExecution.Roslyn.ProgramState, StyleCop.Analyzers.Lightup.IOperationWrapperSonar, SonarAnalyzer.SymbolicExecution.Roslyn.ProgramState>;

namespace SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution
{
    internal class CollectorTestCheck : SymbolicExecutionCheck
    {
        private readonly List<IOperationWrapperSonar> postProcessedOperations = new();

        public override ProgramState PostProcess(ProgramState state, IOperationWrapperSonar operation)
        {
            postProcessedOperations.Add(operation);
            return state;
        }

        public void ValidateOrder(params string[] expected) =>
            postProcessedOperations.Select(TestHelper.Serialize).Should().OnlyContainInOrder(expected);
    }

    internal class PreProcessTestCheck : SymbolicExecutionCheck
    {
        private readonly ProcessFunc preProcess;

        public PreProcessTestCheck(ProcessFunc preProcess) =>
            this.preProcess = preProcess;

        public override ProgramState PreProcess(ProgramState state, IOperationWrapperSonar operation) => preProcess(state, operation);
    }

    internal class PostProcessTestCheck : SymbolicExecutionCheck
    {
        private readonly ProcessFunc postProcess;

        public PostProcessTestCheck(ProcessFunc postProcess) =>
            this.postProcess = postProcess;

        public override ProgramState PostProcess(ProgramState state, IOperationWrapperSonar operation) => postProcess(state, operation);
    }
}