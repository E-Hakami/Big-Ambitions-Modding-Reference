using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class BuildingSizeLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "BuildingSizeData.buildingSize", "BuildingSizeInfo.buildingSize", "Building.BuildingSize", "BuildingInteriorSound.buildingSizes", "BuildingEnterSound.buildingSizes", "BusinessLayoutSet.BuildingSize", "TutorialPointerHideConditionSelectedBizManBuildingHasNoCharacteristics.buildingSize", "BuildingStructureController.buildingSize" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:buildingsize_a" },
		{ 1, "ba:buildingsize_b" },
		{ 2, "ba:buildingsize_c" },
		{ 3, "ba:buildingsize_d" },
		{ 4, "ba:buildingsize_e" },
		{ 5, "ba:buildingsize_f" },
		{ 6, "ba:buildingsize_g" },
		{ 7, "ba:buildingsize_h" },
		{ 8, "ba:buildingsize_i" },
		{ 9, "ba:buildingsize_j" },
		{ 10, "ba:buildingsize_k" },
		{ 11, "ba:buildingsize_l" },
		{ 12, "ba:buildingsize_m" },
		{ 13, "ba:buildingsize_n" },
		{ 14, "ba:buildingsize_boat" },
		{ 15, "ba:buildingsize_parking" },
		{ 16, "ba:buildingsize_o" },
		{ 17, "ba:buildingsize_p" },
		{ 18, "ba:buildingsize_q" },
		{ 19, "ba:buildingsize_r" },
		{ 20, "ba:buildingsize_s" },
		{ 21, "ba:buildingsize_t" }
	};
}
