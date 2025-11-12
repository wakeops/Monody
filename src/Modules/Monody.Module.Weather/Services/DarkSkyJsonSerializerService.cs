using System;
using System.Linq;
using System.Threading.Tasks;
using DarkSky.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monody.Module.Weather.Services;

internal class DarkSkyJsonSerializerService : IJsonSerializerService
{
    private readonly JsonSerializerSettings _jsonSettings = new ();

    public async Task<T> DeserializeJsonAsync<T>(Task<string> json)
    {
        try
        {
            var jsonString = await json;
            jsonString = FixJsonTypeValues(jsonString);

            return (jsonString != null)
                ? JsonConvert.DeserializeObject<T>(jsonString, _jsonSettings)
                : default;
        }
        catch (JsonReaderException e)
        {
            throw new FormatException("Json Parsing Error", e);
        }
    }

    private static string FixJsonTypeValues(string json)
    {
        var jsonToken = JToken.Parse(json);

        var jobj = (JObject)jsonToken.SelectToken("currently");
        if (jobj != null)
        {
            jobj["windBearing"] = (int)(jobj["windBearing"].Value<double>());
            jobj["uvIndex"] = (int)(jobj["uvIndex"].Value<double>());
            jobj["nearestStormBearing"] = (int)(jobj["nearestStormBearing"].Value<double>());
        }

        var dailyObj = jsonToken.SelectTokens("daily.data[*]");
        dailyObj?.Cast<JObject>().ToList().ForEach(a =>
            {
                a["windBearing"] = (int)(a["windBearing"].Value<double>());
                a["uvIndex"] = (int)(a["uvIndex"].Value<double>());
            });

        var hourlyData = jsonToken.SelectTokens("hourly.data[*]");
        hourlyData?.Cast<JObject>().ToList().ForEach(a =>
            {
                a["windBearing"] = (int)(a["windBearing"].Value<double>());
                a["uvIndex"] = (int)(a["uvIndex"].Value<double>());
            });

        return jsonToken.ToString();
    }
}
