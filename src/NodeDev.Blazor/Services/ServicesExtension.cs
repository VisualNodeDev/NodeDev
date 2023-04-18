using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor.Services
{
	public static class ServicesExtension
	{

		public static IServiceCollection AddNodeDev(this IServiceCollection services)
		{
			services.AddMudServices();

			return services;
		}

	}
}
