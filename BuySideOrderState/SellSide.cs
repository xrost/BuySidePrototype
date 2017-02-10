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
		private bool buySideCancelled;

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
			if (!buySideCancelled && !allocationExisted)
				OnAllocated.Raise();
		}

		private void RaiseRejectedOrCancelled()
		{
			var counts = new EventSelector(orders);
			if (buySideCancelled)
				counts.RaiseCancelled(this);
			else
				counts.Raise(this);
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
			GetOrder(brokerId).RejectCancel();
			RaiseRejectedOrCancelled();
		}

		public bool HasAllocations() => orders.Any(o => o.IsAllocated);

		public void Cancel()
		{
			buySideCancelled = true;
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

		struct EventSelector
		{
			private readonly int cancelRejectedCount;
			private readonly int rejectedCount;
			private readonly int deletedCount;
			private readonly int orderCount;

			public EventSelector(IEnumerable<SellSideOrder> orders) : this()
			{
				foreach (var order in orders)
				{
					orderCount++;
					if (order.IsRejected)
						rejectedCount++;
					if (order.IsDeleted)
						deletedCount++;
					if (order.IsCancelRejected)
						cancelRejectedCount++;
				}
			}

			private bool AllRejected() => rejectedCount == orderCount;
			private bool AllInactive() => rejectedCount + deletedCount == orderCount;
			private bool AllCancelRejected() => cancelRejectedCount > 0 && cancelRejectedCount + rejectedCount + deletedCount == orderCount;

			public void Raise(SellSide sellSide)
			{
				if (AllRejected())
					sellSide.OnRejected.Raise();
				else if (AllInactive())
					sellSide.OnCancelled.Raise();
				else if (AllCancelRejected())
					sellSide.OnCancelRejected.Raise();
			}

			public void RaiseCancelled(SellSide sellSide)
			{
				if (AllInactive())
					sellSide.OnCancelConfirmed.Raise();
				else if (AllCancelRejected())
				{
					sellSide.buySideCancelled = false;
					sellSide.OnCancelRejected.Raise();
				}
			}
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
		public event EventHandler OnCancelConfirmed;
	}
}