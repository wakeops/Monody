using System;

namespace Monody.Module.Weather.Models;

public class WeatherAlert
{
    public DateTimeOffset IssuedDate { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Uri Uri { get; set; }
}
