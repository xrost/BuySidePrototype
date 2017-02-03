using System;
using BuySideUI.Events;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;

namespace BuySideUI.ViewModel
{
	public class BrokerViewModel : ObservableObject
	{
		private bool isEnabled = false;

		public BrokerViewModel(int id)
		{
			Id = id;
			AcceptCommand = new RelayCommand(() => Raise(BrokerAction.Accept), () => isEnabled);
			AllocateCommand = new RelayCommand(() => Raise(BrokerAction.Allocate), () => isEnabled);
			DeleteCommand = new RelayCommand(() => Raise(BrokerAction.Delete), () => isEnabled);

			Messenger.Default.Register<BuySideCompletedEvent>(this, (_) => isEnabled = true);
		}

		public int Id { get; }
		public string Name => "Broker #" + Id;
		public RelayCommand AcceptCommand { get; }
		public RelayCommand AllocateCommand { get; }
		public RelayCommand DeleteCommand { get; }

		private void Raise(BrokerAction action)
		{
			Messenger.Default.Send(new BrokerActionEvent(Id, action));
		}
	}

	public enum BrokerAction
	{
		Accept,
		Allocate,
		Delete
	}
}