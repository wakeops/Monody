using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DarkSky.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monody.Services.Weather;

/// <summary>
/// Pirate Weather returns some fields DarkSkyCore models as integers (wind bearing, UV index)
/// as fractional numbers, which fails to deserialize. Truncate them before handing the JSON over.
/// </summary>
internal class DarkSkyJsonSerializerService : IJsonSerializerService
{
    private static readonly string[] _integerFields = ["windBearing", "uvIndex", "nearestStormBearing"];

    public async Task<T> DeserializeJsonAsync<T>(Task<string> json)
    {
        try
        {
            var jsonString = await json;

            return jsonString != null
                ? JsonConvert.DeserializeObject<T>(TruncateIntegerFields(jsonString))
                : default;
        }
        catch (JsonReaderException e)
        {
            throw new FormatException("Json Parsing Error", e);
        }
    }

    private static string TruncateIntegerFields(string json)
    {
        var root = JToken.Parse(json);

        foreach (var node in Blocks(root))
        {
            foreach (var field in _integerFields)
            {
                if (node[field] is JValue { Type: not (JTokenType.Null or JTokenType.Undefined) } value)
                {
                    node[field] = (int)value.Value<double>();
                }
            }
        }

        return root.ToString();
    }

    private static IEnumerable<JObject> Blocks(JToken root) =>
        new[] { root.SelectToken("currently") }
            .Concat(root.SelectTokens("daily.data[*]"))
            .Concat(root.SelectTokens("hourly.data[*]"))
            .OfType<JObject>();
}
