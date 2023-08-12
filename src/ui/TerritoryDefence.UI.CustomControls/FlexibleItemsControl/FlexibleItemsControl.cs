using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.Xaml.Interactivity;
using TerritoryDefence.UI.Toolkit.Extensions;

namespace TerritoryDefence.UI.CustomControls.FlexibleItemsControl;

public partial class FlexibleItemsControl : ItemsControl
{
	private const string c_optionPopupName = "PART_OptionPopup";
	private const string c_optionItemsControlName = "PART_OptionItemsControl";
	private const string c_movementElementName = "PART_MovementElement";

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
	private ItemsControl _optionItemsControl;
	private Toolkit.Behaviors.MoveElementPositionBehavior _moveElementPositionBehavior;

	public FlexibleItemsControl() : base()
	{
		this.DefaultStyleKey = typeof(FlexibleItemsControl);
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_optionPopup = GetTemplateChild(c_optionPopupName) as Popup
			?? throw new InvalidOperationException("OptionPopup not found at Template.");
		_optionItemsControl = GetTemplateChild(c_optionItemsControlName) as ItemsControl
			?? throw new InvalidOperationException("OptionItemsControl not found at Template.");
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
		ContentPresenter container;
		if (Items.Count > 2)
		{
			container = new ContentPresenter();
			SetRelativeContentPresenter(container, item, ItemTemplate);
		}
		else
		{
			container = element as ContentPresenter
				?? throw new InvalidOperationException($"The element {element} should be ContentPresenter.");
			SetRelativeContentPresenter(container, item, RelativeModeTemplate);
		}
		SetMoveElementPositionBehaviorAfterLoaded(container);
		SetDefaultRelativePosition(container);
	}

	private void SetMoveElementPositionBehaviorAfterLoaded(ContentPresenter container)
	{
		RoutedEventHandler? handler = null;
		handler = (sender, e) =>
		{
			container.Loaded -= handler;
			InitializeMoveElementPositionBehavior(container);
		};
		container.Loaded += handler;
	}

	private void InitializeMoveElementPositionBehavior(ContentPresenter container)
	{
		System.Diagnostics.Debug.WriteLine("SetMoveElementPositionBehavior");
		var movementElement = container.FindVisualChild<FrameworkElement>(c_movementElementName);
		var firstContentPresenterChild = container.GetFirstVisualChild();
		if (movementElement != null && firstContentPresenterChild != null)
		{
			var behaviors = Interaction.GetBehaviors(firstContentPresenterChild);
			var existingMoveElementPositionBehavior = behaviors.FirstOrDefault(x => x is Toolkit.Behaviors.MoveElementPositionBehavior);
			if (existingMoveElementPositionBehavior != null)
			{
				_moveElementPositionBehavior = (Toolkit.Behaviors.MoveElementPositionBehavior)existingMoveElementPositionBehavior;
				return;
			}

			Toolkit.Behaviors.MoveElementPositionBehavior moveElementPositionBehavior = new();
			BindingOperations.SetBinding(moveElementPositionBehavior,
				Toolkit.Behaviors.MoveElementPositionBehavior.MovementElementProperty,
					new Binding()
					{
						ElementName = c_movementElementName,
					});
			behaviors.Add(moveElementPositionBehavior);
			_moveElementPositionBehavior = moveElementPositionBehavior;
		}
	}

	private void SetRelativeContentPresenter(ContentPresenter contentPresenter, object item, DataTemplate template)
	{
		contentPresenter.ContentTemplate = template;
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
		if (!_optionPopup.IsOpen)
		{
			_optionPopup.IsOpen = true;
		}

		SetPopupItemSize(container);
		_optionItemsControl.Items.Add(container);
	}

	private void SetPopupItemSize(ContentPresenter container)
	{
		container.Height = 70;
		container.Width = 70;
	}
}
