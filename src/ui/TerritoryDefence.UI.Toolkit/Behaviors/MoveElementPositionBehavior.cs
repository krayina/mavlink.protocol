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
	private bool _isMoving;

	public FrameworkElement MovementElement
	{
		get => (FrameworkElement)this.GetValue(MovementElementProperty);
		set => this.SetValue(MovementElementProperty, value);
	}

	public event EventHandler<bool>? MovingStateChanged;

	public bool IsMoving
	{
		get => _isMoving;
		protected set
		{
			_isMoving = value;
			MovingStateChanged?.Invoke(this, value);
		}
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
		if (movementElement == null)
		{
			return;
		}
		UnsubscribeEvents(movementElement);

		movementElement?.AddHandler(UIElement.PointerPressedEvent, (PointerEventHandler)OnElementPointerPressed, true);
	}

	private void UnsubscribeEvents(FrameworkElement? movementElement)
	{
		movementElement?.RemoveHandler(UIElement.PointerPressedEvent, (PointerEventHandler)OnElementPointerPressed);
	}

	private void OnElementPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("___OnElementPointerPressed");
		if (_movementElement == null)
		{
			return;
		}
		UIElement parent = AssociatedObject.GetTopUIElement()!;
		parent.AddHandler(UIElement.PointerReleasedEvent, (PointerEventHandler)OnElementPointerReleased, true);
		parent.PointerMoved += OnMovementElementMoved;
		_previewPoint = e.GetCurrentPoint(parent).Position;
		_pointerId = (int)e.Pointer.PointerId;
	}

	private void OnElementPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (IsMoving)
		{
			IsMoving = false;
		}
		if (_movementElement == null)
		{
			return;
		}

		if (e.Pointer.PointerId == _pointerId)
		{
			_pointerId = -1;
		}
		UIElement parent = AssociatedObject.GetTopUIElement()!;
		parent.RemoveHandler(UIElement.PointerReleasedEvent, (PointerEventHandler)OnElementPointerReleased);
		parent.PointerMoved -= OnMovementElementMoved;
	}

	private void OnMovementElementMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!IsMoving)
		{
			IsMoving = true;
		}
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

	protected virtual void SetElementPosition(Point shift)
	{
		var transform = AssociatedObject.InitializeTransform();
		transform.X += shift.X;
		transform.Y += shift.Y;
	}

	protected virtual void OnMovementActivated() { }

	protected virtual void OnMovementDeactivated() { }
}
