using CommunityToolkit.WinUI.UI.Controls;
using Mapsui.Tiling;

namespace TerritoryDefence.UI.Pages;

public sealed partial class UnmannedVehiclesManagerPage : Page
{
	public UnmannedVehiclesManagerPage()
	{
		this.InitializeComponent();
		//MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		//MapControl.Map.Navigator.RotationLock = false;
		//MapControl.UnSnapRotationDegrees = 30;
		//MapControl.ReSnapRotationDegrees = 5;
	}
}
