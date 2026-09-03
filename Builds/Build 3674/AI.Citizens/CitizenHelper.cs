using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Neighborhoods;
using Extensions;
using UnityEngine;

namespace AI.Citizens;

public static class CitizenHelper
{
	private const int WorkingClassPriceIndex = 120;

	private const int MiddleClassPriceIndex = 140;

	private const int UpperClassPriceIndex = 170;

	public static readonly Dictionary<string, float> averagePriceIndicesInNeighborhoods = new Dictionary<string, float>();

	private static readonly CitizenData TempCitizenData = new CitizenData();

	private static readonly Dictionary<string, List<(SocialClass, float)>> SocialClassesShares = new Dictionary<string, List<(SocialClass, float)>>();

	private static bool Initialized;

	public static float MaxAcceptableRelativePrice(SocialClass socialClass, string neighborhood)
	{
		return Mathf.Max((float)(socialClass switch
		{
			SocialClass.Working => 120, 
			SocialClass.Middle => 140, 
			SocialClass.Upper => 170, 
			_ => 0, 
		}) * 0.01f, averagePriceIndicesInNeighborhoods[neighborhood]);
	}

	public static AppearanceTag[] AppearanceTagsForSocialClass(SocialClass socialClass)
	{
		return socialClass switch
		{
			SocialClass.Working => new AppearanceTag[1] { AppearanceTag.Casual }, 
			SocialClass.Middle => new AppearanceTag[1] { AppearanceTag.Casual }, 
			SocialClass.Upper => new AppearanceTag[1] { AppearanceTag.Formal }, 
			_ => new AppearanceTag[1] { AppearanceTag.Casual }, 
		};
	}

	public static void Init()
	{
		if (Initialized)
		{
			return;
		}
		Initialized = true;
		NeighborhoodData[] neighborhoodsData = NeighborhoodHelper.NeighborhoodsData;
		foreach (NeighborhoodData neighborhoodData in neighborhoodsData)
		{
			List<(SocialClass, float)> value = new List<(SocialClass, float)>
			{
				(SocialClass.Working, neighborhoodData.workingClassPercentage),
				(SocialClass.Middle, neighborhoodData.middleClassPercentage),
				(SocialClass.Upper, neighborhoodData.upperClassClassPercentage)
			}.OrderByDescending(((SocialClass, float) x) => x.Item2).ToList();
			SocialClassesShares.Add(neighborhoodData.neighbourhood, value);
			float value2 = (1.1999999f * (float)neighborhoodData.workingClassPercentage + 1.4f * (float)neighborhoodData.middleClassPercentage + 1.6999999f * (float)neighborhoodData.upperClassClassPercentage) / (float)(neighborhoodData.workingClassPercentage + neighborhoodData.middleClassPercentage + neighborhoodData.upperClassClassPercentage);
			averagePriceIndicesInNeighborhoods.Add(neighborhoodData.neighbourhood, value2);
		}
	}

	public static CitizenData CreateRandomCitizenDataFromNeighborhood(string neighbourhood)
	{
		CitizenData citizenData = new CitizenData();
		RandomizeCitizenData(citizenData, neighbourhood);
		return citizenData;
	}

	public static CitizenData CreateRandomCitizenDataNonAlloc(string neighbourhood)
	{
		RandomizeCitizenData(TempCitizenData, neighbourhood);
		return TempCitizenData;
	}

	private static void RandomizeCitizenData(CitizenData citizenData, string neighbourhood)
	{
		CitizenDataExample random = InstanceBehavior<GlobalReferences>.Instance.dataExamples.examples.GetRandom();
		citizenData.Age = AppearanceAgeHelper.GetRandomAgeInDays();
		citizenData.Gender = random.gender;
		citizenData.Name = random.name + " " + random.middleInitial + " " + random.surname;
		citizenData.NationalID = random.nationalID;
		citizenData.Neighbourhood = neighbourhood;
		citizenData.SocialClass = GetRandomSocialClassInNeighborhood(neighbourhood);
	}

	private static SocialClass GetRandomSocialClassInNeighborhood(string neighbourhood)
	{
		List<(SocialClass, float)> list = SocialClassesShares[neighbourhood];
		foreach (var item in list)
		{
			if ((float)Random.Range(0, 100) <= item.Item2)
			{
				return item.Item1;
			}
		}
		return list.First().Item1;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Initialized = false;
		averagePriceIndicesInNeighborhoods.Clear();
		SocialClassesShares.Clear();
	}
}
