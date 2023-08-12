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

	public static T? FindVisualChild<T>(this UIElement parent, string controlName) where T : FrameworkElement
	{
		if (parent == null)
		{
			return null;
		}
		if (parent is T && ((T)parent).Name == controlName)
		{
			return (T)parent;
		}
		T? result = null;
		int count = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < count; i++)
		{
			UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);

			if (FindVisualChild<T>(child, controlName) != null)
			{
				result = FindVisualChild<T>(child, controlName);
				break;
			}
		}
		return result;
	}

	public static FrameworkElement? GetFirstVisualChild(this UIElement element)
	{
		int childrenCount = VisualTreeHelper.GetChildrenCount(element);

		if (childrenCount > 0)
		{
			return (FrameworkElement)VisualTreeHelper.GetChild(element, 0);
		}
		return null;
	}
}
