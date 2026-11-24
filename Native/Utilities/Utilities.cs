using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sidebar
{
	public class Utilities
	{
		private static int _isRunning = 0;
		public static void ReleaseLargeResourcesAsync ()
		{
			if (Interlocked.CompareExchange (ref _isRunning, 1, 0) != 0)
				return;
			Task.Factory.StartNew (() => {
				try
				{
					ReleaseLargeResources ();
				}
				finally
				{
					Interlocked.Exchange (ref _isRunning, 0);
				}
			});
		}
		public static void ReleaseLargeResources ()
		{
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			GC.Collect ();
		}
	}
}
