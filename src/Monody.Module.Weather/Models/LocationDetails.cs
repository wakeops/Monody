namespace Monody.Module.Weather.Models;

public class LocationDetails
{
    public Coordinates Coordinates { get; set; }
    public string Country { get; set; }
    public string Region { get; set; }
    public string City { get; set; }
}

public class Coordinates
{
    public double Latitude { get; set;}
    public double Longitude { get; set;}
}
