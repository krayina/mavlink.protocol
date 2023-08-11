using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace TerritoryDefence.UI.Toolkit.Extensions;

public static class UIElementExtensions
{
	public static UIElement? FindVisualParent(this UIElement element, Type type)
	{
		UIElement? parent = element;
		while (parent != null)
		{
			if (type.IsAssignableFrom(parent.GetType()))
			{
				return parent;
			}
			parent = VisualTreeHelper.GetParent(parent) as UIElement;
		}
		return null;
	}

	public static UIElement? GetTopUIElement(this DependencyObject element)
	{
		DependencyObject? parent;
		while ((parent = VisualTreeHelper.GetParent(element)) != null)
		{
			element = parent;
		}
		return element as UIElement;
	}
}
