using Microsoft.UI.Xaml;
using TerritoryDefence.UI.Toolkit.Extensions;

namespace TerritoryDefence.UI.Toolkit.AttachedProperties
{
	public static class RelativeSourceBinding

	{
		public static readonly DependencyProperty AncestorTypeProperty = DependencyProperty.RegisterAttached("AncestorType", typeof(Type), typeof(RelativeSourceBinding), new PropertyMetadata(default(Type), OnAncestorTypeChanged));
		public static readonly DependencyProperty AncestorProperty = DependencyProperty.RegisterAttached("Ancestor", typeof(UIElement), typeof(RelativeSourceBinding), new PropertyMetadata(default(FrameworkElement)));

		public static void SetAncestorType(DependencyObject element, Type value)
		{
			element.SetValue(AncestorTypeProperty, value);
		}

		public static Type GetAncestorType(DependencyObject element)
		{
			return (Type)element.GetValue(AncestorTypeProperty);
		}

		public static void SetAncestor(DependencyObject element, UIElement? value)
		{
			element.SetValue(AncestorProperty, value);
		}

		public static UIElement GetAncestor(DependencyObject element)
		{
			return (UIElement)element.GetValue(AncestorProperty);
		}

		private static void OnAncestorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((FrameworkElement)d).Loaded -= OnFrameworkElementLoaded;

			if (e.NewValue != null)
			{
				((FrameworkElement)d).Loaded += OnFrameworkElementLoaded;
				OnFrameworkElementLoaded((FrameworkElement)d, null);
			}
		}

		private static void OnFrameworkElementLoaded(object sender, RoutedEventArgs? e)
		{
			var ancestorType = GetAncestorType((FrameworkElement)sender);
			if (ancestorType != null)
			{
				var findAncestor = ((FrameworkElement)sender).FindVisualParent(ancestorType);
				RelativeSourceBinding.SetAncestor(((FrameworkElement)sender), findAncestor);
			}
			else
			{
				RelativeSourceBinding.SetAncestor(((FrameworkElement)sender), null);
			}
		}
	}
}
