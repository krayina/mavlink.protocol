using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace TerritoryDefence.UI.CustomControls.FlexibleItemsControl;

public partial class FlexibleItemsControl : ItemsControl
{
	private const string c_optionPopupName = "PART_OptionPopup";
	private const string c_optionItemsRepeaterName = "PART_OptionItemsRepeater";

	public static DependencyProperty OptionPanelItemTemplateProperty { get; } =
	DependencyProperty.Register("OptionPanelItemTemplate",
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

	private Popup _optionPopup;
	private ItemsRepeater _optionItemsRepeater;
	protected ObservableCollection<FrameworkElement> OptionElements => new ObservableCollection<FrameworkElement>();

	public FlexibleItemsControl() : base()
	{
		this.DefaultStyleKey = typeof(FlexibleItemsControl);
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_optionPopup = GetTemplateChild(c_optionPopupName) as Popup
			?? throw new InvalidOperationException("OptionPopup is not found at Template.");
		_optionItemsRepeater = GetTemplateChild(c_optionItemsRepeaterName) as ItemsRepeater
			?? throw new InvalidOperationException("OptionItemsRepeater is not found at Template.");

		Binding itemsSourceBinding = new Binding()
		{
			Path = new PropertyPath("OptionElements"),
			Source = this
		};
		_optionItemsRepeater.SetBinding(ItemsRepeater.ItemsSourceProperty, itemsSourceBinding);
	}

	public DataTemplate OptionPanelItemTemplate
	{
		get => (DataTemplate)GetValue(OptionPanelItemTemplateProperty);
		set => SetValue(OptionPanelItemTemplateProperty, value);
	}

	public DataTemplate RelativeModeTemplate
	{
		get => (DataTemplate)GetValue(RelativeModeTemplateProperty);
		set => SetValue(RelativeModeTemplateProperty, value);
	}

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		ContentPresenter defaultContentPresenter;
		if (Items.Count > 2)
		{
			defaultContentPresenter = new ContentPresenter();
		}
		else
		{
			defaultContentPresenter = element as ContentPresenter
				?? throw new InvalidOperationException($"The element {element} should be ContentPresenter.");
		}

		SetRelativeContentPresenter(defaultContentPresenter, item);
		SetDefaultRelativePosition(defaultContentPresenter);
	}

	private void SetRelativeContentPresenter(ContentPresenter contentPresenter, object item)
	{
		contentPresenter.ContentTemplate = RelativeModeTemplate;
		SetContent(contentPresenter, ContentPresenter.ContentProperty);

		void SetContent(ContentPresenter container, DependencyProperty contentProperty)
		{
			string displayMemberPath = DisplayMemberPath;
			if (string.IsNullOrEmpty(displayMemberPath))
			{
				container.SetValue(contentProperty, item);
			}
			else
			{
				container.SetBinding(contentProperty, new Binding
				{
					Path = new PropertyPath(displayMemberPath),
					Source = item
				});
			}
		}
	}

	protected virtual void SetDefaultRelativePosition(ContentPresenter container)
	{
		if (Items.Count == 1)
		{
			SetRelativeFullSize(container);
		}
		else if (Items.Count == 2)
		{
			var currentIndex = IndexFromContainer(container);
			SetRelativeUnderPrevious(container, (ContentPresenter)ContainerFromIndex(currentIndex - 1));
		}
		else
		{
			AddToOptionPanel(container);
		}
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

	private void AddToOptionPanel(ContentPresenter container)
	{
		_optionPopup.IsOpen = true;
		OptionElements.Add(container);
	}
}
