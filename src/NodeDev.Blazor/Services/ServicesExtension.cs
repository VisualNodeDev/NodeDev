using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace NodeDev.Blazor.Services
{
	public static class ServicesExtension
	{

		public static IServiceCollection AddNodeDev(this IServiceCollection services)
		{
			services
				.AddMudServices()
				.AddScoped<DebuggedPathService>();

			return services;
		}

	}
}
