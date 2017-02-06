using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BuySideUI
{
	public class StepCountToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var count = (int) value;
			return count > 0 ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}