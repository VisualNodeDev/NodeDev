using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace NodeDev.Blazor.Services
{
	public static class ServicesExtension
	{

		/// <summary>
		/// Registers the UI services with lifetimes appropriate for an individual Blazor circuit.
		/// </summary>
		/// <param name="services">The collection receiving the application service registrations.</param>
		/// <returns>The supplied service collection for further composition.</returns>
		public static IServiceCollection AddNodeDev(this IServiceCollection services)
		{
			services
				.AddMudServices()
				.AddScoped<DebuggedPathService>()
				.AddScoped<SourceGenerationService>()
				.AddScoped<ProjectService>()
				.AddScoped<WorkspaceCommandService>()
				.AddSingleton(new AppOptionsContainer("AppOptions.json"));

			return services;
		}

	}
}
