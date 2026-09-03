using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveCorruptedWholesaleContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.DeliveryContracts.RemoveAll(delegate(DeliveryContract x)
		{
			if (!(x.wholesaleAddress == null))
			{
				BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration y) => y.Address == x.wholesaleAddress);
				return buildingRegistration == null || !(buildingRegistration.businessTypeName == "ba:businesstype_wholesalestore");
			}
			return true;
		});
	}
}
