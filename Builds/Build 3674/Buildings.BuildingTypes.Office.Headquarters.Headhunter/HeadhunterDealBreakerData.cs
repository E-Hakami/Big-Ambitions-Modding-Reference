using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Office.Headquarters.Headhunter;

[CreateAssetMenu(fileName = "HeadhunterDealBreakerData", menuName = "BigAmbitions/Headhunter/DealBreakerData")]
public class HeadhunterDealBreakerData : ScriptableObject
{
	public string type;

	public string category;

	public int recruitmentPointCost;

	[AutocompleteDropdown("JobDemands")]
	public List<string> applicableJobDemands;
}
