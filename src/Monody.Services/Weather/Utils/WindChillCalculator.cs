using System;

namespace Monody.Services.Weather.Utils;

internal static class WindChillCalculator
{
    private const double _wcc1 = 35.74;
    private const double _wcc2 = 0.6215;
    private const double _wcc3 = 33.75;
    private const double _wcc4 = 0.4275;

    public static double Calculate(double temperature, double windSpeed)
    {
        var ws = Math.Pow(windSpeed, 0.16);
        return _wcc1 + (_wcc2 * temperature) - (_wcc3 * ws) + (_wcc4 * temperature * ws);
    }
}
