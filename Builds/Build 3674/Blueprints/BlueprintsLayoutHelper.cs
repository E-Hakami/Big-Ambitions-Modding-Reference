using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BusinessLayoutSets;
using Extensions;
using IngameDebugConsole;
using UI;
using UnityEngine;

namespace Blueprints;

public static class BlueprintsLayoutHelper
{
	[ConsoleMethod("LoadBlueprint", "Loads the blueprint into the building", new string[] { })]
	public static async void LoadBlueprint(string blueprintName)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			Debug.LogWarning("You have to be in an owned building to run this command");
			return;
		}
		Blueprint blueprint = await BlueprintsFolderLoader.GetBlueprint(blueprintName);
		if (blueprint == null)
		{
			return;
		}
		if (blueprint.metadata.buildingType != InstanceBehavior<BuildingManager>.Instance.building.BuildingType || blueprint.metadata.buildingSizeInfo.buildingSize != InstanceBehavior<BuildingManager>.Instance.building.BuildingSize || blueprint.metadata.buildingSizeInfo.buildingVersion != InstanceBehavior<BuildingManager>.Instance.building.BuildingVersion)
		{
			string text = blueprint.metadata.buildingType.GetIdWithoutType() + "/" + blueprint.metadata.buildingSizeInfo.ToString();
			string text2 = InstanceBehavior<BuildingManager>.Instance.building.BuildingType.GetIdWithoutType() + "/" + InstanceBehavior<BuildingManager>.Instance.building.BuildingSize + $"{InstanceBehavior<BuildingManager>.Instance.building.BuildingVersion}";
			Debug.LogError("The blueprint building specs (" + text + ") doesn't matchcurrent building specs (" + text2 + ")");
			return;
		}
		BusinessLayoutSet businessLayoutSet = await blueprint.GetLayout();
		if (businessLayoutSet == null)
		{
			return;
		}
		InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.ClearChildren();
		foreach (ItemInstance item in ItemHelper.GetItemsByAddress(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address).Values.ToList())
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		BusinessLayoutSetHelper.InsertLayoutSet(InstanceBehavior<BuildingManager>.Instance.building.Address, businessLayoutSet);
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GenerateInteriorDesignerLookup();
		Debug.Log("Layout inserted!");
		if (InstanceBehavior<BuildingManager>.Instance.LoadBuilding())
		{
			InstanceBehavior<BuildingManager>.Instance.LoadItems();
		}
		else
		{
			Debug.LogError("Couldn't load building. Try re-entering");
		}
	}

	[ConsoleMethod("PreviewBlueprintLayout", "Previews the blueprint", new string[] { })]
	public static async void PreviewBlueprintLayout(string blueprintName)
	{
		Blueprint blueprint = await BlueprintsFolderLoader.GetBlueprint(blueprintName);
		if (blueprint != null)
		{
			BusinessLayoutSet businessLayoutSet = await blueprint.GetLayout();
			if (businessLayoutSet != null)
			{
				InstanceBehavior<UIs>.Instance.buildingPreview.PreviewLayout(businessLayoutSet);
			}
		}
	}

	public static async Task<BusinessLayoutSet> GetLayoutFromBlueprint(string designName)
	{
		Blueprint blueprint = await BlueprintsFolderLoader.GetBlueprint(designName);
		if (blueprint == null)
		{
			return null;
		}
		return await blueprint.GetLayout();
	}
}
