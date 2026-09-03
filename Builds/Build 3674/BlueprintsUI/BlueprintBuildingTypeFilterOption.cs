using BigAmbitions.SaveSystem;

namespace BlueprintsUI;

public class BlueprintBuildingTypeFilterOption : BlueprintFilterOption
{
	private readonly string _buildingType;

	public override string Tag => _buildingType.GetIdWithoutType();

	public BlueprintBuildingTypeFilterOption(string buildingType)
		: base(toggled: false)
	{
		text = buildingType;
		localizeText = true;
		_buildingType = buildingType;
	}
}
