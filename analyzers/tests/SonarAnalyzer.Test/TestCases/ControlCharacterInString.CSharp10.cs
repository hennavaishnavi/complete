﻿using System;

namespace Tests.Diagnostics
{
    class Program
    {
        public const string Part1 = " ";      // Noncompliant {{Replace the control character at position 1 by its escape sequence '\0'.}}
        public const string Part2 = "Some valid string";
        public const string InterpolatedString = $"{Part1}{Part2}"; // The noncompliant control character is not reported here.
    }
}
