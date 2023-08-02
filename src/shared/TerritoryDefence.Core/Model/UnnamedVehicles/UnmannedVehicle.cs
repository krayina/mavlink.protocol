using TerritoryDefence.Core.Interfaces.UnmannedVehicle;

namespace TerritoryDefence.Core.Model.UnnamedVehicles;
public abstract record UnmannedVehicle(string Id) : IUnmannedVehicle;
