using Mapsui.Tiling;

namespace TerritoryDefence.UI.UserControls;
public sealed partial class UnmannedAerialVehicle : UserControl
{
	public UnmannedAerialVehicle()
	{
		this.InitializeComponent();
		MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		MapControl.Map.Navigator.RotationLock = false;
		MapControl.UnSnapRotationDegrees = 30;
		MapControl.ReSnapRotationDegrees = 5;
	}
}
