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

		private void button_Click(object sender, RoutedEventArgs e)
		{
			var current = DataContext as MainViewModel;
			current.Cleanup();
			DataContext = ServiceLocator.Current.GetInstance<MainViewModel>(Guid.NewGuid().ToString());
		}
	}
}
