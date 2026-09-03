using System.Collections.Generic;
using Entities;
using Extensions;
using UI.Apps.Contacts;
using UnityEngine;

public class AdManager : InstanceBehavior<AdManager>
{
	private static readonly MarketingTypeName[] Types = new MarketingTypeName[3]
	{
		MarketingTypeName.LargeBillboard,
		MarketingTypeName.MediumBillboard,
		MarketingTypeName.SmallBillboard
	};

	private static readonly LogoSize[] SpecialLogoSizes = new LogoSize[5]
	{
		LogoSize.Billboard1x2,
		LogoSize.Billboard1x4,
		LogoSize.Billboard4x1,
		LogoSize.Billboard2x1,
		LogoSize.SquareSign
	};

	public float playerWeight = 1f;

	public float aiWeight = 0.5f;

	public int adsPerDay = 192;

	public int adsPerDayGenerated = 24;

	public Vector2 randomTimeRange = new Vector2(-10f, 10f);

	private readonly Dictionary<MarketingTypeName, List<AdSettings>> _adCycles = new Dictionary<MarketingTypeName, List<AdSettings>>();

	private readonly Dictionary<MarketingTypeName, List<float>> _adWeights = new Dictionary<MarketingTypeName, List<float>>();

	private readonly List<ContactPreset> _billboardPresets = new List<ContactPreset>();

	private readonly List<BuildingRegistration> _possibleRegistrations = new List<BuildingRegistration>();

