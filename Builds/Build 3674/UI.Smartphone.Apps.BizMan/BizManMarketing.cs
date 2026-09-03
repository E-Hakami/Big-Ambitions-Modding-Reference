using System.Collections.Generic;
using System.Linq;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Dialog;
using UI.Elements;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class BizManMarketing : MonoBehaviour
{
	[Header("Marketing Efficiency")]
	[SerializeField]
	private TMP_Text totalDailyExpenses;

	[SerializeField]
	private ProgressBar efficiencyProgressBar;

	[Header("Campaigns")]
	[SerializeField]
	private Transform agencyTemplate;

	[SerializeField]
	private Transform campaignList;

	[SerializeField]
	private Transform noCampaignsWarning;

	[Header("Contacts")]
	[SerializeField]
	private Transform contactEntry;

	[SerializeField]
	private Transform contactSplitterEntry;

	[SerializeField]
	private Transform contactList;

	private BizManBusiness _bizManBusiness;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
	}

	private void OnEnable()
	{
		RefreshData();
	}

	public void RefreshData()
	{
		RefreshMarketingEfficiency();
		RefreshCampaigns();
		RefreshContacts();
	}

	private void RefreshMarketingEfficiency()
	{
		totalDailyExpenses.text = _bizManBusiness.buildingRegistration.GetDailyMarketingExpenses().ToCurrencyFormat();
		efficiencyProgressBar.SetValue(_bizManBusiness.buildingRegistration.promotion.marketing);
	}

	private void RefreshCampaigns()
	{
		agencyTemplate.ResetTemplate();
		if (_bizManBusiness.buildingRegistration.marketingCampaigns.Count == 0)
		{
			noCampaignsWarning.gameObject.SetActive(value: true);
			campaignList.gameObject.SetActive(value: false);
			return;
		}
		noCampaignsWarning.gameObject.SetActive(value: false);
		campaignList.gameObject.SetActive(value: true);
		int squareMeters = BuildingSizeHelper.GetData(_bizManBusiness.buildingRegistration.BuildingCached.BuildingSize).squareMeters;
		float marketingReachMultiplier = BuildingTypeHelper.GetData(_bizManBusiness.buildingRegistration.BuildingCached.BuildingType).marketingReachMultiplier;
		foreach (IGrouping<Address, MarketingCampaign> agencyGroup in from x in _bizManBusiness.buildingRegistration.marketingCampaigns
			group x by x.agencyAddress)
		{
			List<MarketingCampaign> agencyCampaigns = agencyGroup.ToList();
			Transform entry = Object.Instantiate(agencyTemplate, agencyTemplate.parent);
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(agencyGroup.Key);
			string businessName = buildingRegistration.BusinessName;
			entry.GetLabelByName("TopPart/AgencyName").text = businessName;
			Transform transform = entry.Find("TopPart/MarketingTypeEntry");
			transform.ResetTemplate();
			foreach (MarketingTypeName marketingTypeName in (buildingRegistration.BuildingCached.SpecialService.settings as MarketingAgencySettings).marketingTypesAvailable)
			{
				MarketingTypeSettings marketingTypeSettings = MarketingTypeSettings.Get(marketingTypeName);
				Transform obj = Object.Instantiate(transform, transform.parent);
				int marketingReachPercentage = Mathf.RoundToInt(Mathf.Min(100f * (float)marketingTypeSettings.sqmReach * marketingReachMultiplier / (float)squareMeters, 100f));
				obj.GetLanguageChangeEventByName("MarketingTypeName").SetData((marketingTypeName.GetLocalizeKey() + "_info").Localize(new
				{
					marketingReachPercentage = marketingReachPercentage,
					pricePerDay = marketingTypeSettings.pricePerDay.ToShortCurrencyFormat()
				}));
				Toggle component = obj.Find("Enabled").GetComponent<Toggle>();
				component.isOn = agencyCampaigns.Any((MarketingCampaign x) => x.marketingTypeName == marketingTypeName && x.enabled);
				component.onValueChanged.AddListener(delegate(bool isEnabled)
				{
					UpdateCampaignEnabled(isEnabled, marketingTypeName, agencyGroup.Key, entry);
				});
				obj.gameObject.SetActive(value: true);
			}
			entry.GetButtonByName("CancelCampaign").onClick.AddListener(delegate
			{
				LanguageChangeEventDataHolder bodyData = "bizman_marketing_hud_confirm_cancel_campain".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					CancelAllCampaignsFromAgency(agencyCampaigns);
				});
			});
			float val = agencyCampaigns.Where((MarketingCampaign x) => x.enabled).Sum((MarketingCampaign x) => MarketingTypeSettings.Get(x.marketingTypeName).pricePerDay);
			entry.GetLabelByName("PricePerDayValue").text = val.ToCurrencyFormat();
			entry.gameObject.SetActive(value: true);
		}
	}

	private void RefreshContacts()
	{
		List<Contact> list = SaveGameManager.Current.Contacts.Where((Contact x) => x.HasAddressAssigned && BuildingHelper.GetBuildingRegistration(x.Address)?.businessTypeName == "ba:businesstype_marketingagency").ToList();
		foreach (Transform item in contactEntry.parent)
		{
			if (item != contactEntry && item != contactSplitterEntry && item.CompareTag("UiTemplate"))
			{
				Object.Destroy(item.gameObject);
			}
		}
		contactEntry.gameObject.SetActive(value: false);
		contactSplitterEntry.gameObject.SetActive(value: false);
		if (list.Count == 0)
		{
			contactList.gameObject.SetActive(value: false);
			return;
		}
		contactList.gameObject.SetActive(value: true);
		bool flag = true;
		foreach (Contact contact in list)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				Object.Instantiate(contactSplitterEntry, contactSplitterEntry.parent);
			}
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(contact.Address);
			string businessTypeName = buildingRegistration.businessTypeName;
			string businessName = buildingRegistration.BusinessName;
			Transform obj = Object.Instantiate(contactEntry, contactEntry.parent);
			obj.GetLanguageChangeEventByName("BusinessType").Key = businessTypeName;
			obj.GetLabelByName("BusinessName").text = businessName;
			obj.GetButtonByName("CallButton").onClick.AddListener(delegate
			{
				CallContact(contact);
			});
			Image imageByName = obj.GetImageByName("BusinessLogoMask/BusinessLogo");
			Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(contact.id, LogoSize.SquareSign);
			Rect rect = new Rect(0f, 0f, businessLogoTexture.width, businessLogoTexture.height);
			if (imageByName.sprite != null)
			{
				Object.Destroy(imageByName.sprite);
			}
			imageByName.sprite = Sprite.Create(businessLogoTexture, rect, new Vector2(0.5f, 0.5f));
			obj.gameObject.SetActive(value: true);
		}
	}

	private void UpdateCampaignEnabled(bool isEnabled, MarketingTypeName marketingTypeName, Address agencyAddress, Transform entry)
	{
		MarketingCampaign marketingCampaign = _bizManBusiness.buildingRegistration.marketingCampaigns.FirstOrDefault((MarketingCampaign x) => x.agencyAddress == agencyAddress && x.marketingTypeName == marketingTypeName);
		if (marketingCampaign == null)
		{
			_bizManBusiness.buildingRegistration.marketingCampaigns.Add(new MarketingCampaign
			{
				agencyAddress = agencyAddress,
				enabled = isEnabled,
				marketingTypeName = marketingTypeName
			});
		}
		else
		{
			marketingCampaign.enabled = isEnabled;
		}
		BusinessHelper.UpdatePromotion(_bizManBusiness.buildingRegistration);
		RefreshMarketingEfficiency();
		float val = _bizManBusiness.buildingRegistration.marketingCampaigns.Where((MarketingCampaign x) => x.agencyAddress == agencyAddress && x.enabled).Sum((MarketingCampaign x) => MarketingTypeSettings.Get(x.marketingTypeName).pricePerDay);
		entry.GetLabelByName("PricePerDayValue").text = val.ToCurrencyFormat();
	}

	private void CancelAllCampaignsFromAgency(List<MarketingCampaign> campaignGroup)
	{
		foreach (MarketingCampaign item in campaignGroup)
		{
			_bizManBusiness.buildingRegistration.marketingCampaigns.Remove(item);
		}
		BusinessHelper.UpdatePromotion(_bizManBusiness.buildingRegistration);
		RefreshMarketingEfficiency();
		RefreshCampaigns();
	}

	private void CallContact(Contact contact)
	{
		MarketingCampaignSettings.QueueSelectedBusiness(_bizManBusiness.buildingRegistration);
		ContactsApp.OpenAndStartCall(contact);
	}
}
