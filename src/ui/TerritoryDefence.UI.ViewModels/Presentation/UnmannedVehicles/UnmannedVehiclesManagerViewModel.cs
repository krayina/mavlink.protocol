using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using ReactiveUI;

namespace TerritoryDefence.UI.ViewModels.Presentation.UnmannedVehicles;

public class UnmannedVehiclesManagerViewModel
{
	public ObservableCollection<string> UnmannedVehicles { get; } = new()
	{
		//{ "0" }, { "1" }, { "2" }, { "3" }, { "4" }
	};

	public ReactiveCommand<Unit, Unit> CreateUnmannedVehicle => ReactiveCommand.Create(CreateUnmannedVehicleExecute);

	private void CreateUnmannedVehicleExecute()
	{
		int newValue = UnmannedVehicles.Count + 1;
		UnmannedVehicles.Add(newValue.ToString(Thread.CurrentThread.CurrentCulture));
	}
}
