using System;
using System.Collections.Generic;
using System.Text;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/CustomLocalization/FromDynamicPurchaseRequirement")]
public class QuestEntryCustomLocalizationFromDynamicPurchaseRequirement : QuestEntryCustomLocalization
{
	private const string LightRedColor = "#F23D3D";

	private const string NoSuitableBusinessLocalizationKey = "tutorial_no_business_suitable_for_this_quest";

	[SerializeField]
	private HasPurchasedDynamicItems hasPurchasedDynamicItemsRequirement;

	private readonly StringBuilder _stringBuilder = new StringBuilder();

	private QuestEntryTarget _addressTarget;

	private Address _lastAddress;

	private string _lastBusinessTypeName;

	private Dictionary<string, int> _itemCount = new Dictionary<string, int>();

	public override bool IsDynamic()
	{
		return true;
	}

	public override LanguageChangeEventDataHolder GetLocalization(string localizeKey)
	{
		TutorialDynamicItems dynamicItemsForTutorialPointers = hasPurchasedDynamicItemsRequirement.GetDynamicItemsForTutorialPointers();
		_itemCount.Clear();
		foreach (string[] dynamicItem in dynamicItemsForTutorialPointers.dynamicItems)
		{
			if (!_itemCount.TryAdd(dynamicItem[0], 1))
			{
				_itemCount[dynamicItem[0]]++;
			}
		}
		_stringBuilder.Clear();
		if (dynamicItemsForTutorialPointers.invalid)
		{
			_stringBuilder.Append("<color=#F23D3D>");
			_stringBuilder.Append("(" + "tutorial_no_business_suitable_for_this_quest".GetLocalization() + ")");
			_stringBuilder.Append("</color>");
		}
		else
		{
			foreach (KeyValuePair<string, int> item in _itemCount)
			{
				if (item.Value == 1)
				{
					_stringBuilder.Append(" - " + item.Key.GetLocalization() + "<br>");
				}
				else
				{
					_stringBuilder.Append($" - {item.Value}x {item.Key.GetLocalization()}<br>");
				}
			}
			_stringBuilder.Remove(_stringBuilder.Length - 4, 4);
		}
		return new LanguageChangeEventDataHolder
		{
			Key = localizeKey,
			Arguments = new
			{
				list = _stringBuilder.ToString()
			}
		};
	}

	public override void Init()
	{
		_addressTarget = hasPurchasedDynamicItemsRequirement.customBuildingTarget;
		_lastAddress = _addressTarget.GetAddress();
		_lastBusinessTypeName = BuildingHelper.GetBuildingRegistration(_lastAddress)?.businessTypeName ?? "ba:businesstype_empty";
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChanged));
	}

	private void OnBuildingRegistrationChanged(Address address)
	{
		if (_lastAddress.IsUndefined())
		{
			if (!_addressTarget.GetAddress().IsUndefined())
			{
				UpdateLocalizationIfNeeded();
			}
		}
		else if (_lastAddress == address)
		{
			UpdateLocalizationIfNeeded();
		}
	}

	private void UpdateLocalizationIfNeeded()
	{
		Address address = _addressTarget.GetAddress();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (address != _lastAddress || buildingRegistration?.businessTypeName != _lastBusinessTypeName)
		{
			_lastAddress = _addressTarget.GetAddress();
			_lastBusinessTypeName = buildingRegistration?.businessTypeName ?? "ba:businesstype_empty";
			InstanceBehavior<UIs>.Instance.tutorialUI.UpdateQuestEntries();
		}
	}

	public override void Dispose()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChanged));
	}
}
