using System.Collections.Generic;
using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateBathroomTiles : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				buildingRegistration.interiorDesigns = new List<SerializedInteriorDesign>();
				continue;
			}
			foreach (SerializedInteriorDesign interiorDesign in buildingRegistration.interiorDesigns)
			{
				interiorDesign.materials = interiorDesign.materials.Where((SerializedInteriorDesign.SerializableInteriorMaterial material) => !material.ignore).ToArray();
			}
		}
	}
}
