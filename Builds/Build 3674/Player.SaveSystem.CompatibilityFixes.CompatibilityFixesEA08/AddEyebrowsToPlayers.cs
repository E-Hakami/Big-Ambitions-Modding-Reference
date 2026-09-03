using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class AddEyebrowsToPlayers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CharacterData characterData = gameInstance.charactersData[0];
		if (!characterData.elements.Any((AppearanceElementData x) => x.type == AppearanceElementType.Eyebrows))
		{
			string text = characterData.elements.First((AppearanceElementData x) => x.type == AppearanceElementType.Hair).colorId;
			if (string.IsNullOrEmpty(text))
			{
				text = ((characterData.gender == Gender.Male) ? "Y9dij6dz2k6wbVesVl7txg==" : "VR7Y1sqDU6T59WMSnIzw==");
			}
			characterData.elements.Add(new AppearanceElementData
			{
				type = AppearanceElementType.Eyebrows,
				variantId = ((characterData.gender == Gender.Male) ? "9NaE12Not0Wnsqp+kCthLQ==" : "S1Ks0LfkEUOn145Cl7hqg=="),
				colorId = text
			});
		}
	}
}
