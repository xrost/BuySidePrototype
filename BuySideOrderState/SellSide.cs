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

		public void AcceptOrder(int brokerId)
		{
			var wasEmpty = orders.All(o => !o.IsAccepted);
			GetOrder(brokerId).Accept();
			if (wasEmpty)
				OnFirstOrderAccepted.Raise();
		}

		public void AllocateOrder(int brokerId)
		{
			var allocationExisted = orders.Any(o => o.IsAllocated);
			GetOrder(brokerId).Allocate();
			if (!allocationExisted)
				OnAllocated.Raise();
		}

		private void RaiseRejectedOrCancelled()
		{
			var allRejected = true;
			foreach (var order in orders)
			{
				if (!(order.IsDeleted || order.IsRejected))
					return;
				allRejected = allRejected && order.IsRejected;
			}
			if (allRejected)
				OnRejected.Raise();
			else
				OnCancelled.Raise();
		}

		public void DeleteOrder(int brokerId)
		{
			GetOrder(brokerId).Delete();
			RaiseRejectedOrCancelled();
		}

		public void RejectOrder(int brokerId)
		{
			GetOrder(brokerId).Reject();
			RaiseRejectedOrCancelled();
		}

		public void RejectCancel(int brokerId)
		{
			var before = orders.All(o => o.IsCancelRejected || o.NotAccepted);
			GetOrder(brokerId).RejectCancel();
			if (!before && orders.All(o => o.IsCancelRejected || o.NotAccepted))
				OnCancelRejected.Raise();
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

		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<SellSideOrder> GetEnumerator() => this.orders.GetEnumerator();


		#endregion

		public event EventHandler OnFirstOrderAccepted;
		public event EventHandler OnAllocated;
		public event EventHandler OnCancelled;
		public event EventHandler OnCancelRejected;
		public event EventHandler OnRejected;
	}
}