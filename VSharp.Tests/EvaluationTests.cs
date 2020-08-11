﻿//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Niklas Gransjøen
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using System;
using System.Collections.Generic;
using Xunit;

namespace VSharp.Tests
{
    public class EvaluationTests
    {
        [Theory]
        [MemberData(nameof(GetStatementsData))]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        #region Reports

        [Fact]
        public void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            string text = @"
                {
                    var x = 10;
                    var y = 100;
                    {
                        var x = 10;
                    }
                    var [x] = 5;
                }
            ";

            string diagnostics = @"
                Variable 'x' is already declared.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_Undefined()
        {
            string text = @"
                [x] = 10;
            ";

            string diagnostics = @"
                Variable 'x' does not exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_CannotConvert()
        {
            string text = @"
                {
                    var x = 10;
                    x = [true];
                }
            ";

            string diagnostics = @"
                Cannot convert type 'bool' to 'int'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Unary_Reports_UndefinedOperator()
        {
            string text = @"[+]true;";

            string diagnostics = @"
                Unary operator '+' is not defined for type 'bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Binary_Reports_UndefinedOperator()
        {
            string text = @"10 [*] true;";

            string diagnostics = @"
                Binary operator '*' is not defined for types 'int' and 'bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        #endregion Reports

        public static IEnumerable<object[]> GetStatementsData()
        {
            foreach ((string statement, object result) in GetStatements())
                yield return new object[] { statement, result };
        }

        private static IEnumerable<(string statement, object result)> GetStatements()
        {
            // Bool statements
            yield return ("true;", true);
            yield return ("false;", false);
            yield return ("!true;", false);
            yield return ("!false;", true);
            yield return ("true || true;", true);
            yield return ("true || false;", true);
            yield return ("false || true;", true);
            yield return ("false || false;", false);
            yield return ("true && true;", true);
            yield return ("true && false;", false);
            yield return ("false && true;", false);
            yield return ("false && false;", false);

            // Bool comparisons
            yield return ("false == false;", true);
            yield return ("true == false;", false);
            yield return ("false != false;", false);
            yield return ("true != false;", true);

            // Bool bitwise operations
            yield return ("false | false;", false);
            yield return ("false | true;", true);
            yield return ("true | false;", true);
            yield return ("true | true;", true);
            yield return ("false & false;", false);
            yield return ("false & true;", false);
            yield return ("true & false;", false);
            yield return ("true & true;", true);
            yield return ("false ^ false;", false);
            yield return ("false ^ true;", true);
            yield return ("true ^ false;", true);
            yield return ("true ^ true;", false);

            // Int statements
            yield return ("1;", 1);
            yield return ("+1;", 1);
            yield return ("-1;", -1);
            yield return ("~1;", ~1);
            yield return ("14 + 12;", 26);
            yield return ("12 - 3;", 9);
            yield return ("4 * 2;", 8);
            yield return ("9 / 3;", 3);
            yield return ("(10);", 10);

            // Int comparisons
            yield return ("12 == 3;", false);
            yield return ("3 == 3;", true);
            yield return ("12 != 3;", true);
            yield return ("3 != 3;", false);
            yield return ("3 < 4;", true);
            yield return ("5 < 4;", false);
            yield return ("4 <= 4;", true);
            yield return ("4 <= 5;", true);
            yield return ("5 <= 4;", false);
            yield return ("4 > 3;", true);
            yield return ("4 > 5;", false);
            yield return ("4 >= 4;", true);
            yield return ("5 >= 4;", true);
            yield return ("4 >= 5;", false);

            // Int bitwise operations
            yield return ("1 | 2;", 3);
            yield return ("1 | 0;", 1);
            yield return ("1 & 2;", 0);
            yield return ("1 & 1;", 1);
            yield return ("1 & 0;", 0);
            yield return ("1 ^ 0;", 1);
            yield return ("0 ^ 1;", 1);
            yield return ("1 ^ 3;", 2);

            // Int variable & assignment
            yield return ("{ var a = 0; (a = 10) * a; }", 100);
            yield return ("{ var a = 11; ++a; }", 12);
            yield return ("{ var a = 11; --a; }", 10);
            yield return ("{ var a = 11; a++; }", 11);
            yield return ("{ var a = 11; a--; }", 11);
            yield return ("{ var a = 11; ++a; a; }", 12);
            yield return ("{ var a = 11; --a; a; }", 10);
            yield return ("{ var a = 11; a++; a; }", 12);
            yield return ("{ var a = 11; a--; a; }", 10);
            yield return ("{ var a = 11; a += -1; }", 10);
            yield return ("{ var a = 11; a -= 1; }", 10);
            yield return ("{ var a = 10; a *= 2; }", 20);
            yield return ("{ var a = 10; a /= 2; }", 5);

            // Float statements
            yield return ("1f;", 1d);
            yield return ("+1f;", 1d);
            yield return ("-1f;", -1d);
            yield return ("14f + 12f;", 26d);
            yield return ("12f - 3f;", 9d);
            yield return ("4f * 2f;", 8d);
            yield return ("9f / 3f;", 3d);
            yield return ("(10f);", 10d);

            // Float comparisons
            yield return ("12f == 3f;", false);
            yield return ("3f == 3f;", true);
            yield return ("12f != 3f;", true);
            yield return ("3f != 3f;", false);
            yield return ("3f < 4f;", true);
            yield return ("5f < 4f;", false);
            yield return ("4f <= 4f;", true);
            yield return ("4f <= 5f;", true);
            yield return ("5f <= 4f;", false);
            yield return ("4f > 3f;", true);
            yield return ("4f > 5f;", false);
            yield return ("4f >= 4f;", true);
            yield return ("5f >= 4f;", true);
            yield return ("4f >= 5f;", false);

            // Float variable & assignment
            yield return ("{ var a = 0f; (a = 10f) * a; }", 100d);
            yield return ("{ var a = 11f; ++a; }", 12d);
            yield return ("{ var a = 11f; --a; }", 10d);
            yield return ("{ var a = 11f; a++; }", 11d);
            yield return ("{ var a = 11f; a--; }", 11d);
            yield return ("{ var a = 11f; ++a; a; }", 12d);
            yield return ("{ var a = 11f; --a; a; }", 10d);
            yield return ("{ var a = 11f; a++; a; }", 12d);
            yield return ("{ var a = 11f; a--; a; }", 10d);
            yield return ("{ var a = 11f; a += -1f; }", 10d);
            yield return ("{ var a = 11f; a -= 1f; }", 10d);
            yield return ("{ var a = 10f; a *= 2f; }", 20d);
            yield return ("{ var a = 10f; a /= 2f; }", 5d);

            // If-else-statement
            yield return ("{ var a = 0; if (a == 0) a = 10; a; }", 10);
            yield return ("{ var a = 4; if (a == 0) a = 10; a; }", 4);
            yield return ("{ var a = 0; if (a == 0) a = 10; else a = 34; a; }", 10);
            yield return ("{ var a = 4; if (a == 0) a = 10; else a = 32; a; }", 32);

            // While, for statement
            yield return ("{ var a = 0; while (a < 10) a = a + 1; a; }", 10);
            yield return ("{ var result = 0; for (var i = 0; i <= 10; ++i) result = result + i; result; }", 55);

            // Typeof
            yield return ("typeof(string);", TypeSymbol.String.Name);
            yield return ("typeof(int);", TypeSymbol.Int.Name);
            yield return ("typeof(bool);", TypeSymbol.Boolean.Name);

            // Nameof
            yield return ("{ var a = 0; nameof(a); }", "a");

            // Line comment
            yield return ("{ var a = 3; nameof(a); // gets the name of a\n }", "a");
            yield return ("{ var a = 5; //nameof(a); \n }", 5);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            Compilation compilation = new Compilation(syntaxTree);
            Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();
            EvaluationResult result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            AnnotatedText annotaedText = AnnotatedText.Parse(text);
            SyntaxTree syntaxTree = SyntaxTree.Parse(annotaedText.Text);
            Compilation compilation = new Compilation(syntaxTree);
            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            string[] expectedDiagnostics = AnnotatedText.UnintentLines(diagnosticText);

            if (annotaedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

            Assert.Equal(expectedDiagnostics.Length, result.Diagnostics.Length);

            for (int i = 0; i < expectedDiagnostics.Length; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = result.Diagnostics[i].Message;

                TextSpan expectedSpan = annotaedText.Spans[i];
                TextSpan actualSpan = result.Diagnostics[i].Span;

                Assert.Equal(expectedMessage, actualMessage);
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}