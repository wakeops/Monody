using System;
using Monody.AI.Tools.Capabilities.Calculator;
using Xunit;

namespace Monody.AI.Tools.Tests;

public class ExpressionEvaluatorTests
{
    [Theory]
    [InlineData("1 + 1", "2")]
    [InlineData("2 - 3", "-1")]
    [InlineData("6 * 7", "42")]
    [InlineData("10 / 4", "2.5")]
    [InlineData("10 % 3", "1")]
    [InlineData("1234 * 5.5 / 3", "2262.3333333333")]
    [InlineData("1_000_000 / 4", "250000")]
    [InlineData("1.5e3", "1500")]
    [InlineData("2e-3", "0.002")]
    public void EvaluatesArithmetic(string expression, string expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.Format(ExpressionEvaluator.Evaluate(expression)));
    }

    [Theory]
    // Precedence and associativity are the whole point of a parser; assert them explicitly.
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("2 ^ 3 ^ 2", 512)]      // right associative, not 64
    [InlineData("10 - 3 - 2", 5)]       // left associative
    [InlineData("-2 ^ 2", -4)]          // unary binds looser than ^, as in ordinary notation
    [InlineData("2 ^ -3", 0.125)]       // but a negative exponent still parses
    [InlineData("-2 ^ 3", -8)]
    [InlineData("--5", 5)]
    [InlineData("-(3 + 4)", -7)]
    public void RespectsPrecedenceAndAssociativity(string expression, double expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.Evaluate(expression), 10);
    }

    [Theory]
    [InlineData("sqrt(16)", 4)]
    [InlineData("abs(-7.5)", 7.5)]
    [InlineData("round(2.5)", 3)]           // away from zero, not banker's rounding
    [InlineData("round(3.14159, 2)", 3.14)]
    [InlineData("min(4, 2, 9)", 2)]
    [InlineData("max(4, 2, 9)", 9)]
    [InlineData("sum(1, 2, 3, 4)", 10)]
    [InlineData("avg(2, 4, 6)", 4)]
    [InlineData("log(1000)", 3)]
    [InlineData("log(8, 2)", 3)]
    [InlineData("pow(2, 10)", 1024)]
    [InlineData("floor(-1.5)", -2)]
    [InlineData("ceil(-1.5)", -1)]
    [InlineData("sqrt(max(9, 4)) + min(1, 2)", 4)]
    public void EvaluatesFunctions(string expression, double expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.Evaluate(expression), 10);
    }

    [Theory]
    [InlineData("pi", System.Math.PI)]
    [InlineData("2 * pi", 2 * System.Math.PI)]
    [InlineData("e", System.Math.E)]
    public void KnowsConstants(string expression, double expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.Evaluate(expression), 10);
    }

    [Theory]
    [InlineData("1 / 0", "Division by zero")]
    [InlineData("10 % 0", "Division by zero")]
    [InlineData("2 +", "ends unexpectedly")]
    [InlineData("(1 + 2", "Expected ')'")]
    [InlineData("1 + 2)", "Unexpected ')'")]
    [InlineData("nope(2)", "not a known function")]
    [InlineData("someConstant", "not a known constant")]
    [InlineData("sqrt(1, 2)", "takes one argument")]
    [InlineData("", "no expression")]
    public void ExplainsBadInput(string expression, string expectedFragment)
    {
        var ex = Assert.Throws<FormatException>(() => ExpressionEvaluator.Evaluate(expression));

        Assert.Contains(expectedFragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    // Nothing that looks like code should parse. These are rejected by the grammar, not a filter.
    [InlineData("System.Environment.Exit(0)")]
    [InlineData("1; DROP TABLE UserMemories")]
    [InlineData("$(rm -rf /)")]
    [InlineData("__import__('os')")]
    [InlineData("{{7*7}}")]
    public void RefusesAnythingThatIsNotArithmetic(string expression)
    {
        Assert.Throws<FormatException>(() => ExpressionEvaluator.Evaluate(expression));
    }

    [Fact]
    public void RefusesResultsThatAreNotFinite()
    {
        Assert.Throws<FormatException>(() => ExpressionEvaluator.Evaluate("sqrt(-1)"));
    }

    [Fact]
    public void RefusesDeeplyNestedInput()
    {
        // Would otherwise recurse until the stack gives out.
        var nested = new string('(', 200) + "1" + new string(')', 200);

        Assert.Throws<FormatException>(() => ExpressionEvaluator.Evaluate(nested));
    }

    [Fact]
    public void RefusesOverlyLongInput()
    {
        var long_ = string.Join(" + ", new string[ExpressionEvaluator.MaxExpressionLength]).Replace(" +  + ", " + 1 + ");

        Assert.Throws<FormatException>(() => ExpressionEvaluator.Evaluate(long_));
    }

    [Theory]
    [InlineData(2.0, "2")]
    [InlineData(2.5, "2.5")]
    [InlineData(0.1 + 0.2, "0.3")]      // the classic float artefact should not reach the user
    [InlineData(-0.0, "0")]
    [InlineData(1234567.0, "1234567")]
    public void FormatsResultsCleanly(double value, string expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.Format(value));
    }
}
