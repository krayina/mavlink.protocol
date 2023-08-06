using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
		SetDefaultRelativePosition(defaultContentPresenter);
	}

	protected virtual void SetDefaultRelativePosition(ContentPresenter container)
	{
		if (Items.Count == 1)
		{
			SetRelativeFullSize(container);
		}
		if (Items.Count == 2)
		{
			var currentIndex = IndexFromContainer(container);
			SetRelativeUnderPrevious(container, (ContentPresenter)ContainerFromIndex(currentIndex - 1));
		}
	}

	class RelativeContainerInfo
	{
		public ContentPresenter ContentPresenter { get; }
	}

	private void SetRelativeFullSize(ContentPresenter container)
	{
		container.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
		container.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);

		container.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);
		container.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
	}

	private void SetRelativeUnderPrevious(ContentPresenter container, ContentPresenter previousContainer)
	{
		previousContainer.SetValue(RelativePanel.AlignBottomWithPanelProperty, false);
		previousContainer.Height = ActualHeight / 2;

		//container.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
		container.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);

		container.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);
		container.SetValue(RelativePanel.AlignRightWithPanelProperty, true);

		container.SetValue(RelativePanel.BelowProperty, previousContainer);
	}
}
