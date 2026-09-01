using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Monody.AI.Tools.Capabilities.Calculator;

/// <summary>
/// A small recursive-descent evaluator for arithmetic expressions.
/// </summary>
/// <remarks>
/// Hand-written on purpose. The input comes from a model relaying whatever a user typed, so
/// anything that compiles or interprets general code is the wrong shape: this understands
/// numbers, the operators below, and a fixed list of functions, and nothing else parses at all.
/// </remarks>
public static class ExpressionEvaluator
{
    public const int MaxExpressionLength = 500;

    private static readonly Dictionary<string, double> _constants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pi"] = Math.PI,
        ["e"] = Math.E,
        ["tau"] = Math.Tau
    };

    private static readonly Dictionary<string, Func<double[], double>> _functions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["abs"] = a => Math.Abs(Single(a, "abs")),
        ["sqrt"] = a => Math.Sqrt(Single(a, "sqrt")),
        ["cbrt"] = a => Math.Cbrt(Single(a, "cbrt")),
        ["ln"] = a => Math.Log(Single(a, "ln")),
        ["log"] = a => a.Length == 2 ? Math.Log(a[0], a[1]) : Math.Log10(Single(a, "log")),
        ["log2"] = a => Math.Log2(Single(a, "log2")),
        ["exp"] = a => Math.Exp(Single(a, "exp")),
        ["floor"] = a => Math.Floor(Single(a, "floor")),
        ["ceil"] = a => Math.Ceiling(Single(a, "ceil")),
        ["round"] = a => a.Length == 2
            ? Math.Round(a[0], (int)a[1], MidpointRounding.AwayFromZero)
            : Math.Round(Single(a, "round"), MidpointRounding.AwayFromZero),
        ["sign"] = a => Math.Sign(Single(a, "sign")),
        ["min"] = Min,
        ["max"] = Max,
        ["sum"] = Sum,
        ["avg"] = a => Sum(a) / a.Length,
        ["sin"] = a => Math.Sin(Single(a, "sin")),
        ["cos"] = a => Math.Cos(Single(a, "cos")),
        ["tan"] = a => Math.Tan(Single(a, "tan")),
        ["asin"] = a => Math.Asin(Single(a, "asin")),
        ["acos"] = a => Math.Acos(Single(a, "acos")),
        ["atan"] = a => Math.Atan(Single(a, "atan")),
        ["pow"] = a => a.Length == 2 ? Math.Pow(a[0], a[1]) : throw new FormatException("pow takes two arguments.")
    };

    /// <summary>Evaluates <paramref name="expression"/>, throwing <see cref="FormatException"/> on bad input.</summary>
    public static double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new FormatException("There is no expression to evaluate.");
        }

        if (expression.Length > MaxExpressionLength)
        {
            throw new FormatException($"Expressions must be {MaxExpressionLength} characters or fewer.");
        }

        var parser = new Parser(expression);
        var value = parser.ParseExpression();
        parser.ExpectEnd();

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new FormatException("That does not have a finite answer.");
        }

        return value;
    }

    private static double Single(double[] args, string name) =>
        args.Length == 1 ? args[0] : throw new FormatException($"{name} takes one argument.");

    private static double Min(double[] args)
    {
        Require(args, "min");
        var result = args[0];
        foreach (var value in args)
        {
            result = Math.Min(result, value);
        }

        return result;
    }

    private static double Max(double[] args)
    {
        Require(args, "max");
        var result = args[0];
        foreach (var value in args)
        {
            result = Math.Max(result, value);
        }

        return result;
    }

    private static double Sum(double[] args)
    {
        Require(args, "sum");
        var total = 0d;
        foreach (var value in args)
        {
            total += value;
        }

        return total;
    }

    private static void Require(double[] args, string name)
    {
        if (args.Length == 0)
        {
            throw new FormatException($"{name} needs at least one argument.");
        }
    }

    /// <summary>
    /// Grammar, loosest binding first:
    /// expression := term (('+' | '-') term)*
    /// term       := unary (('*' | '/' | '%') unary)*
    /// unary      := ('+' | '-')* power
    /// power      := primary ('^' unary)?     - right associative, and binds tighter than unary
    ///                                          minus, so -2^2 is -(2^2) as in ordinary notation
    /// primary    := number | constant | function '(' args ')' | '(' expression ')'
    /// </summary>
    private sealed class Parser
    {
        // Bounds recursion so a pathological input fails cleanly instead of overflowing the stack.
        private const int MaxDepth = 64;

        private readonly string _text;
        private int _position;
        private int _depth;

        public Parser(string text) => _text = text;

        public double ParseExpression()
        {
            var value = ParseTerm();

            while (true)
            {
                SkipWhitespace();

                if (Match('+'))
                {
                    value += ParseTerm();
                }
                else if (Match('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        public void ExpectEnd()
        {
            SkipWhitespace();

            if (_position < _text.Length)
            {
                throw new FormatException($"Unexpected '{_text[_position]}' at position {_position + 1}.");
            }
        }

        private double ParseTerm()
        {
            var value = ParseUnary();

            while (true)
            {
                SkipWhitespace();

                if (Match('*'))
                {
                    value *= ParseUnary();
                }
                else if (Match('/'))
                {
                    var divisor = ParseUnary();
                    if (divisor == 0)
                    {
                        throw new FormatException("Division by zero.");
                    }

                    value /= divisor;
                }
                else if (Match('%'))
                {
                    var divisor = ParseUnary();
                    if (divisor == 0)
                    {
                        throw new FormatException("Division by zero.");
                    }

                    value %= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseUnary()
        {
            SkipWhitespace();

            if (Match('-'))
            {
                return -ParseUnary();
            }

            if (Match('+'))
            {
                return ParseUnary();
            }

            return ParsePower();
        }

        private double ParsePower()
        {
            var value = ParsePrimary();

            SkipWhitespace();

            // The right operand goes through unary so 2^-3 parses; recursing there also makes
            // the operator right associative, so 2^3^2 is 2^(3^2).
            return Match('^') ? Math.Pow(value, ParseUnary()) : value;
        }

        private double ParsePrimary()
        {
            SkipWhitespace();

            if (_position >= _text.Length)
            {
                throw new FormatException("The expression ends unexpectedly.");
            }

            if (Match('('))
            {
                var value = Nested(ParseExpression);
                Expect(')');
                return value;
            }

            var current = _text[_position];

            if (char.IsAsciiDigit(current) || current == '.')
            {
                return ParseNumber();
            }

            if (char.IsAsciiLetter(current))
            {
                return ParseIdentifier();
            }

            throw new FormatException($"Unexpected '{current}' at position {_position + 1}.");
        }

        private double ParseNumber()
        {
            var start = _position;

            while (_position < _text.Length && (char.IsAsciiDigit(_text[_position]) || _text[_position] == '.' || _text[_position] == '_'))
            {
                _position++;
            }

            // Scientific notation, e.g. 1.5e-3.
            if (_position < _text.Length && (_text[_position] == 'e' || _text[_position] == 'E') &&
                _position + 1 < _text.Length &&
                (char.IsAsciiDigit(_text[_position + 1]) || _text[_position + 1] is '+' or '-'))
            {
                _position += 2;
                while (_position < _text.Length && char.IsAsciiDigit(_text[_position]))
                {
                    _position++;
                }
            }

            var literal = _text[start.._position].Replace("_", "");

            return double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"'{literal}' is not a number.");
        }

        private double ParseIdentifier()
        {
            var start = _position;

            while (_position < _text.Length && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] == '_'))
            {
                _position++;
            }

            var name = _text[start.._position];

            SkipWhitespace();

            if (!Match('('))
            {
                return _constants.TryGetValue(name, out var constant)
                    ? constant
                    : throw new FormatException($"'{name}' is not a known constant or function.");
            }

            if (!_functions.TryGetValue(name, out var function))
            {
                throw new FormatException($"'{name}' is not a known function.");
            }

            var arguments = new List<double>();

            SkipWhitespace();
            if (!Match(')'))
            {
                do
                {
                    arguments.Add(Nested(ParseExpression));
                    SkipWhitespace();
                }
                while (Match(','));

                Expect(')');
            }

            return function([.. arguments]);
        }

        private double Nested(Func<double> parse)
        {
            if (++_depth > MaxDepth)
            {
                throw new FormatException("That expression is nested too deeply.");
            }

            try
            {
                return parse();
            }
            finally
            {
                _depth--;
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private bool Match(char expected)
        {
            if (_position < _text.Length && _text[_position] == expected)
            {
                _position++;
                return true;
            }

            return false;
        }

        private void Expect(char expected)
        {
            SkipWhitespace();

            if (!Match(expected))
            {
                throw new FormatException($"Expected '{expected}' at position {_position + 1}.");
            }
        }
    }

    /// <summary>Formats a result without exponent notation or trailing noise from binary floats.</summary>
    public static string Format(double value)
    {
        var rounded = Math.Round(value, 10, MidpointRounding.AwayFromZero);

        var text = rounded.ToString("0.##########", CultureInfo.InvariantCulture);

        return text == "-0" ? "0" : text;
    }

    internal static string DescribeSupported()
    {
        var builder = new StringBuilder("Operators + - * / % ^, parentheses. Functions: ");
        builder.AppendJoin(", ", _functions.Keys);
        builder.Append(". Constants: ");
        builder.AppendJoin(", ", _constants.Keys);
        builder.Append('.');

        return builder.ToString();
    }
}
