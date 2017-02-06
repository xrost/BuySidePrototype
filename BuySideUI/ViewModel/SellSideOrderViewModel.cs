using System;
using System.Linq;
using BuySideOrderState;
using GalaSoft.MvvmLight;

namespace BuySideUI.ViewModel
{
	public class SellSideOrderViewModel : ObservableObject
	{
		private readonly SellSideOrder order;

		public SellSideOrderViewModel(SellSideOrder order)
		{
			this.order = order;
			order.OnStateChange += OnStateChange;
			BrokerId = order.BrokerId;
			BrokerName = "Broker #" + order.BrokerId;
		}

		private void OnStateChange(object sender, EventArgs args)
		{
			RaisePropertyChanged(() => AllowedActions);
			RaisePropertyChanged(() => State);
		}

		public int BrokerId { get; }
		public string BrokerName { get; }
		public string AllowedActions => string.Join(" | ", order.AllowedActions.Where(a => a != SellSideOrder.Action.RejectCancel));

		public string State
		{
			get
			{
				var result = GetState();
				if (order.IsCancelRejected)
					result += " (CR)";
				return result;
			}
		}

		private string GetState()
		{
			switch (order.GetState())
			{
				case SellSideOrder.State.DoesNotExist:
					return "Pending";
				case SellSideOrder.State.Rejected:
					return "Rejected";
				case SellSideOrder.State.PendingAllocation:
					return "Order Confirmed";
				case SellSideOrder.State.Allocated:
					return "Allocated";
				case SellSideOrder.State.Deleted:
					return "Deleted";
			}
			return "Unknown";
		}
	}
}