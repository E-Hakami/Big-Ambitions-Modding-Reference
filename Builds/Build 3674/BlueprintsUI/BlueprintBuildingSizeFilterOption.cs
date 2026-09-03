using Blueprints;

namespace BlueprintsUI;

public class BlueprintBuildingSizeFilterOption : BlueprintFilterOption
{
	private readonly BuildingSizeInfo _buildingSizeInfo;

	public override string Tag => _buildingSizeInfo.ToString();

	public BlueprintBuildingSizeFilterOption(BuildingSizeInfo buildingSizeInfo)
		: base(toggled: false)
	{
		text = buildingSizeInfo.ToString();
		localizeText = false;
		_buildingSizeInfo = buildingSizeInfo;
	}
}
