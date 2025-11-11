using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Monody.Domain.Extensions;

public static class IConfigurationExtensions
{
    public static T GetRequiredOptions<T>(this IConfiguration configuration, string sectionName)
        where T : class
    {
        var opt = configuration.GetSection(sectionName).Get<T>() ?? default;

        var validationContext = new ValidationContext(opt);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(opt, validationContext, validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var failures = validationResults
                .SelectMany(r => (r.MemberNames?.Any() ?? false)
                    ? r.MemberNames.Select(m => $"{m}: {r.ErrorMessage}")
                    : [r.ErrorMessage])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            throw new OptionsValidationException(sectionName, typeof(T), failures);
        }

        return opt;
    }
}
