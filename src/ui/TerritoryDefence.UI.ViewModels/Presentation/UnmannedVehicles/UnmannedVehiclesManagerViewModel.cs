using System.Collections.ObjectModel;

namespace TerritoryDefence.UI.ViewModels.Presentation.UnmannedVehicles;

public class UnmannedVehiclesManagerViewModel
{
	public ObservableCollection<string> UnmannedVehicles { get; } = new()
	{
		{ "0" }, { "1" }, { "2" }, { "3" }, { "4" }
	};
}
