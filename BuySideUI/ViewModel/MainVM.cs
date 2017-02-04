using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BuySideOrderState;
using BuySideUI.Events;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;

namespace BuySideUI.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
		private readonly Order model = new Order();

		public MainViewModel()
		{
			SellSideOrders = model.SellSide.Orders.Select(o => new SellSideOrderViewModel(o)).ToList();
			Brokers = model.SellSide.Orders.Select(o => new BrokerViewModel(o.BrokerId)).ToList();

			AddOrderCommand = new RelayCommand(AddBuySideOrder, () => model.IsActionAvailable(Order.Trigger.AddBuySideOrder));

			CancelBuySideOrderCommand = new RelayCommand(() => model.CancelBuySide(), 
				() => model.IsActionAvailable(Order.Trigger.CancelBuySide));


	        model.OnSellSide += OnSellSideTransition;
			model.OnStateChange += (_, state) => RaisePropertyChanged(() => StateName);
			model.SellSide.OnFirstOrderAccepted += (_, state) => BuySideMessages.Add("Order Accepted");
			model.SellSide.OnRejected += (_, state) => BuySideMessages.Add("Order Rejected");
			model.SellSide.OnCancelled += (_, state) => BuySideMessages.Add("Order Cancelled");
			model.SellSide.OnCancelRejected += (_, state) => BuySideMessages.Add("Cancellation Rejected");
			model.OnBuySideCancel += (_, args) => SellSideMessages.Add("Order was cancelled");

			Messenger.Default.Register<BrokerActionEvent>(this, OnBrokerAction);
		}

	    private void OnBrokerAction(BrokerActionEvent evt)
	    {
		    try
		    {
			    switch (evt.Action)
			    {
				    case BrokerAction.Accept:
					    model.OrderCreated(evt.BrokerId);
					    break;
				    case BrokerAction.Allocate:
					    model.OrderAllocated(evt.BrokerId);
					    break;
				    case BrokerAction.Delete:
					    model.OrderDeleted(evt.BrokerId);
					    break;
				    case BrokerAction.Reject:
					    model.OrderRejected(evt.BrokerId);
					    break;
					case BrokerAction.RejectCancel:
						model.CancelRejected(evt.BrokerId);
						break;
				}

		    }
		    catch (InvalidOperationException)
		    {
			    MessageBox.Show($"Action {evt.Action} is not allowed");
		    }
	    }

	    private void OnSellSideTransition(object sender, EventArgs eventArgs)
	    {
			ShowSellSide = true;
			RaisePropertyChanged(nameof(ShowSellSide));
			SellSideMessages.Add("New buy side order");
			Messenger.Default.Send(new BuySideCompletedEvent());
		}

		public IReadOnlyCollection<SellSideOrderViewModel> SellSideOrders { get; }
		public IReadOnlyCollection<BrokerViewModel> Brokers { get; }

	    public ObservableCollection<string> BuySideMessages { get; } = new ObservableCollection<string>();
	    public ObservableCollection<string> SellSideMessages { get; } = new ObservableCollection<string>();
	    public ObservableCollection<string> GatewayMessages { get; } = new ObservableCollection<string>();

		public RelayCommand AddOrderCommand { get; }
		public RelayCommand CancelBuySideOrderCommand { get; }

	    public int BuySideStepCount => model.BuySideOrderCount;
		public bool ShowSellSide { get; private set; }


	    public string StateName => model.GetState().ToString();

	    private void AddBuySideOrder()
	    {
		    model.AddBuySideOrder(DateTime.Now.ToLongTimeString());
			RaisePropertyChanged(nameof(BuySideStepCount));
		}

	    public override void Cleanup()
	    {
			Messenger.Default.Unregister<BrokerActionEvent>(this);
		}
    }
}