namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixMaterialIDNotFound : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			foreach (SerializedInteriorDesign interiorDesign in buildingRegistration.interiorDesigns)
			{
				for (int i = 0; i < interiorDesign.materials.Length; i++)
				{
					if (interiorDesign.materials[i].MaterialID == "N25JMY7Yg0uODNW57o+GXw==")
					{
						interiorDesign.materials[i] = new SerializedInteriorDesign.SerializableInteriorMaterial
						{
							MaterialID = "t7Z9gwH5vkqpPuhXUkzWw==",
							MaterialIndex = interiorDesign.materials[i].MaterialIndex,
							ColorIndex = 0
						};
					}
				}
			}
		}
	}
}
