using System.Collections.Generic;
using Blueprints;
using UnityEngine;

namespace BigAmbitions.Rivals;

[CreateAssetMenu(fileName = "NewRivalsSettings", menuName = "BigAmbitions/Rivals/RivalsSettings")]
public class RivalsSettings : ScriptableObject
{
	public List<BuildingSizeInfo> specialRivalRetailBuildingSizes;

	public List<BuildingSizeInfo> specialRivalOfficeBuildingSizes;

	public List<BuildingSizeInfo> importerRivalRetailBuildingSizes;

	public List<BuildingSizeInfo> importerRivalOfficeBuildingSizes;

	public List<BuildingSizeInfo> wholesalerRivalRetailBuildingSizes;

	public List<BuildingSizeInfo> wholesalerRivalOfficeBuildingSizes;

	public List<BuildingSizeInfo> cinemaTheaterBuildingSizes;
}
