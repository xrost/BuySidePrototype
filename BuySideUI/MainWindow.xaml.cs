using System;
using System.Windows;
using BuySideUI.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace BuySideUI
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private MainViewModel CreateViewModel()
		{
			var current = DataContext as MainViewModel;
			current.Cleanup();
			var newViewModel = ServiceLocator.Current.GetInstance<MainViewModel>(Guid.NewGuid().ToString());
			return newViewModel;
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			var newViewModel = CreateViewModel();
			DataContext = newViewModel;
		}

		private void ResetToSellSide_Click(object sender, RoutedEventArgs e)
		{
			var newViewModel = CreateViewModel();
			newViewModel.AddOrderCommand.Execute(null);
			newViewModel.AddOrderCommand.Execute(null);
			DataContext = newViewModel;
		}
	}
}
