using System.Collections.Generic;
using AI.Citizens;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using Character;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class UpdateCharacterDataToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		(Gender, string) randomGenderAndName = RecruitmentHelper.GetRandomGenderAndName();
		Gender item = randomGenderAndName.Item1;
		string item2 = randomGenderAndName.Item2;
		List<AppearanceElementData> elements = new List<AppearanceElementData>
		{
			new AppearanceElementData
			{
				type = AppearanceElementType.Hair,
				variantId = ((item == Gender.Male) ? "ssogR8NlrUi0YJOGFuhKNA==" : "ZL42dyyKp0mdo3tKrirHHQ=="),
				colorId = ((item == Gender.Male) ? "yUe0zjDfIkevcmz5sqsCdw==" : "KVyEj8IsZkmR8eiIXZ31UA==")
			},
			new AppearanceElementData
			{
				type = AppearanceElementType.Head,
				variantId = ((item == Gender.Male) ? "Okk+5M6Ga0W7eX114kJhkw==" : "yM1a53DXl0S0OxpUNEC6QA==")
			},
			new AppearanceElementData
			{
				type = AppearanceElementType.Torso,
				variantId = ((item == Gender.Male) ? "WKxDUBMWnE+2FukZKwg1Uw==" : "wbaAqv0GUi6dQxcUmVclA=="),
				colorId = "BBdvINrlh0a9Bq6BfNezA=="
			},
			new AppearanceElementData
			{
				type = AppearanceElementType.Legs,
				variantId = ((item == Gender.Male) ? "6oJqKVwfz0Sfy9hlkrv9Ng==" : "Pz+iOgGGmkye2vgZfTZCSA=="),
				colorId = ((item == Gender.Male) ? "sxohgVvsWkCikOYrPprEQQ==" : "fk5hIw6Dp0WK5Mi7JlhK5g==")
			},
			new AppearanceElementData
			{
				type = AppearanceElementType.Feet,
				variantId = ((item == Gender.Male) ? "Fag3FAvz4UiiFkVlfIBXlA==" : "+gILudhT6UOR3FRtwu+l4g=="),
				colorId = ((item == Gender.Male) ? "Ddq82BWfC0Go3D4TrTN2Zw==" : "eLGVuDPrGkiVAbM07WUmEA==")
			}
		};
		gameInstance.charactersData = new List<CharacterData>
		{
			new CharacterData
			{
				name = item2,
				ageInDays = 1080 + gameInstance.Day,
				gender = item,
				color = SkinColorHelper.GetRandom().color,
				strength = 0.5f,
				fatness = 0.5f,
				elements = elements,
				skills = new List<Skill>
				{
					new Skill
					{
						name = "ba:skill_customerservice",
						value = 50f
					}
				}
			}
		};
		foreach (EmployeePreset employeePreset in gameInstance.employeePresets)
		{
			employeePreset.femaleElements = new List<AppearanceElementData>();
			employeePreset.maleElements = new List<AppearanceElementData>();
		}
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			(Gender, string) randomGenderAndName2 = RecruitmentHelper.GetRandomGenderAndName();
			item = randomGenderAndName2.Item1;
			item2 = randomGenderAndName2.Item2;
			employeeInstance.characterData = new CharacterData
			{
				name = item2,
				ageInDays = 1080 + gameInstance.Day,
				gender = item,
				color = SkinColorHelper.GetRandom().color,
				strength = 0.5f,
				fatness = 0.5f,
				elements = new List<AppearanceElementData>(),
				skills = employeeInstance.skills
			};
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			(Gender, string) randomGenderAndName3 = RecruitmentHelper.GetRandomGenderAndName();
			item = randomGenderAndName3.Item1;
			item2 = randomGenderAndName3.Item2;
			candidateEmployeeInstance.characterData = new CharacterData
			{
				name = item2,
				ageInDays = 1080 + gameInstance.Day,
				gender = item,
				skills = candidateEmployeeInstance.skills
			};
		}
	}
}
