using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Buildings;

public class BuildingStructureController : MonoBehaviour
{
	private const string NavMeshesPath = "Assets/Prefabs/BuildingStructures/";

	[SerializeField]
	[AutocompleteDropdown("BuildingSizes")]
	private string buildingSize;

	[SerializeField]
	private int version;

	public int Version => version;

	public void GetWallHeights(List<float> wallHeights)
	{
		float[] wallHeights2 = BuildingSizeHelper.GetData(buildingSize).wallHeights;
		wallHeights.Clear();
		wallHeights.AddRange(wallHeights2);
	}
}
