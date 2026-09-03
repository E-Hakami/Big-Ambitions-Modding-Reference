using BigAmbitions.Characters.Appearance;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixEmployeeUniformPresetsHaveHeadsAndHairs : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeePreset employeePreset in gameInstance.employeePresets)
		{
			employeePreset.maleElements.RemoveAll(delegate(AppearanceElementData x)
			{
				AppearanceElementType type = x.type;
				return type == AppearanceElementType.Head || type == AppearanceElementType.Hair;
			});
			employeePreset.femaleElements.RemoveAll(delegate(AppearanceElementData x)
			{
				AppearanceElementType type = x.type;
				return type == AppearanceElementType.Head || type == AppearanceElementType.Hair;
			});
		}
	}
}
