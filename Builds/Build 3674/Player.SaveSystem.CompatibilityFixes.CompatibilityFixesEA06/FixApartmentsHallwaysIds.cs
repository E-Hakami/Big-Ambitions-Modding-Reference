using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixApartmentsHallwaysIds : ICompatibilityFix
{
	private const string AffectedBuildingSize = "ba:buildingsize_n";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && buildingRegistration.BuildingCached.BuildingType == "ba:buildingtype_residential" && buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_n")
			{
				ApplyFixesForN1Hallway(buildingRegistration);
			}
		}
	}

	private void ApplyFixesForN1Hallway(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "kXTMZBHV8EuSxQsp1IeZgA==", 1, "7AHItxKA6E+uqPDH4gLDDg==", 2);
		ReplaceDesign(registration, "wcKDr3e8XEyvItVDZsWhVw==", 1, "ykrVq3Z0pUyYmdQ+ol34eQ==", 2);
		ReplaceDesign(registration, "Dv+W1wj0zkqGNQZ5fxnJzA==", 0, "PgyiQTgAPEqUse+qOGsWzg==", 2, removeOldDesign: false);
		ReplaceDesign(registration, "Dv+W1wj0zkqGNQZ5fxnJzA==", 0, "3bweGMfnyEOEAicCArAQ6g==", 2, removeOldDesign: false);
		ReplaceDesign(registration, "Dv+W1wj0zkqGNQZ5fxnJzA==", 0, "PFM05zSsNEeQhQ5udAp+8w==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "Dv+W1wj0zkqGNQZ5fxnJzA==", 0, "yUOwg6E02YL+IrUaSIEA==", 2);
	}

	private static void ReplaceDesign(BuildingRegistration registration, string oldDesignId, int oldMaterialIndex, string newDesignId, int newMaterialIndex, bool removeOldDesign = true)
	{
		SerializedInteriorDesign serializedInteriorDesign = registration.interiorDesigns.FirstOrDefault((SerializedInteriorDesign x) => x.UUID == oldDesignId);
		if (serializedInteriorDesign != null)
		{
			SerializedInteriorDesign.SerializableInteriorMaterial serializableInteriorMaterial = serializedInteriorDesign.materials.FirstOrDefault((SerializedInteriorDesign.SerializableInteriorMaterial x) => x.MaterialIndex == oldMaterialIndex);
			SerializedInteriorDesign serializedInteriorDesign2 = new SerializedInteriorDesign();
			serializedInteriorDesign2.UUID = newDesignId;
			serializedInteriorDesign2.materials = new SerializedInteriorDesign.SerializableInteriorMaterial[1]
			{
				new SerializedInteriorDesign.SerializableInteriorMaterial
				{
					MaterialID = serializableInteriorMaterial.MaterialID,
					MaterialIndex = newMaterialIndex,
					ColorIndex = serializableInteriorMaterial.ColorIndex
				}
			};
			SerializedInteriorDesign item = serializedInteriorDesign2;
			if (removeOldDesign)
			{
				registration.interiorDesigns.Remove(serializedInteriorDesign);
			}
			registration.interiorDesigns.Add(item);
		}
	}
}
