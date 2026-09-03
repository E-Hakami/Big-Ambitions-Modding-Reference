using BigAmbitions.SaveSystem;
using Localizor;

namespace BlueprintsUI;

public class BlueprintBusinessTypeFilterOption : BlueprintFilterOption
{
	private readonly string _businessTypeName;

	public override string Tag => _businessTypeName.GetIdWithoutType();

	public BlueprintBusinessTypeFilterOption(string businessTypeName)
		: base(toggled: false)
	{
		text = businessTypeName.GetLocalization();
		localizeText = false;
		_businessTypeName = businessTypeName;
	}
}
