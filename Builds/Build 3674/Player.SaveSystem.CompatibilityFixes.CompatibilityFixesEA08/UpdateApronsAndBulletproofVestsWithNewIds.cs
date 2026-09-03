using System.Linq;
using BigAmbitions.Characters.Appearance;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateApronsAndBulletproofVestsWithNewIds : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeePreset employeePreset in gameInstance.employeePresets)
		{
			if (employeePreset.maleElements.Any((AppearanceElementData x) => x.variantId == "H+7+fJiz+0ieC6MX4AN2ZQ==" || x.variantId == "XAyf+NgW8Ua+Ocem36u9KQ==" || x.variantId == "6rZMEEdkTk6SWDEWA+HKkw==" || x.variantId == "e+KQ3wrzrkWphDE368msg==" || x.variantId == "oJkdIF17CkqDfxBYK0Jdsw=="))
			{
				employeePreset.maleElements.RemoveAll((AppearanceElementData x) => x.type == AppearanceElementType.Torso);
				foreach (AppearanceElementData maleElement in employeePreset.maleElements)
				{
					if (maleElement.variantId == "H+7+fJiz+0ieC6MX4AN2ZQ==")
					{
						maleElement.type = AppearanceElementType.Torso;
					}
					else if (maleElement.variantId == "XAyf+NgW8Ua+Ocem36u9KQ==")
					{
						maleElement.variantId = "H+7+fJiz+0ieC6MX4AN2ZQ==";
						maleElement.colorId = "Nv6Yr5JvrkSznsTeL7r+Ww==";
						maleElement.type = AppearanceElementType.Torso;
					}
					else if (maleElement.variantId == "6rZMEEdkTk6SWDEWA+HKkw==")
					{
						maleElement.type = AppearanceElementType.Torso;
					}
					else if (maleElement.variantId == "e+KQ3wrzrkWphDE368msg==")
					{
						maleElement.variantId = "6rZMEEdkTk6SWDEWA+HKkw==";
						maleElement.colorId = "Nv6Yr5JvrkSznsTeL7r+Ww==";
						maleElement.type = AppearanceElementType.Torso;
					}
					else if (maleElement.variantId == "oJkdIF17CkqDfxBYK0Jdsw==")
					{
						maleElement.type = AppearanceElementType.Torso;
					}
				}
				employeePreset.maleElements.Add(new AppearanceElementData
				{
					type = AppearanceElementType.TorsoAccessory,
					variantId = "p1nrsYXGtk+i2dnDcjMZPQ=="
				});
			}
			if (!employeePreset.femaleElements.Any((AppearanceElementData x) => x.variantId == "CUzkdkel+kKxSvf7gqOHQ==" || x.variantId == "wQxBw05PhUyzYTLPFnfQDg==" || x.variantId == "yXe2FmtZpE6IqTvB9Iipw==" || x.variantId == "yVvPc0UZJkuW0oCbi1ZXVw==" || x.variantId == "2bqcYQ1bbU6sWaoN8jFv0A=="))
			{
				continue;
			}
			employeePreset.femaleElements.RemoveAll((AppearanceElementData x) => x.type == AppearanceElementType.Torso);
			foreach (AppearanceElementData femaleElement in employeePreset.femaleElements)
			{
				if (femaleElement.variantId == "CUzkdkel+kKxSvf7gqOHQ==")
				{
					femaleElement.type = AppearanceElementType.Torso;
				}
				else if (femaleElement.variantId == "wQxBw05PhUyzYTLPFnfQDg==")
				{
					femaleElement.variantId = "CUzkdkel+kKxSvf7gqOHQ==";
					femaleElement.colorId = "Nv6Yr5JvrkSznsTeL7r+Ww==";
					femaleElement.type = AppearanceElementType.Torso;
				}
				else if (femaleElement.variantId == "yXe2FmtZpE6IqTvB9Iipw==")
				{
					femaleElement.type = AppearanceElementType.Torso;
				}
				else if (femaleElement.variantId == "yVvPc0UZJkuW0oCbi1ZXVw==")
				{
					femaleElement.variantId = "yXe2FmtZpE6IqTvB9Iipw==";
					femaleElement.colorId = "Nv6Yr5JvrkSznsTeL7r+Ww==";
					femaleElement.type = AppearanceElementType.Torso;
				}
				else if (femaleElement.variantId == "2bqcYQ1bbU6sWaoN8jFv0A==")
				{
					femaleElement.type = AppearanceElementType.Torso;
				}
			}
			employeePreset.femaleElements.Add(new AppearanceElementData
			{
				type = AppearanceElementType.TorsoAccessory,
				variantId = "JpBIPAjEUuMKWcT9OrhPw=="
			});
		}
	}
}
