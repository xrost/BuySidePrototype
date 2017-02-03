using System;
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
			order.OnStateChange += (sender, args) => RaisePropertyChanged(() => State);
			BrokerId = order.BrokerId;
			BrokerName = "Broker #" + order.BrokerId;
		}

		public int BrokerId { get; }
		public string BrokerName { get; }

		public void Accept()
		{
			order.Accept();
		}

		public void Allocate()
		{
			order.Allocate();
		}

		public void Delete()
		{
			order.Delete();
		}

		public void Reject()
		{
			order.Reject();
		}

		public void RejectCancel()
		{
			order.RejectCancel();
		}

		public string State
		{
			get
			{
				switch (order.GetState())
				{
					case SellSideOrder.State.NotAccepted:
						return "Pending";
					case SellSideOrder.State.Rejected:
						return "Rejected";
					case SellSideOrder.State.PendingAllocation:
						return "Order Confirmed";
					case SellSideOrder.State.Allocated:
						return "Allocated";
					case SellSideOrder.State.Deleted:
						return "Deleted";
					case SellSideOrder.State.CancelRejected:
						return "Cancel Rejected";
				}
				return "Unknown";
			}
		}
	}
}