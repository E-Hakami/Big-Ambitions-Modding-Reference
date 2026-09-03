using System;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/CustomLocalization/FromSkillName")]
public class QuestEntryCustomLocalizationFromSkillName : QuestEntryCustomLocalization
{
	[SerializeField]
	private CustomBuildingTarget target;

	private Address _lastAddress;

	private string _lastBusinessTypeName;

	public override bool IsDynamic()
	{
		return true;
	}

	public override LanguageChangeEventDataHolder GetLocalization(string localizeKey)
	{
		string label = "ba:skill_customerservice";
		BuildingRegistration buildingRegistration = target.GetBuildingRegistration();
		if (buildingRegistration != null)
		{
			label = BusinessTypeHelper.GetData(buildingRegistration).employeePrimarySkills[0];
		}
		return new LanguageChangeEventDataHolder
		{
			Key = localizeKey,
			Arguments = new
			{
				skillName = label.GetLocalization()
			}
		};
	}

	public override void Init()
	{
		_lastAddress = target.GetAddress();
		_lastBusinessTypeName = BuildingHelper.GetBuildingRegistration(_lastAddress)?.businessTypeName ?? "ba:businesstype_empty";
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChanged));
	}

	private void OnBuildingRegistrationChanged(Address address)
	{
		if (_lastAddress.IsUndefined())
		{
			if (!target.GetAddress().IsUndefined())
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
		Address address = target.GetAddress();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (!(buildingRegistration?.businessTypeName == _lastBusinessTypeName))
		{
			_lastAddress = address;
			_lastBusinessTypeName = buildingRegistration?.businessTypeName ?? "ba:businesstype_empty";
			InstanceBehavior<UIs>.Instance.tutorialUI.UpdateQuestEntries();
		}
	}

	public override void Dispose()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChanged));
	}
}
