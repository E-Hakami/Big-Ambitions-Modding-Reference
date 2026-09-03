using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class K1ElevatorMaterial : ICompatibilityFix
{
	private const string AffectedBuildingSize = "ba:buildingsize_k";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && x.BuildingCached.BuildingSize == "ba:buildingsize_k"))
		{
			SerializedInteriorDesign serializedInteriorDesign = item.interiorDesigns.FirstOrDefault((SerializedInteriorDesign x) => x.UUID == "mjQiMk0ShUi5RYPXHcGGIA==");
			if (serializedInteriorDesign != null)
			{
				serializedInteriorDesign.materials[0].MaterialIndex = 0;
			}
		}
	}
}
