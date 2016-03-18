/*
 * SonarQube C# Plugin
 * Copyright (C) 2014-2016 SonarSource SA
 * mailto:contact AT sonarsource DOT com
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
package org.sonar.plugins.csharp;

import java.io.File;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;
import org.mockito.InOrder;
import org.mockito.Mockito;

import static org.mockito.Mockito.inOrder;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;

public class SarifParserTest {

  @Rule
  public ExpectedException thrown = ExpectedException.none();

  @Test
  public void non_existing() {
    thrown.expectMessage("Unable to read the Roslyn SARIF report file: ");
    thrown.expectMessage("non_existing.json");

    SarifParserCallback callback = mock(SarifParserCallback.class);
    new SarifParser(callback).parse(new File("src/test/resources/SarifParserTest/non_existing.json"));
  }

  @Test
  public void should_not_fail_ony_empty_report() {
    SarifParserCallback callback = mock(SarifParserCallback.class);
    new SarifParser(callback).parse(new File("src/test/resources/SarifParserTest/v0_1_empty.json"));
    verify(callback, Mockito.never()).onIssue(Mockito.anyString(), Mockito.anyString(), Mockito.anyString(), Mockito.anyInt());
  }

  // VS 2015 Update 1
  @Test
  public void sarif_version_0_1() {
    SarifParserCallback callback = mock(SarifParserCallback.class);
    new SarifParser(callback).parse(new File("src/test/resources/SarifParserTest/v0_1.json"));

    InOrder inOrder = inOrder(callback);

    inOrder.verify(callback).onIssue("S1172", "C:\\Foo.cs", "Remove this unused method parameter \"args\".", 42);
    inOrder.verify(callback).onIssue("CA1000", "C:\\Bar.cs", "There is just a full message.", 1);
    inOrder.verify(callback).onProjectIssue("AssemblyLevelRule", "This is an assembly level Roslyn issue with no location.");
    inOrder.verify(callback).onProjectIssue("NoAnalysisTargetsLocation", "No analysis targets, report at assembly level.");
    inOrder.verifyNoMoreInteractions();
  }

}
