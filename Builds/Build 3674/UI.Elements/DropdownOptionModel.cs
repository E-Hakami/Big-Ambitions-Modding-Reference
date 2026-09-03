namespace UI.Elements;

public class DropdownOptionModel
{
	public string option;

	public int optionId;

	public bool selected;

	public DropdownOptionModel(string option, int optionId, bool selected)
	{
		this.option = option;
		this.optionId = optionId;
		this.selected = selected;
	}
}
