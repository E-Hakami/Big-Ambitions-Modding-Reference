using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class AddStorageRoomAndBathroomToJ1 : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				ReplaceDesign(buildingRegistration, "mYNtGAQh4UWI0SXrQ6FyrQ==", 1, "ZHGl7zfJgUuFmdcTysky1g==", 1);
				ReplaceDesign(buildingRegistration, "utXMrzWcU2U2aD9J4Nkug==", 1, "CD7WLfmAkK+gtW2lUqddQ==", 2);
				ReplaceDesign(buildingRegistration, "z7EGMtg2aUmuGJF3ULJbfg==", 1, "9X0YR8JaXkivlAWOT+rwAQ==", 2);
				ReplaceDesign(buildingRegistration, "upUcpJmZ0kyHMJZM731ChA==", 1, "1fgUg4MZok+JBm4SypLHjw==", 2);
				ReplaceDesign(buildingRegistration, "ytVvwaeBMEqgmzXBLXCDpA==", 1, "8C4YbJoqkCHhphTwYOARw==", 2);
				ReplaceDesign(buildingRegistration, "VMykL6V6gEGktDtEyqOjzA==", 1, "evdSethOT0ySBnsAnw4j+w==", 2);
				ReplaceDesign(buildingRegistration, "Y4zb3bcr7E+b4LtOiyN0cg==", 1, "2yPOUsJuC0qx0F4c8t6gxA==", 2, removeOldDesign: false);
				ReplaceDesign(buildingRegistration, "Y4zb3bcr7E+b4LtOiyN0cg==", 1, "P0QRedIYzUyz8iqlHutexQ==", 1);
				ReplaceDesign(buildingRegistration, "ZgUIDwvn4EGi9vsHDCzQg==", 1, "VzXJm3C2T0K9DY7MyuSSGA==", 2);
				ReplaceDesign(buildingRegistration, "F0zhzAe+M0yYUfS1cIAULw==", 1, "Y+PZfSIUDU29eqAX1u4wQ==", 2);
				ReplaceDesign(buildingRegistration, "niWP3yRcEWA1czDuX6glQ==", 1, "ftt54yg6zkmO2nAteKH4YA==", 2);
				ReplaceDesign(buildingRegistration, "l7iG00pupkS6SCM5hSqGpg==", 1, "EjRc7Mz0m4ypzKtHjRbw==", 2);
				ReplaceDesign(buildingRegistration, "+ePun7nwck2SUGz94o1Wkw==", 1, "yz9DwNskO0CoGQkqj1sepw==", 2);
				ReplaceDesign(buildingRegistration, "4PzJmXNt8UGeLGUf9VZEg==", 1, "6cujawhHzECmahEeOgQgKQ==", 2);
				ReplaceDesign(buildingRegistration, "CZg1ksw0IkCQIBLLQFCkg==", 1, "+2aH1Qp1pk6dtO6RJoFuA==", 2);
				ReplaceDesign(buildingRegistration, "1AvIQCl5Q0yyil+yLWcCg==", 1, "GJsTtNwlMUG+djd7qNV+eQ==", 2);
				ReplaceDesign(buildingRegistration, "VDO1Wt1uTE+7KCExLZZRSQ==", 1, "fcW7HRqN0SNpL0gS5zXLw==", 2);
				ReplaceDesign(buildingRegistration, "Xm41MjgBGUulBl3ebjxog==", 1, "IEib9I0I0UKDFBvLGCdiw==", 1);
			}
		}
	}

	private static void ReplaceDesign(BuildingRegistration registration, string oldDesignId, int oldMaterialIndex, string newDesignId, int newMaterialIndex, bool removeOldDesign = true)
	{
		SerializedInteriorDesign serializedInteriorDesign = registration.interiorDesigns.FirstOrDefault((SerializedInteriorDesign x) => x.UUID == oldDesignId);
		if (serializedInteriorDesign == null)
		{
			return;
		}
		if (!serializedInteriorDesign.materials.Any((SerializedInteriorDesign.SerializableInteriorMaterial x) => x.MaterialIndex == oldMaterialIndex))
		{
			if (removeOldDesign)
			{
				registration.interiorDesigns.Remove(serializedInteriorDesign);
			}
			return;
		}
		SerializedInteriorDesign.SerializableInteriorMaterial serializableInteriorMaterial = serializedInteriorDesign.materials.First((SerializedInteriorDesign.SerializableInteriorMaterial x) => x.MaterialIndex == oldMaterialIndex);
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
