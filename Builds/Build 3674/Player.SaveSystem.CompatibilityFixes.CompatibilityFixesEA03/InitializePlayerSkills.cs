using System.Linq;
using BigAmbitions.Characters.Skills;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class InitializePlayerSkills : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (!gameInstance.charactersData.First().skills.Exists((Skill x) => x.name == "ba:skill_customerservice"))
		{
			gameInstance.charactersData.First().skills.Add(new Skill
			{
				name = "ba:skill_customerservice",
				value = 50f
			});
		}
	}
}
