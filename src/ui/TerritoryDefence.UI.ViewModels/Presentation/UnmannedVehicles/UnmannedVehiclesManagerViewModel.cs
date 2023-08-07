using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace TerritoryDefence.UI.ViewModels.Presentation.UnmannedVehicles;

public class UnmannedVehiclesManagerViewModel
{
	public ObservableCollection<string> UnmannedVehicles { get; } = new();

	public ReactiveCommand<Unit, Unit> CreateUnmannedAerialVehicleCommand => ReactiveCommand.Create(CreateUnmannedAerialVehicleExecute);

	private void CreateUnmannedAerialVehicleExecute()
	{
		int newValue = UnmannedVehicles.Count + 1;
		UnmannedVehicles.Add(newValue.ToString(Thread.CurrentThread.CurrentCulture));
	}
}
