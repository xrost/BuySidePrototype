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

		public string State
		{
			get
			{
				switch (order.GetState())
				{
					case SellSideOrder.State.NotAccepted:
						return "";
					case SellSideOrder.State.Accepted:
						return "Accepted";
					case SellSideOrder.State.WaitingForAllocation:
						return "Allocation Pending";
					case SellSideOrder.State.Allocated:
						return "Allocated";
					case SellSideOrder.State.Deleted:
						return "Deleted";
				}
				return "Unknown";
			}
		}
	}
}