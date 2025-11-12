using System;

namespace Monody.Module.Weather.Utils;

internal static class HeatIndexCalculator
{
    private const double _hic1 = -42.379;
    private const double _hic2 = 2.04901523;
    private const double _hic3 = 10.14333127;
    private const double _hic4 = -0.22475541;
    private const double _hic5 = -0.00683783;
    private const double _hic6 = -0.05481717;
    private const double _hic7 = 0.00122874;
    private const double _hic8 = 0.00085282;
    private const double _hic9 = -0.00000199;

    public static double Calculate(double temperature, double humidity)
    {
        var t = temperature;
        var r = humidity;

        var heatIndex = 0.5 * (t + 61.0 + ((t - 68.0) * 1.2) + (r * 0.094));

        if (heatIndex < 80)
        {
            return heatIndex;
        }

        heatIndex =
            _hic1 +
            _hic2 * t +
            _hic3 * r +
            _hic4 * t * r +
            _hic5 * t * t +
            _hic6 * r * r +
            _hic7 * t * t * r +
            _hic8 * t * r * r +
            _hic9 * t * t * r * r;

        if (r < 13 && t >= 80 && t <= 112)
        {
            return heatIndex - ((13 - r) / 4) * Math.Sqrt((17 - Math.Abs(t - 95)) / 17);
        }

        if (r > 85 && t >= 80 && t <= 87)
        {
            return heatIndex + ((r - 85) / 10) * ((87 - t) / 5);
        }

        return heatIndex;
    }
}