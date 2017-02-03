using System;
using System.Collections.Generic;

namespace BuySideOrderState
{
	public class BuySide
	{
		private readonly List<object> steps = new List<object>();

		public void Add(object step)
		{
			steps.Add(step);
		}

		public int Count => steps.Count;

		public bool IsCompleted() => steps.Count == 2;
	}
}