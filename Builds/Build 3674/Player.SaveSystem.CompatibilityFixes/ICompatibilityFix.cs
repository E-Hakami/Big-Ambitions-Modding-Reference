namespace Player.SaveSystem.CompatibilityFixes;

public interface ICompatibilityFix
{
	bool Priority => false;

	void Apply(GameInstance gameInstance);
}
