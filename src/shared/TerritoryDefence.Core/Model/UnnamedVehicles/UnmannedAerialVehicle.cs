namespace TerritoryDefence.Core.Model.UnnamedVehicles;

public record UnmannedAerialVehicle(string Id, IReadOnlyCollection<dynamic> Propellers) : UnmannedVehicle(Id);
