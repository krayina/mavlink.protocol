using System.ComponentModel;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Devices.Enumeration;

namespace TerritoryDefence.UI.CustomControls.FlexibleItemsControl;

public partial class FlexibleItemsControl : ItemsControl
{
	public static DependencyProperty RelativeModeTemplateProperty { get; } =
		DependencyProperty.Register("RelativeModeTemplate",
			typeof(DataTemplate),
			typeof(FlexibleItemsControl),
#if WINDOWS
			new PropertyMetadata(default
#else
			new FrameworkPropertyMetadata(
					default,
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext
#endif
					//(s, e) => ((FlexibleItemsControl)s)?.OnRelativeModeTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
				)
			);

	public FlexibleItemsControl() : base()
	{
		this.DefaultStyleKey = typeof(FlexibleItemsControl);
	}

	//protected override DependencyObject GetContainerForItemOverride()
	//{
	//	DependencyObject a = base.GetContainerForItemOverride();
	//	return a;
	//}

	public DataTemplate RelativeModeTemplate
	{
		get => (DataTemplate)GetValue(RelativeModeTemplateProperty);
		set => SetValue(RelativeModeTemplateProperty, value);
	}

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		ContentPresenter defaultContentPresenter = element as ContentPresenter;

		base.PrepareContainerForItemOverride(element, item);
		defaultContentPresenter.ContentTemplate = RelativeModeTemplate;

		//var relativeModeTemplateContent = RelativeModeTemplate.LoadContent() as FrameworkElement;
		//var contentPresenter = FindVisualChild<ContentPresenter>(relativeModeTemplateContent);
		//contentPresenter.ContentTemplate = null;

		//contentPresenter.LayoutUpdated += (s, e) =>
		//{
		//	if (contentPresenter.ContentTemplate != null) 
		//	{
		//		var aaa = contentPresenter.ContentTemplate.LoadContent() as FrameworkElement;
		//	}
		//};
		//contentPresenter.SetBinding(ContentPresenter.ContentTemplateProperty,
		//	new Binding
		//	{
		//		Path = new PropertyPath("ItemTemplate"),
		//		Source = this
		//	}
		//);



		//contentPresenter.ContentTemplate = ItemTemplate;
		//TryRepairContentConnection(contentControl, item);
		//SetContent(contentControl, FrameworkElement.DataContextProperty);

		//contentPresenter.ContentTemplate = ItemTemplate;
		//contentPresenter.SetValue(ContentPresenter.ContentProperty, item);

		//		void TryRepairContentConnection(ContentControl container, object item)
		//		{
		//			UIElement uIElement = item as UIElement;
		//			if (uIElement != null && container.DataContext == uIElement && GetVisualTreeParent(uIElement) == null)
		//			{
		//				container.DataContext = null;
		//			}
		//		}

		//		UIElement GetVisualTreeParent(UIElement uiElement)
		//		{
		//#if WINDOWS
		//			return VisualTreeHelper.GetParent(uiElement) as UIElement;
		//#else
		//			UIElement VisualParent(FrameworkElement element) => ((IDependencyObjectStoreProvider)element).Store.Parent as UIElement;
		//			return VisualParent(uiElement as FrameworkElement);
		//#endif
		//		}

		//		void SetContent(FrameworkElement container, DependencyProperty contentProperty)
		//		{
		//			string displayMemberPath = DisplayMemberPath;
		//			if (string.IsNullOrEmpty(displayMemberPath))
		//			{
		//				container.SetValue(contentProperty, item);
		//			}
		//			else
		//			{
		//				container.SetBinding(contentProperty, new Binding
		//				{
		//					Path = new PropertyPath(displayMemberPath),
		//					Source = item
		//				});
		//				//container.SetValue(ItemHasManualBindingExpressionProperty, true);
		//			}
		//		}
	}

	private T? FindVisualChild<T>(DependencyObject? obj)
		where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
		{
			DependencyObject? child = VisualTreeHelper.GetChild(obj, i);
			if (child != null && child is T)
			{
				return (T)child;
			}
			else
			{
				T? childOfChild = FindVisualChild<T>(child);
				if (childOfChild != null)
				{
					return childOfChild;
				}
			}
		}
		return default;
	}

	internal static void FindChildren<T>(List<T> results, DependencyObject startNode)
		where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(startNode);
		for (int i = 0; i < count; i++)
		{
			DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
			if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
			{
				T asType = (T)current;
				results.Add(asType);
			}
			FindChildren<T>(results, current);
		}
	}
}


public class RelativeSourceBinding
{
	public static readonly DependencyProperty AncestorTypeProperty = DependencyProperty.RegisterAttached("AncestorType", typeof(Type), typeof(RelativeSourceBinding), new PropertyMetadata(default(Type), OnAncestorTypeChanged));

	public static void SetAncestorType(DependencyObject element, Type value)
	{
		element.SetValue(AncestorTypeProperty, value);
	}

	public static Type GetAncestorType(DependencyObject element)
	{
		return (Type)element.GetValue(AncestorTypeProperty);
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

	public static readonly DependencyProperty AncestorProperty = DependencyProperty.RegisterAttached("Ancestor", typeof(UIElement), typeof(RelativeSourceBinding), new PropertyMetadata(default(FrameworkElement)));

	public static void SetAncestor(DependencyObject element, UIElement? value)
	{
		element.SetValue(AncestorProperty, value);
	}

	public static UIElement GetAncestor(DependencyObject element)
	{
		return (UIElement)element.GetValue(AncestorProperty);
	}
}

public static class Aa
{
	public static UIElement FindVisualParent(this UIElement element, Type type)
	{
		UIElement parent = element;
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
}

