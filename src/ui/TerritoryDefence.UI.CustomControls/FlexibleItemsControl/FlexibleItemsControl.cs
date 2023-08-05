using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace TerritoryDefence.UI.CustomControls.FlexibleItemsControl;

public partial class FlexibleItemsControl : ItemsControl
{
	public static DependencyProperty RelativeModeTemplateProperty { get; } =
		DependencyProperty.Register("RelativeModeTemplate",
			typeof(DataTemplate),
			typeof(FlexibleItemsControl),
#if WINDOWS
			new PropertyMetadata(default,
#else
			new FrameworkPropertyMetadata(
					default,
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
#endif
					(s, e) => ((FlexibleItemsControl)s)?.OnRelativeModeTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
				)
			);

	public FlexibleItemsControl() : base()
	{
		this.DefaultStyleKey = typeof(FlexibleItemsControl);
	}

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		base.PrepareContainerForItemOverride(element, item);
	}


	public DataTemplate RelativeModeTemplate
	{
		get => (DataTemplate)GetValue(RelativeModeTemplateProperty);
		set => SetValue(RelativeModeTemplateProperty, value);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		WrapRelativeModeTemplate(ItemTemplate);
	}

	protected virtual void OnRelativeModeTemplateChanged(DataTemplate oldRelativeModeTemplate, DataTemplate newRelativeModeTemplate)
	{
		if (ItemTemplate == null || newRelativeModeTemplate == null || !IsLoaded)
		{
			return;
		}
		WrapRelativeModeTemplate(ItemTemplate);
	}

	protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
	{
		if (newItemTemplate == RelativeModeTemplate || newItemTemplate == null)
		{
			base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);
			return;
		}

		if (RelativeModeTemplate == null)
		{
			return;
		}
		WrapRelativeModeTemplate(newItemTemplate);
	}

	protected void WrapRelativeModeTemplate(DataTemplate newItemTemplate)
	{
		if (RelativeModeTemplate?.LoadContent() is not Panel panel)
		{
			throw new TypeLoadException("RelativeModeTemplate content must be a Panel");
		}

		var testtt = newItemTemplate.LoadContent() as FrameworkElement;

		var contentControl = FindVisualChild<ContentControl>(panel);
		if (contentControl != null && newItemTemplate != null)
		{
			contentControl.Content = testtt;
			contentControl.UpdateLayout();
			ItemTemplate = RelativeModeTemplate;
			contentControl.UpdateLayout();
		}
		UpdateLayout();
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
}
