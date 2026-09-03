using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using BusinessLayoutSets;
using Extensions;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class PopulateRivals : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in RivalsHelper.IgnoredAddresses.Select(BuildingHelper.GetBuildingRegistration))
		{
			item.buildingOwnerRivalId = string.Empty;
			item.businessOwnerRivalId = string.Empty;
		}
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			HandleBusinessDistribution(neighborhood);
		}
		CityGenerator.DistributeBuildingsToRivals();
	}

	private void HandleBusinessDistribution(string neighborhood)
	{
		SpecialRival specialRivalByNeighborhood = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		List<BuildingRegistration> list = (from x in BuildingHelper.allBuildings.Where(delegate(Building x)
			{
				if (x.Neighbourhood == neighborhood)
				{
					string buildingType = x.BuildingType;
					return buildingType == "ba:buildingtype_retail" || buildingType == "ba:buildingtype_office";
				}
				return false;
			})
			orderby x.GetCustomerCapacity descending
			select x.GetRegistration() into x
			where !x.RentedByPlayer && !x.AvailableForRent && BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.allowplayercreation) && !RivalsHelper.IgnoredAddresses.Contains(x.Address)
			select x).ToList();
		int count = Mathf.RoundToInt((float)list.Count * 0.4f) + 1;
		List<BuildingRegistration> list2 = list.Take(Mathf.RoundToInt((float)list.Count * 0.6f) + 1).ToList().Shuffle()
			.Take(count)
			.ToList();
		foreach (BuildingRegistration item in list)
		{
			Building building = BuildingHelper.GetBuilding(item.Address);
			BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(item.businessTypeName, new BuildingSizeInfo(building), item.Layout);
			if (orLoadBusinessLayoutSet != null)
			{
				SpecialRival rival = (list2.Contains(item) ? specialRivalByNeighborhood : null);
				AiBusinessDefault rivalBusinessDefault = RivalsHelper.GetRivalBusinessDefault(orLoadBusinessLayoutSet, rival);
				string text = rivalBusinessDefault.corporationRivalId;
				if (string.IsNullOrEmpty(text))
				{
					text = RivalsHelper.GetNonSpecialRivals().GetRandom().id;
				}
				else if (text == "*")
				{
					text = RivalsHelper.GetRandomSpecialRivalId(canFallbackToImport: true, canFallbackToWholesale: false);
				}
				CompetitionHelper.StartNewCompetitorBusiness(orLoadBusinessLayoutSet.BusinessType, item, impactMarket: false, rivalBusinessDefault, text);
			}
		}
	}
}
