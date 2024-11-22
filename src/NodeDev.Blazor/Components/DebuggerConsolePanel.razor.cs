﻿using Microsoft.AspNetCore.Components;
using NodeDev.Core;
using System.Reactive.Subjects;


namespace NodeDev.Blazor.Components;

public partial class DebuggerConsolePanel : ComponentBase, IDisposable
{
	[Parameter]
	public Project Project { get; set; } = null!;

	private IDisposable? GraphExecutionChangedDisposable;

	private TextWriter? PreviousTextWriter;

	private readonly ReverseQueue Lines = new(10_000);
	private string LastLine = ">";

	private bool IsShowing = false;

	private readonly Subject<object?> RefreshRequiredSubject = new();
	private IDisposable? RefreshRequiredDisposable;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		RefreshRequiredDisposable = RefreshRequiredSubject.AcceptThenSample(TimeSpan.FromMilliseconds(100)).Subscribe(_ => InvokeAsync(StateHasChanged));

		GraphExecutionChangedDisposable = Project.GraphExecutionChanged.Subscribe(OnGraphExecutionChanged);
	}

	public void Clear()
	{
		Lines.Clear();
		LastLine = ">";
	}

	private void OnGraphExecutionChanged(bool status)
	{
		if (status)
		{
			Clear();
			PreviousTextWriter = Console.Out;
			Console.SetOut(new ControlWriter(AddText));
		}
		else if (PreviousTextWriter != null) // we were in debug mode, let's go back to normal
		{
			Console.SetOut(PreviousTextWriter);
			PreviousTextWriter = null;
		}
	}

	private void AddText(string text)
	{
		var newLineCharacterIndex = text.IndexOf('\r');
		if (newLineCharacterIndex == -1)
		{
			// no new line character, just add the text to the last line
			LastLine += text;
		}
		else
		{
			int nbCrop = 1; // crop the \r
			if (text.Length > newLineCharacterIndex + 1 && text[newLineCharacterIndex + 1] == '\n')
			{
				nbCrop = 2; // crop the \r\n
			}

			var previousLine = string.Concat(LastLine, text.AsSpan()[..newLineCharacterIndex]);
			Lines.Enqueue(previousLine);
			LastLine = string.Concat(">", text.AsSpan()[(newLineCharacterIndex + nbCrop)..]);
		}

		RefreshRequiredSubject.OnNext(null);
	}

	public void Dispose()
	{
		GraphExecutionChangedDisposable?.Dispose();
		RefreshRequiredDisposable?.Dispose();

		if (PreviousTextWriter != null)
		{
			Console.SetOut(PreviousTextWriter);
			PreviousTextWriter = null;
		}
	}

	private class ControlWriter : TextWriter
	{
		private Action<string> AddText;

		public ControlWriter(Action<string> addText)
		{
			AddText = addText;
		}

		public override void Write(char value)
		{
			AddText(value.ToString());
		}

		public override void Write(string? value)
		{
			AddText(value ?? "null");
		}

		public override System.Text.Encoding Encoding
		{
			get { return System.Text.Encoding.ASCII; }
		}
	}

	private class ReverseQueue
	{
		private readonly List<string> Queue;

		private int StartIndex = 0;

		private int NbLines => Queue.Count;

		public ReverseQueue(int maxLines)
		{
			Queue = new(maxLines);
		}

		public void Clear()
		{
			Queue.Clear();
			StartIndex = 0;
		}

		public void Enqueue(string line)
		{
			if (NbLines == Queue.Capacity) // we are full, let's just replace the first line
			{
				Queue[StartIndex] = line;
				StartIndex = (StartIndex + 1) % Queue.Capacity;
			}
			else
				Queue.Add(line);
		}

		public IEnumerable<string> Reverse()
		{
			// Return the values from the last to the first
			// If the queue isn't full, StartIndex will be 0 and we won't enter the loop
			for (int i = StartIndex - 1; i >= 0; i--)
				yield return Queue[i];

			// Now start from the other side of the queue and go to the StartIndex
			for (int i = Queue.Count - 1; i >= StartIndex; i--)
				yield return Queue[i];
		}

	}

}
