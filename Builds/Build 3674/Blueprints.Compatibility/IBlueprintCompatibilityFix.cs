namespace Blueprints.Compatibility;

public interface IBlueprintCompatibilityFix
{
	void Apply(Blueprint blueprint, BusinessLayoutSet layout, CompatibilityFixScope scope);
}
