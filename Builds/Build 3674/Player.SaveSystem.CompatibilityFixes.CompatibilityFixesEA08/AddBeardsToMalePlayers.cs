using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class AddBeardsToMalePlayers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CharacterData characterData = gameInstance.charactersData[0];
		if (characterData.gender != Gender.Female && !characterData.elements.Any((AppearanceElementData x) => x.type == AppearanceElementType.Beard))
		{
			characterData.elements.Add(new AppearanceElementData
			{
				type = AppearanceElementType.Beard,
				variantId = "6n3ywYOkkkStmwrm8ikfQ==",
				colorId = null
			});
		}
	}
}
