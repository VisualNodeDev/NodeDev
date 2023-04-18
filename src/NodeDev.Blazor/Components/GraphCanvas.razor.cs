using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NodeDev.Blazor.Components
{
	public partial class GraphCanvas : ComponentBase, IDisposable
	{
		[Inject]
		private IJSRuntime JS { get; set; } = null!;


		private string Id = Guid.NewGuid().ToString();
		private DotNetObjectReference<GraphCanvas> Ref = null!;

		private string CanvasJS => $"Canvas_{Id}";

		protected override void OnInitialized()
		{
			base.OnInitialized();

			Ref = DotNetObjectReference.Create(this);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			if (firstRender)
				await JS.InvokeVoidAsync("InitializeCanvas", Ref, Id);

		}

		[JSInvokable]
		public async Task OnSomethingHappened()
		{
			await JS.InvokeVoidAsync($"{CanvasJS}.AddNode");
		}

		public void Dispose()
		{
			Ref?.Dispose();
			Ref = null!;
		}
	}
}
