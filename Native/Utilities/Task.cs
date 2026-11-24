using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks
{
	public static class TaskExtra
	{
		public static Task WhenAll (params Task [] tasks)
		{
			if (tasks == null) throw new ArgumentNullException ("tasks");
			if (tasks.Length == 0)
			{
				var tcs = new TaskCompletionSource<object> ();
				tcs.SetResult (null);
				return tcs.Task;
			}
			var tcsResult = new TaskCompletionSource<object> ();
			int remaining = tasks.Length;
			bool hasError = false;
			Action<Task> onCompleted = null;
			onCompleted = (t) =>
			{
				if (hasError) return;
				if (t.IsFaulted)
				{
					hasError = true;
					tcsResult.SetException (t.Exception.InnerExceptions);
					return;
				}
				if (t.IsCanceled)
				{
					hasError = true;
					tcsResult.SetCanceled ();
					return;
				}
				if (Interlocked.Decrement (ref remaining) == 0)
					tcsResult.SetResult (null);
			};
			foreach (var task in tasks)
			{
				task.ContinueWith (onCompleted, TaskContinuationOptions.ExecuteSynchronously);
			}
			return tcsResult.Task;
		}
	}
}
