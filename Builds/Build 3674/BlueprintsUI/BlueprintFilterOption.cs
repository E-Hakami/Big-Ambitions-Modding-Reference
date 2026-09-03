namespace BlueprintsUI;

public class BlueprintFilterOption
{
	public string text;

	public bool localizeText;

	public bool toggled;

	public virtual string Tag => string.Empty;

	protected BlueprintFilterOption(bool toggled)
	{
		this.toggled = toggled;
	}
}
