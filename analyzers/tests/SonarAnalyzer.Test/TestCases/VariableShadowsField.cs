﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Tests.Diagnostics
{
    public class VariableShadowsField
    {
        public int myField;
        public IDisposable myDisposableField;
        public int myField2;
        public object myField3;
        public int @int;
        public int MyField { get; set; }

        public void doSomething(object someParameter)
        {
            int myField = 0, // Noncompliant
            //  ^^^^^^^
                other = 5;
            int @int = 42; // Noncompliant

            for (myField = 0; myField < 10; myField++) // Compliant
            {
            }
            for (int myField2 = 0; myField2 < 10; myField2++) // Noncompliant {{Rename 'myField2' which hides the field with the same name.}}
            {
            }

            using (var myField2 = new MemoryStream()) // Noncompliant
            {
            }
            using (var local = new MemoryStream()) // Compliant
            {
            }
            using (myDisposableField = new MemoryStream()) // Compliant
            {
            }

            foreach (var myField2 in new[] { 1, 2 }) // Noncompliant
            {
            }
            foreach (var local in new[] { 1, 2 }) // Compliant
            {
            }

            if (someParameter is object myField3) // Noncompliant
            {

            }

            if (someParameter is object myFieldNonExistent) // Compliant
            {

            }
        }

        class X
        {
            public int f;
        }

        public unsafe void doSomeUnsafe()
        {
            var x = new X();

            fixed (int* myField2 = &x.f) // Noncompliant
            {
            }
            fixed (int* local = &x.f) // Compliant
            {
            }
        }

        public void doSomethingElse(int MyField) // Compliant
        {
            this.MyField = MyField;
        }

        public VariableShadowsField(int myField)
        {
            this.myField = myField;
        }

        public static VariableShadowsField build(int MyField)
        {
            return null;
        }
    }

    public class WithLocalFunctions
    {
        private int field;
        private int Property { get; set; }

        public void ShadowField()
        {
            void doSomething()
            {
                int field = 0; // Noncompliant
            }

            static void doMore()
            {
                int field = 0; // Noncompliant
            }
        }

        public void ShadowProperty()
        {
            void doSomething()
            {
                int Property = 0; // Noncompliant
            }

            static void doMore()
            {
                int Property = 0; // Noncompliant
            }
        }

        public void MethodWithLocalVar()
        {
            bool isUsed = true;

            void doSomething()
            {
                bool isUsed = true; // Compliant - currently the rule only looks at fields and properties
            }

            static void doMore()
            {
                bool isUsed = true; // Compliant
            }
        }
    }

    public class DeclarationExpressions
    {
        private object myField1 = null;

        public void OutDeclarationWithConcreteType()
        {
            OutParameter(out object myField1); // Noncompliant
        }

        public void OutDeclarationWithVar()
        {
            OutParameter(out var myField1);    // Noncompliant
        }

        public void OutReference()
        {
            OutParameter(out myField1);        // Compliant
        }

        public void OutParameter(out object parameter)
        {
            parameter = null;
        }
    }
}
