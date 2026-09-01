using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace Monody.AI.Tools.Capabilities.Calculator;

/// <summary>
/// Arithmetic the model should not be doing in its head.
/// </summary>
public sealed class MathPlugin
{
    [KernelFunction("calculate")]
    [Description(
        "Evaluates an arithmetic expression exactly. Use this for any calculation rather than " +
        "working it out yourself, however simple it looks. " +
        "Operators + - * / % ^ and parentheses; functions include sqrt, abs, round, floor, ceil, " +
        "min, max, sum, avg, log, ln, exp and the trig functions; constants pi, e, tau.")]
    public Task<CalculateToolResponse> CalculateAsync(CalculateToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Expression);

        try
        {
            var value = ExpressionEvaluator.Evaluate(request.Expression);

            return Task.FromResult(new CalculateToolResponse
            {
                Result = ExpressionEvaluator.Format(value),
                Expression = request.Expression.Trim()
            });
        }
        catch (FormatException ex)
        {
            // Returned rather than thrown: the model can relay the reason or fix the expression
            // and retry, which is more useful than a failed tool call.
            return Task.FromResult(new CalculateToolResponse
            {
                Expression = request.Expression.Trim(),
                Error = $"{ex.Message} {ExpressionEvaluator.DescribeSupported()}"
            });
        }
    }
}

public sealed class CalculateToolRequest
{
    [Description("The expression to evaluate, e.g. '(1234 * 5.5) / 3' or 'round(sqrt(2), 4)'.")]
    [Required]
    [MaxLength(ExpressionEvaluator.MaxExpressionLength)]
    public string Expression { get; set; }
}

public sealed class CalculateToolResponse
{
    [Description("The expression that was evaluated.")]
    public string Expression { get; set; }

    [Description("The exact result. Empty when Error is set.")]
    public string Result { get; set; }

    [Description("Why the expression could not be evaluated. Empty on success.")]
    public string Error { get; set; }
}
