using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using UnityEngine;

namespace BigAmbitions.Rivals;

[Serializable]
public class RivalData
{
	public string id;

	public string rivalName;

	public Gender gender;

	public int startingAgeInYears;

	[NonSerialized]
	public List<BuildingRegistration> ownedBuildings;

	[NonSerialized]
	public List<BuildingRegistration> ownedBusinesses;

	[NonSerialized]
	public List<BuildingRegistration> ownedRetailOfficeBusinesses;

	public float WeeklyIncome
	{
		get
		{
			if (ownedRetailOfficeBusinesses.Where(delegate(BuildingRegistration x)
			{
				string buildingType = x.GetBuildingType();
				return buildingType == "ba:buildingtype_office" || buildingType == "ba:buildingtype_retail";
			}).All((BuildingRegistration x) => x.dailyIncomes != null))
			{
				return ownedRetailOfficeBusinesses.Sum((BuildingRegistration x) => x.dailyIncomes.TakeLast(7).Sum());
			}
			return 0f;
		}
	}

	public string MostActiveNeighborhood
	{
		get
		{
			List<BuildingRegistration> list = ownedRetailOfficeBusinesses;
			if (list != null && list.Count > 0)
			{
				return (from b in ownedBusinesses
					group b by b.Neighborhood into g
					select new
					{
						Neighbourhood = g.Key,
						TotalIncome = g.Sum((BuildingRegistration b) => b.dailyIncomes.TakeLast(7).Sum())
					} into g
					orderby g.TotalIncome descending
					select g).First().Neighbourhood;
			}
			Debug.LogWarning("No owned businesses found for rival " + rivalName);
			return string.Empty;
		}
	}
}
