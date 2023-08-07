using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace TerritoryDefence.UI.Toolkit.Behaviors;

public class ShowAttachedFlyoutBehavior
	: BehaviorBase<FrameworkElement>
{
	protected override bool Initialize()
	{
		var result = ShowAttachedFlyout();
		return result;
	}

	protected override bool Uninitialize()
	{
		HideAttachedFlyout();
		return true;
	}

	private bool ShowAttachedFlyout()
	{
		if (AssociatedObject == null)
		{
			return false;
		}
		FlyoutBase.ShowAttachedFlyout(AssociatedObject);
		return true;
	}

	private void HideAttachedFlyout()
	{
		FlyoutBase.GetAttachedFlyout(AssociatedObject).Hide();
	}
}
