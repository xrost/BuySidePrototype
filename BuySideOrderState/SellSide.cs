using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BuySideOrderState
{
	public class SellSide : IEnumerable<SellSideOrder>
	{
		private readonly List<SellSideOrder> orders = new List<SellSideOrder>();

		public SellSide(int brokerCount)
		{
			for (int i = 0; i < brokerCount; i++)
			{
				orders.Add(new SellSideOrder(i));
			}
		}

		public bool AcceptOrder(int brokerId)
		{
			var wasEmpty = orders.All(o => !o.IsAccepted);
			GetOrder(brokerId).Accept();
			return wasEmpty;

		}

		public void AllocateOrder(int brokerId) => GetOrder(brokerId).Allocate();

		public bool DeleteOrder(int brokerId)
		{
			GetOrder(brokerId).Delete();
			return orders.All(o => !o.IsAccepted);
		}

		public bool RejectOrder(int brokerId)
		{
			GetOrder(brokerId).Reject();
			return orders.All(o => o.IsRejected);
		}

		public bool RejectCancel(int brokerId)
		{
			GetOrder(brokerId).RejectCancel();
			return orders.All(o => o.IsCancelRejected);
		}

		[CanBeNull]
		private SellSideOrder FindOrder(int brokerId) => orders.FirstOrDefault(b => b.BrokerId == brokerId);

		[NotNull]
		private SellSideOrder GetOrder(int brokerId)
		{
			var order = FindOrder(brokerId);
			if (order == null)
				throw new Exception("Broker not found");
			return order;
		}

		public bool HasAcceptedOrders() => orders.Any(b => b.IsAccepted);
		public bool AllOrdersDeleted() => orders.All(b => b.IsDeleted);
		public bool OrderExists(int brokerId) => (FindOrder(brokerId)?.IsAccepted).GetValueOrDefault();

		public bool CanAllocate(int brokerId)
		{
			var broker = FindOrder(brokerId);
			if (broker == null)
				return false;
			return !broker.IsAllocated;
		}

		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<SellSideOrder> GetEnumerator() => this.orders.GetEnumerator();


		#endregion
	}
}