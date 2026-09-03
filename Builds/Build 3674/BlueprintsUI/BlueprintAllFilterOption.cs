namespace BlueprintsUI;

public class BlueprintAllFilterOption : BlueprintFilterOption
{
	public BlueprintAllFilterOption()
		: base(toggled: true)
	{
		text = "common_all";
		localizeText = true;
	}
}
