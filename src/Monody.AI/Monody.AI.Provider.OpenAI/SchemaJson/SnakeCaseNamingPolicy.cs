using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Monody.AI.Provider.OpenAI.SchemaJson;

internal class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder(name.Length + 8);
        var prevCategory = default(UnicodeCategory?);

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var category = char.GetUnicodeCategory(c);

            if (category == UnicodeCategory.UppercaseLetter)
            {
                if (i > 0 &&
                    prevCategory != UnicodeCategory.UppercaseLetter &&
                    prevCategory != UnicodeCategory.SpaceSeparator)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else if (category == UnicodeCategory.DecimalDigitNumber)
            {
                if (i > 0 && prevCategory != UnicodeCategory.DecimalDigitNumber)
                {
                    sb.Append('_');
                }

                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }

            prevCategory = category;
        }

        return sb.ToString();
    }
}
