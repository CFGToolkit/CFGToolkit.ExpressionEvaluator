using System.Collections.Generic;
using Xunit;
using System;

namespace CFGToolkit.ExpressionEvaluator.Tests
{
    public class BasicTests
    {
        private static Dictionary<string, object> _vars;

        static BasicTests()
        {
            _vars = new Dictionary<string, object>()
            {
                { "x", 1 },
                { "date1", new DateTime(2000, 1, 2) },
                { "date2", new DateTime(2000, 1, 4) }
            };
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Parse(string expression, object value)
        {
            Assert.Equal(value, Evaluator.Eval(expression, _vars, ExpressionParser.ExpressionLanguage.C));
        }

        public static IEnumerable<object[]> Data => new List<object[]>
        {
                new object[] { @"1+1", 2.0 },
                new object[] { @"+1+1", 2.0 },
                new object[] { @"-1+1", 0.0 },
                new object[] { @"2*3+1", 7.0 },
                new object[] { @"5-3*5", -10.0 },
                new object[] { @"(5-3)*5", 10.0 },
                new object[] { @"(5-3)*5 + 1 * 3", 13.0 },
                new object[] { @"date1", _vars["date1"] },
                new object[] { @"date2", _vars["date2"] },
                new object[] { @"date1 < date2", true },
                new object[] { @"1<1", false },
                new object[] { @"(1<1) && (3 > 5)", false },
                new object[] { @"(5<7) && (6 > 5)", true },
                new object[] { @"(5<7) && (6 < 5)", false },
                new object[] { @"true", true },
                new object[] { @"false", false },
        };
    }
}