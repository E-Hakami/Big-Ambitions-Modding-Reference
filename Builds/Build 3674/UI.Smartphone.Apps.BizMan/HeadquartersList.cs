using System.Collections.Generic;
using Entities;
using Extensions;
using Helpers;
using Streets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class HeadquartersList : MonoBehaviour
{
	[SerializeField]
	private Transform headquartersEntry;

	private readonly List<Sprite> _cachedBusinessIcons = new List<Sprite>();

	public void Load()
	{
		headquartersEntry.ResetTemplate();
		foreach (Sprite cachedBusinessIcon in _cachedBusinessIcons)
		{
			Object.Destroy(cachedBusinessIcon);
		}
		_cachedBusinessIcons.Clear();
		foreach (BuildingRegistration item in SaveGameManager.Current.BuildingRegistrations.FindAll((BuildingRegistration x) => x.RentedByPlayer && x.businessTypeName == "ba:businesstype_headquarters"))
		{
			SetUpEntry(item);
		}
	}

	private void SetUpEntry(BuildingRegistration headquarters)
	{
		Transform entry = Object.Instantiate(headquartersEntry, headquartersEntry.parent);
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(headquarters.Address);
		});
		entry.GetLabelByName("TopPanel/Name").text = headquarters.BusinessName;
		entry.GetLanguageChangeEventByName("TopPanel/Address").SetValue(headquarters.Address.ToFormattedString(), clearKey: true);
		entry.GetButtonByName("TopPanel/Address/SetDestinationButton").onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(headquarters.Address);
		});
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(headquarters.BusinessName, LogoSize.SquareSign, playerBusiness: true);
		if (businessLogoTexture == null)
		{
			BusinessLogoGenerator.Create(headquarters.BusinessName, headquarters.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(headquarters.BusinessName), headquarters.RentedByPlayer, delegate
			{
				Texture2D businessLogoTexture2 = LogoHelper.GetBusinessLogoTexture(headquarters.BusinessName, LogoSize.SquareSign, playerBusiness: true);
				if (businessLogoTexture2 != null)
				{
					SetHeadquartersIcon(entry, businessLogoTexture2);
				}
			});
		}
		else
		{
			SetHeadquartersIcon(entry, businessLogoTexture);
		}
		SetUpEmployeeCounter(entry, headquarters.Address, "PricingManagers", "ba:skill_pricingmanager");
		SetUpEmployeeCounter(entry, headquarters.Address, "LogisticsManagers", "ba:skill_logisticsmanager");
		SetUpEmployeeCounter(entry, headquarters.Address, "PurchasingAgents", "ba:skill_purchasingagent");
		SetUpEmployeeCounter(entry, headquarters.Address, "HRManagers", "ba:skill_hrmanager");
		SetUpEmployeeCounter(entry, headquarters.Address, "Headhunters", "ba:skill_headhunter");
		entry.gameObject.SetActive(value: true);
	}

	private static void SetUpEmployeeCounter(Transform entry, Address address, string tabName, string skill)
	{
		Button buttonByName = entry.GetButtonByName("EmployeeCounter/" + tabName);
		buttonByName.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(address, tabName);
		});
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = address,
			withSkills = new string[1] { skill },
			excludeBeingReplaced = true
		});
		buttonByName.transform.GetLabelByName("Count").text = employeeInstances.Count.ToString();
	}

	public void SetHeadquartersIcon(Transform entry, Texture2D texture)
	{
		if ((bool)entry)
		{
			Rect rect = new Rect(0f, 0f, texture.width, texture.height);
			Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
			_cachedBusinessIcons.Add(sprite);
			entry.GetImageByName("TopPanel/Logo/BusinessIcon").sprite = sprite;
		}
	}
}