	private readonly Dictionary<LogoSize, List<AdSettings>> _specialAdCycles = new Dictionary<LogoSize, List<AdSettings>>();

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			MarketingTypeName[] types = Types;
			foreach (MarketingTypeName key in types)
			{
				_adCycles.Add(key, new List<AdSettings>(adsPerDayGenerated));
				_adWeights.Add(key, new List<float>(adsPerDayGenerated));
			}
			for (int j = 0; j < SpecialLogoSizes.Length; j++)
			{
				_specialAdCycles.Add(SpecialLogoSizes[j], new List<AdSettings>());
			}
		}
	}

	private void GenerateAds()
	{
		ClearAdCycles();
		AddPlayerCampaigns();
		AddContactBillboards();
		FillPossibleRegistrations();
		FillRemainingAds();
		AddSpecialBillboards();
	}

	private void ClearAdCycles()
	{
		foreach (KeyValuePair<MarketingTypeName, List<AdSettings>> adCycle in _adCycles)
		{
			adCycle.Value.Clear();
		}
		foreach (KeyValuePair<MarketingTypeName, List<float>> adWeight in _adWeights)
		{
			adWeight.Value.Clear();
		}
		foreach (KeyValuePair<LogoSize, List<AdSettings>> specialAdCycle in _specialAdCycles)
		{
			specialAdCycle.Value.Clear();
		}
	}

	private void AddPlayerCampaigns()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName == "ba:businesstype_empty")
			{
				continue;
			}
			foreach (MarketingCampaign marketingCampaign in buildingRegistration.marketingCampaigns)
			{
				if (marketingCampaign.enabled && IsBillboardType(marketingCampaign.marketingTypeName))
				{
					AddCampaign(marketingCampaign.marketingTypeName, buildingRegistration, isPlayerAd: true);
				}
			}
		}
	}

	private void FillPossibleRegistrations()
	{
		_possibleRegistrations.Clear();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && buildingRegistration.businessTypeName != "ba:businesstype_empty" && !string.IsNullOrEmpty(buildingRegistration.BusinessName))
			{
				_possibleRegistrations.Add(buildingRegistration);
			}
		}
	}

	private void FillRemainingAds()
	{
		if (_possibleRegistrations.Count == 0)
		{
			return;
		}
		for (int i = 0; i < Types.Length; i++)
		{
			MarketingTypeName marketingTypeName = Types[i];
			List<AdSettings> list = _adCycles[marketingTypeName];
			while (list.Count < adsPerDayGenerated)
			{
				AddCampaign(marketingTypeName, _possibleRegistrations.GetRandom());
			}
		}
	}

	private void AddSpecialBillboards()
	{
		for (int i = 0; i < _possibleRegistrations.Count; i++)
		{
			BuildingRegistration buildingRegistration = _possibleRegistrations[i];
			string logoFolderPath = "BusinessLogos/" + LogoHelper.GetBusinessNamePathSafe(buildingRegistration.BusinessName);
			for (int j = 0; j < SpecialLogoSizes.Length; j++)
			{
				LogoSize logoSize = SpecialLogoSizes[j];
				if (HasLogoInFolder(logoFolderPath, logoSize))
				{
					_specialAdCycles[logoSize].Add(new AdSettings
					{
						businessName = buildingRegistration.BusinessName
					});
				}
			}
		}
	}

	private void AddCampaign(MarketingTypeName type, BuildingRegistration registration, bool isPlayerAd = false)
	{
		AddCampaign(type, registration.BusinessName, isPlayerAd);
	}

	private void AddCampaign(MarketingTypeName type, string businessName, bool isPlayerAd = false)
	{
		_adCycles[type].Add(new AdSettings
		{
			businessName = businessName,
			isPlayerAd = isPlayerAd
		});
		_adWeights[type].Add(isPlayerAd ? playerWeight : aiWeight);
	}

	private void AddContactBillboards()
	{
		ContactsHelper.FillBillboardPresets(_billboardPresets);
		for (int i = 0; i < _billboardPresets.Count; i++)
		{
			string text = _billboardPresets[i].name;
			if (!HasLogo(text, LogoSize.Billboard))
			{
				Debug.LogError(text + " claims it has a billboard logo but it's not available in BusinessLogos/" + LogoHelper.GetBusinessNamePathSafe(text));
				continue;
			}
			for (int j = 0; j < Types.Length; j++)
			{
				AddCampaign(Types[j], text);
			}
		}
	}

	private static bool HasLogo(string businessName, LogoSize logoSize)
	{
		return HasLogoKey("BusinessLogos/" + LogoHelper.GetBusinessNamePathSafe(businessName) + "/" + logoSize.ToStringFast());
	}

	private static bool HasLogoInFolder(string logoFolderPath, LogoSize logoSize)
	{
		return HasLogoKey(logoFolderPath + "/" + logoSize.ToStringFast());
	}

	private static bool HasLogoKey(string logoPath)
	{
		if (!AddressableChecksHelper.IsValidAddressableKey(logoPath + ".jpg"))
		{
			return AddressableChecksHelper.IsValidAddressableKey(logoPath + ".png");
		}
		return true;
	}

	private static bool IsBillboardType(MarketingTypeName type)
	{
		for (int i = 0; i < Types.Length; i++)
		{
			if (Types[i] == type)
			{
				return true;
			}
		}
		return false;
	}

	public void RunDaily()
	{
		GenerateAds();
	}

	public AdSettings RequestAd(MarketingTypeName marketingTypeName, out float nextExecutionTime, BillboardAd ad)
	{
		if (!_adCycles.TryGetValue(marketingTypeName, out var value))
		{
			Debug.LogWarning($"Invalid BillboardAd MarketingTypeName {marketingTypeName}", ad);
			nextExecutionTime = 0f;
			return null;
		}
		if (value.Count == 0)
		{
			GenerateAds();
		}
		nextExecutionTime = GetNextExecutionTime();
		if (value.Count != 0)
		{
			return value.GetRandomWeighted(_adWeights[marketingTypeName]);
		}
		return null;
	}

	public AdSettings RequestAd(LogoSize logoSize, out float nextExecutionTime, BillboardAd ad)
	{
		if (!_specialAdCycles.TryGetValue(logoSize, out var value))
		{
			Debug.LogWarning($"Invalid BillboardAd LogoSize {logoSize}", ad);
			nextExecutionTime = 0f;
			return null;
		}
		if (value.Count == 0)
		{
			GenerateAds();
		}
		nextExecutionTime = GetNextExecutionTime();
		if (value.Count != 0)
		{
			return value.GetRandom();
		}
		return null;
	}

	private float GetNextExecutionTime()
	{
		return TimeHelper.NowInMinutes() + 24f / (float)adsPerDay * 60f + Random.Range(randomTimeRange.x, randomTimeRange.y);
	}
}
