using NodeDev.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor;

public static class Utility
{
	public static IObservable<T> AcceptThenSample<T>(this IObservable<T> source, TimeSpan interval)
	{
		return Observable.Create<T>(o =>
		{
			var stopwatch = new Stopwatch();
			T lastReceived = default!;
			bool isTimerRunning = false;
			var timer = new Timer(_ =>
			{
				o.OnNext(lastReceived);

				isTimerRunning = false; // timer is done, so we can start a new one
				lastReceived = default!; // clear the cache
				stopwatch.Restart();
			});

			var sub = source.Subscribe(x =>
			{
				if (!stopwatch.IsRunning) // either the first time or it's been a while since the last time
				{
					o.OnNext(x); // send the value away
					stopwatch.Restart(); // start the timer since the last time we sent a value
				}
				else if (stopwatch.Elapsed < interval) // it's not been long enough, cache the value and start a timer to send if nothing else comes in
				{
					lastReceived = x;

					if (!isTimerRunning)
					{
						isTimerRunning = true;
						timer.Change(interval - stopwatch.Elapsed, Timeout.InfiniteTimeSpan); // Start a timer for the remaining time, with no repeat
					}
				}
			}, ex =>
			{
				timer?.Dispose();
				timer = null!;
				o.OnError(ex);
			}, () =>
			{
				timer?.Dispose();
				timer = null!;
				o.OnCompleted();
			});

			return Disposable.Create(() =>
			{
				timer?.Dispose();
				timer = null!;
				sub.Dispose();
			});
		});
	}

}
