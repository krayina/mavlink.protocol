using Mapsui.Tiling;

namespace TerritoryDefence.UI.Shared.Pages
{
	public sealed partial class UnmannedVehiclesManagerPage : Page
	{
		public UnmannedVehiclesManagerPage()
		{
			this.InitializeComponent();
            MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            MapControl.Map.Navigator.RotationLock = false;
            MapControl.UnSnapRotationDegrees = 30;
            MapControl.ReSnapRotationDegrees = 5;
        }
    }
}
