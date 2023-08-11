using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;
using TerritoryDefence.UI.Toolkit.Extensions;
using Windows.Foundation;

namespace TerritoryDefence.UI.Toolkit.Behaviors;

public class MoveElementPositionBehavior : Behavior<FrameworkElement>
{
	public static readonly DependencyProperty MovementElementProperty = DependencyProperty.Register("MovementElement",
	   typeof(FrameworkElement), typeof(MoveElementPositionBehavior),
	   new PropertyMetadata(null, (d, e) => ((MoveElementPositionBehavior)d).OnMovementElementChanged(e)));

	private FrameworkElement? _movementElement;
	private Point _previewPoint;
	private int _pointerId = -1;

	public FrameworkElement MovementElement
	{
		get => (FrameworkElement)this.GetValue(MovementElementProperty);
		set => this.SetValue(MovementElementProperty, value);
	}

	protected override void OnAttached()
	{
		base.OnAttached();
		SubscribeEvents(_movementElement);
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		UnsubscribeEvents(_movementElement);
	}

	private void SubscribeEvents(FrameworkElement? movementElement)
	{
		if (_movementElement == null)
		{
			return;
		}
		UnsubscribeEvents(movementElement);

		_movementElement?.AddHandler(UIElement.PointerPressedEvent, (PointerEventHandler)OnElementPointerPressed, true);
	}

	private void UnsubscribeEvents(FrameworkElement? movementElement)
	{
		_movementElement?.RemoveHandler(UIElement.PointerPressedEvent, (PointerEventHandler)OnElementPointerPressed);
	}

	private void OnElementPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("___OnElementPointerPressed");
		if (_movementElement == null)
		{
			return;
		}
		_movementElement.AddHandler(UIElement.PointerReleasedEvent, (PointerEventHandler)OnElementPointerReleased, true);
		_movementElement.PointerMoved += OnMovementElementMoved;
		_previewPoint = e.GetCurrentPoint(_movementElement).Position;
		_pointerId = (int)e.Pointer.PointerId;
	}

	private void OnElementPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("___OnElementPointerReleased");
		if (_movementElement == null)
		{
			return;
		}

		if (e.Pointer.PointerId == _pointerId)
		{
			_pointerId = -1;
		}
		_movementElement.RemoveHandler(UIElement.PointerReleasedEvent, (PointerEventHandler)OnElementPointerReleased);
		_movementElement.PointerMoved -= OnMovementElementMoved;
	}

	private void OnMovementElementMoved(object sender, PointerRoutedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("___OnMovementElementMoved");
		double zoomFactor = 1;
		var position = e.GetCurrentPoint((UIElement)sender).Position;
		SetElementPosition(
			new Point
			{
				X = (position.X - _previewPoint.X) / zoomFactor,
				Y = (position.Y - _previewPoint.Y) / zoomFactor
			});
		_previewPoint = position;
	}

	protected virtual void SetElementPosition(Point shift)
	{
		var transform = AssociatedObject.InitializeTransform();
		transform.X += shift.X;
		transform.Y += shift.Y;
	}

	private void OnMovementElementChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue != null)
		{
			UnsubscribeEvents((FrameworkElement)e.OldValue);
		}
		else if (e.NewValue == null)
		{
			if (_movementElement != null)
			{
				UnsubscribeEvents(_movementElement);
			}
			return;
		}

		_movementElement = (FrameworkElement)e.NewValue;
		SubscribeEvents(_movementElement);
	}
}
