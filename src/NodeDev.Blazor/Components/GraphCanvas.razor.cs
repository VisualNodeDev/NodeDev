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

		private List<Node> SelectedNodes { get; } = new();

		private ValueTask InvokeJSVoid(string name, params object?[]? parameters) => JS.InvokeVoidAsync($"{CanvasJS}.{name}", parameters);
		private ValueTask<T> InvokeJS<T>(string name, params object?[]? parameters) => JS.InvokeAsync<T>($"{CanvasJS}.{name}", parameters);

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


		#region Events from client

		#region Node Selected / unselected

		[JSInvokable]
		public void OnNodeSelectedInClient(string nodeId)
		{
			if (Graph.Nodes.TryGetValue(nodeId, out var node))
				SelectedNodes.Add(node);
		}

		[JSInvokable]
		public void OnNodeUnselectedInClient(string nodeId)
		{
			if (Graph.Nodes.TryGetValue(nodeId, out var node))
				SelectedNodes.Remove(node);
		}

		#endregion

		#region Connection Added / Removed

		[JSInvokable]
		public void OnConnectionAdded(string nodeSourceId, string outputId, string nodeDestinationID, string inputId)
		{
			Graph.Invoke(() =>
			{
				if (Graph.Nodes.TryGetValue(nodeSourceId, out var nodeSource) && Graph.Nodes.TryGetValue(nodeDestinationID, out var nodeDestination))
				{
					var source = nodeSource.Outputs.FirstOrDefault(x => x.Id == outputId);
					var destination = nodeDestination.Inputs.FirstOrDefault(x => x.Id == inputId);

					if (source != null && destination != null)
					{
						source.Connections.Add(destination);
						destination.Connections.Add(source);
					}
				}
			});
		}

		[JSInvokable]
		public void OnConnectionRemoved(string nodeSourceId, string outputId, string nodeDestinationID, string inputId)
		{
			Graph.Invoke(() =>
			{
				if (Graph.Nodes.TryGetValue(nodeSourceId, out var nodeSource) && Graph.Nodes.TryGetValue(nodeDestinationID, out var nodeDestination))
				{
					var source = nodeSource.Outputs.FirstOrDefault(x => x.Id == outputId);
					var destination = nodeDestination.Inputs.FirstOrDefault(x => x.Id == inputId);

					if (source != null && destination != null)
					{
						source.Connections.Remove(destination);
						destination.Connections.Remove(source);
					}
				}
			});
		}

		#endregion

		#region Node Moved

		[JSInvokable]
		public void OnNodeMoved(string nodeId, int x, int y)
		{
			if (!Graph.Nodes.TryGetValue(nodeId, out var node))
				return;
			var decoration = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
			decoration.Position = new(x, y);
		}

		#endregion

		#region OnPortDroppedOnCanvas

		[JSInvokable]
		public void OnPortDroppedOnCanvas(string nodeId, string connectionId, int x, int y)
		{
			if (!Graph.Nodes.TryGetValue(nodeId, out var node))
				return;
			
			var connection = node.Inputs.Concat(node.Outputs).FirstOrDefault( x=> x.Id == connectionId);
			if (connection == null)
				return;

		}

		#endregion

		#endregion

		#region Initialize

		private async Task InitializeCanvasWithGraphNodes()
		{
			var infos = Graph.Nodes.Values.Select(GetNodeCreationInfo).ToList();

			await InvokeJSVoid("AddNodes", infos);
		}

		private record class NodeCreationInfo(string Id, string Name, float X, float Y, List<NodeCreationInfo_Connection> Inputs, List<NodeCreationInfo_Connection> Outputs);
		private record class NodeCreationInfo_Connection(string Id, string Name, List<string>? Connections);
		private NodeCreationInfo GetNodeCreationInfo(Node node)
		{
			var positionAttribute = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));

			return new(node.Id.ToString(),
				node.Name,
				positionAttribute.X,
				positionAttribute.Y,
				node.Inputs.Select(x => new NodeCreationInfo_Connection(x.Id, x.Name, x.Connections.Select(y => y.Id).ToList())).ToList(),
				node.Outputs.Select(x => new NodeCreationInfo_Connection(x.Id, x.Name, null)).ToList()); // no need to specify connections on outputs, they're all gonna be defined anyway from the inputs
		}

		#endregion

		public void Dispose()
		{
			Ref?.Dispose();
			Ref = null!;
		}
	}
}
