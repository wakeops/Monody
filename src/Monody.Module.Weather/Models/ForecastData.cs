namespace Monody.Module.Weather.Models;

public class ForecastData<T> where T : class
{
    public string TimeZone { get; set; }

    public T Data { get; set; }
}
