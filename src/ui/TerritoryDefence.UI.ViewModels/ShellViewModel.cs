using TerritoryDefence.UI.ViewModels.Presentation.UnmannedVehicles;

namespace TerritoryDefence.UI.ViewModels;

public class ShellViewModel
{
	private readonly INavigator _navigator;

	public ShellViewModel(
		INavigator navigator)
	{
		_navigator = navigator;
		_ = Start();
	}

	public async Task Start()
	{
		await _navigator.NavigateViewModelAsync<UnmannedVehiclesManagerViewModel>(this);
	}
}
