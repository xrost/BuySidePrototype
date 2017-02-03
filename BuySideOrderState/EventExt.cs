﻿using System;

namespace BuySideOrderState
{
	public static class EventExt
	{
		public static void Raise(this EventHandler handler, object sender = null) => handler?.Invoke(sender, EventArgs.Empty);
	}
}