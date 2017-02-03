using System;
using Stateless;

namespace BuySideOrderState
{
	class Program
	{
		public static void Main()
		{
			var order = new Order();
			order.AddBuySideOrder("step1");
			order.AddBuySideOrder("step2");
			order.OrderCreated(0);
			order.OrderCreated(1);
			order.OrderCreated(2);
			//order.CancelBuySide();
			order.OrderDeleted(0);
			order.OrderDeleted(1);
			order.OrderDeleted(2);
		}
	}
}
