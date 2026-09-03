using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using Newtonsoft.Json;
using UnityEngine;

namespace AI.Citizens;

[Serializable]
public class CitizenData
{
	public string NationalID;

	public string Name;

	public int Age;

	public string Neighbourhood;

	public SocialClass SocialClass;

	public Gender Gender;

	[JsonProperty]
	private List<string> _demands;

	public AppearanceTag[] AppearanceTags => CitizenHelper.AppearanceTagsForSocialClass(SocialClass);

	public bool IsPriceAcceptable(string itemName, float price)
	{
		if (price <= 0f)
		{
			return true;
		}
		return price <= ItemHelper.CalculateMaxAcceptablePrice(itemName, Neighbourhood, SocialClass);
	}

	public List<string> GetDemands()
	{
		List<string> demands = _demands;
		if (demands != null && demands.Count > 0)
		{
			return _demands;
		}
		IEnumerable<IGrouping<string, BuildingRegistration>> currentBusinessTypes = from b in BuildingHelper.allBuildings
			where b.Neighbourhood == Neighbourhood
			select BuildingHelper.GetBuildingRegistration(b.Address) into x
			group x by x.businessTypeName;
		IOrderedEnumerable<BusinessType> source = from x in BusinessTypeHelper.GetAllPlayerAvailableBusinesses()
			where x.HasTag(TagRef.Businesstag.generatesrevenue)
			orderby currentBusinessTypes.FirstOrDefault((IGrouping<string, BuildingRegistration> c) => c.Key == x.businessTypeName)?.Count()
			select x;
		_demands = (from x in source.Take(UnityEngine.Random.Range(1, 3))
			select x.businessTypeName).ToList();
		return _demands;
	}
}
