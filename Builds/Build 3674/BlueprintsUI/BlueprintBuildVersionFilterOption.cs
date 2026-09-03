namespace BlueprintsUI;

public class BlueprintBuildVersionFilterOption : BlueprintFilterOption
{
	public string Version { get; }

	public BlueprintBuildVersionFilterOption(string version)
		: base(toggled: false)
	{
		text = version;
		localizeText = false;
		Version = version;
	}

	public bool IsMatch(string version)
	{
		return Version == version;
	}
}
