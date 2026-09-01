using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Monody.Domain.Extensions;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates <typeparamref name="T"/> against its data annotations, returning the
    /// bound instance for use during registration.
    /// </summary>
    /// <remarks>
    /// Validation runs twice, deliberately. The eager pass covers the returned instance, which
    /// callers hand straight to SDK registration - without it those SDKs reject an empty key
    /// first, with a message that doesn't name the setting. The registered pass covers instances
    /// resolved later through <see cref="IOptions{T}"/>.
    /// </remarks>
    public static T ApplyValidatedOptions<T>(this IServiceCollection services, IConfiguration configuration, string configSectionPath)
        where T : class, new()
    {
        services.AddOptionsWithValidateOnStart<T>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations();

        // A section that is absent entirely binds to null; validate a default instance so the
        // failure names the missing settings rather than surfacing as a null reference.
        var options = configuration.GetSection(configSectionPath).Get<T>() ?? new T();

        Validate(options, configSectionPath);

        return options;
    }

    private static void Validate<T>(T options, string configSectionPath)
        where T : class
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true))
        {
            return;
        }

        var failures = results
            .Select(result => string.Join(", ", result.MemberNames) is { Length: > 0 } members
                ? $"{configSectionPath}:{members} - {result.ErrorMessage}"
                : $"{configSectionPath} - {result.ErrorMessage}")
            .ToList();

        throw new OptionsValidationException(configSectionPath, typeof(T), failures);
    }
}
