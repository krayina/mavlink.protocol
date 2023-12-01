using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;

namespace TerritoryDefence.UI.Toolkit.Behaviors;

public sealed partial class ShowAttachedFlyoutAction : DependencyObject, IAction
{
    /// <summary>
    /// Gets or sets the owner of AttachedFlyout.
    /// </summary>
    public FrameworkElement SourceObject
    {
        get => (FrameworkElement)GetValue(SourceObjectProperty);
        set => SetValue(SourceObjectProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="TargetObject"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceObjectProperty = 
        DependencyProperty.Register(
            nameof(SourceObject),
            typeof(FrameworkElement),
            typeof(ShowAttachedFlyoutAction),
            new PropertyMetadata(null));

    public object Execute(object sender, object parameter)
    {
        var targetObject = SourceObject is null ? (FrameworkElement)sender : SourceObject;
        FlyoutBase.ShowAttachedFlyout(targetObject);
        return null!;
    }
}
