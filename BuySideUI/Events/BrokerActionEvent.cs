using BuySideUI.ViewModel;

namespace BuySideUI.Events
{
	public class BrokerActionEvent
	{
		public BrokerActionEvent(int brokerId, BrokerAction action)
		{
			BrokerId = brokerId;
			Action = action;
		}

		public int BrokerId { get; }
		public BrokerAction Action { get; }
	}
}