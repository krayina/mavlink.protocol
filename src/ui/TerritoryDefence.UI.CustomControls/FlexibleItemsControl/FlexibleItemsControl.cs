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
	}
}
