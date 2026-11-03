using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Settings;

public static class SettingsServiceCollectionExtensions
{
   public static IServiceCollection AddAppOptions<T>(this IServiceCollection services, IConfiguration configuration)
      where T : class, IAppOptions
   {
      services.AddOptions<T>().Bind(configuration.GetSection(T.SectionName));
      services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<T>, AppOptionsValidator<T>>());
      return services;
   }
}
