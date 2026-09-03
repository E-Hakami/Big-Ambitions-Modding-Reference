using System.Collections.Generic;
using System.Linq;
using Buildings;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/DynamicImportTarget")]
public class DynamicImportTarget : QuestEntryTarget
{
	[SerializeField]
	private CustomBuildingTarget customBuildingTarget;

	public override Address GetAddress()
	{
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return null;
		}
		List<string> listOfItemsForSale = buildingRegistration.GetListOfItemsForSale();
		foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration2.businessTypeName != "ba:businesstype_importexport")
			{
				continue;
			}
			ImportExportSettings importExportSettings = buildingRegistration2.BuildingCached.SpecialService.settings as ImportExportSettings;
			foreach (string item in listOfItemsForSale)
			{
				if (importExportSettings.GetItemsAvailable().Contains(item))
				{
					return buildingRegistration2.Address;
				}
			}
		}
		return null;
	}
}
