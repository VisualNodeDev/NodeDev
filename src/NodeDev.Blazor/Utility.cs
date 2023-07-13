using NodeDev.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor;

public static class Utility
{

	public static IObservable<T> AcceptThenSample<T>(this IObservable<T> observable, TimeSpan interval) where T : class
	{
		return observable.Sample(interval);

		var firstChanges = observable
			.Timestamp()
			.Scan((Last: DateTime.MinValue, Value: (T?)null), (scan, value) => DateTime.Now - scan.Last > interval ? (value.Timestamp.LocalDateTime, value.Value) : (scan.Last, null))
			.Where(x => x.Value != null)
			.Select(x => x.Value!);

		return firstChanges
			.Merge(observable.Sample(interval))
			.Select(x => x);
	}

}
