using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace ConfigManager
{
	public static class ServiceCollectionConfigExtension
	{
		public static IServiceCollection AddFileConfig<T>(this IServiceCollection services, string path, bool saveOnDispose,
			FileStreamOptions? fileStreamOptions = null, JsonSerializerOptions? jsonSerializerOptions = null)
		where T : class, new()
		{
			return services.AddSingleton<IFileConfig<T>, FileConfig<T>>((isp) =>
			{
				var cfg = new FileConfig<T>(path, saveOnDispose, fileStreamOptions, jsonSerializerOptions);
				cfg.Read();
				return cfg;
			});
		}
	}
}
