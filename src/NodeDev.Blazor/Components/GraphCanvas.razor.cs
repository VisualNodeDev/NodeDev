using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core;
using NodeDev.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NodeDev.Blazor.Components
{
	public partial class GraphCanvas : ComponentBase, IDisposable
	{
		[Inject]
		private IJSRuntime JS { get; set; } = null!;


		private readonly string Id = Guid.NewGuid().ToString();
		private DotNetObjectReference<GraphCanvas> Ref = null!;

		[Parameter, EditorRequired]
		public Graph Graph { get; set; } = null!;

		private string CanvasJS => $"Canvas_{Id}";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			if (firstRender)
			{
				Ref = DotNetObjectReference.Create(this);
				await JS.InvokeVoidAsync("InitializeCanvas", Ref, Id);

				await Graph.Invoke(InitializeCanvasWithGraphNodes);
			}

		}

		private ValueTask InvokeJSVoid(string name, params object?[]? parameters) => JS.InvokeVoidAsync($"{CanvasJS}.{name}", parameters);
		private ValueTask<T> InvokeJS<T>(string name, params object?[]? parameters) => JS.InvokeAsync<T>($"{CanvasJS}.{name}", parameters);

		[JSInvokable]
		public async Task OnSomethingHappened()
		{
			//await InvokeJSVoid("AddNode")
		}

		private async Task InitializeCanvasWithGraphNodes()
		{
			var infos = Graph.Nodes.Select(GetNodeCreationInfo).ToList();

			await InvokeJSVoid("AddNodes", infos);
		}

		private record class NodeCreationInfo(string Id, string Name, float X, float Y, List<NodeCreationInfo_Connection> Inputs, List<NodeCreationInfo_Connection> Outputs);
		private record class NodeCreationInfo_Connection(string Name);
		private NodeCreationInfo GetNodeCreationInfo(Node node)
		{
			var positionAttribute = node.GetOrAddAttribute<NodeDecorationPosition>(() => new(Vector2.Zero));

			return new(node.Id.ToString(),
				node.Name, 
				positionAttribute.X, 
				positionAttribute.Y, 
				node.Inputs.Select( x => new NodeCreationInfo_Connection(x.Name)).ToList(), 
				node.Outputs.Select(x => new NodeCreationInfo_Connection(x.Name)).ToList());
		}

		public void Dispose()
		{
			Ref?.Dispose();
			Ref = null!;
		}
	}
}
