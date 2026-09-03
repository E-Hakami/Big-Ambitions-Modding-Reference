using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class AddStorageRoomAndBathroomToD2 : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				ReplaceDesign(buildingRegistration, "cCH5aqpwDEKqAMhvKEP4Wg==", 1, "fVKWl0uVkydNjJCxYqqsg==", 1);
				ReplaceDesign(buildingRegistration, "fot55z6T+0KQCz7rqer8Tw==", 1, "5WS8454tsESyEcaqWD4pBw==", 2);
				ReplaceDesign(buildingRegistration, "W8A277m2kUSpHQYNaoFFiw==", 1, "4dPPr7RNSEySwcfKPMFOFA==", 2);
				ReplaceDesign(buildingRegistration, "HLXywJSA0Wp8xLGKADRgA==", 1, "lKClySDvfE+4P6tcAAdkUA==", 2);
				ReplaceDesign(buildingRegistration, "mmqSRlAXvkiZ5HDVlbZeHA==", 1, "Kwrfp1YC2kK9IgKuay9YDw==", 2);
				ReplaceDesign(buildingRegistration, "yGLb6yVvyk2Eg3Dq2nCQ==", 1, "PpEr1PImw0u3wOm+gvHElA==", 2, removeOldDesign: false);
				ReplaceDesign(buildingRegistration, "yGLb6yVvyk2Eg3Dq2nCQ==", 1, "2HmfPXmMWU2BrZfJfKrc6w==", 1);
				ReplaceDesign(buildingRegistration, "SOL+sgtU2mdlJlKOIyUg==", 1, "5hDpOE7isE6yytzdOmUeYg==", 2);
				ReplaceDesign(buildingRegistration, "7VP2CvXFeky0Pk6JaCiqw==", 1, "5+bEVagHK06FHqPsWoMxA==", 2);
				ReplaceDesign(buildingRegistration, "pyUYWvbwQkuFU9IxpxyAUw==", 1, "3Ba+a0uwX0a5AURZHFxbbg==", 2);
				ReplaceDesign(buildingRegistration, "Yjck8HaBkiuBDT+sjnkJQ==", 1, "KgSLgS600+OmLrn2DwHog==", 2);
				ReplaceDesign(buildingRegistration, "NDGSlB52TE6pfNHlOmUweA==", 1, "MfKnXHAW8UWdk4rjJ3qYyg==", 2);
				ReplaceDesign(buildingRegistration, "OgHpsPzI+0SvrIGey73A7g==", 1, "0zDi4NJww0WYUkFq8WPJpw==", 2);
				ReplaceDesign(buildingRegistration, "pFghtWgoC0qBiEcNKOjhAg==", 1, "6cujawhHzECmahEeOgQgKQ==", 2);
				ReplaceDesign(buildingRegistration, "X1F8AkxM60+hWsUiqODGgw==", 1, "rGu9u7CgzE6JUu533Y3PgA==", 2);
				ReplaceDesign(buildingRegistration, "QSuZ1UND602GP++0hclWQ==", 1, "Rdzi9ZmopE2zy0AW5gColQ==", 2);
				ReplaceDesign(buildingRegistration, "6gP4slyvB0um7GEmS0By5g==", 1, "eigkZ9Qog0GKvbtj3nUtw==", 2);
				ReplaceDesign(buildingRegistration, "85M7YOXE+EGUp7MRLd56pQ==", 1, "BiLgXUan6EazVnARsP97PQ==", 1);
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
