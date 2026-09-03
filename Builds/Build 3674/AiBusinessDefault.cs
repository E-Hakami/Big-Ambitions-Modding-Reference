using System.Collections.Generic;
using System.Linq;
using HGAttributes;
using IngameDebugConsole;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "BigAmbitions/AI Business Default")]
public class AiBusinessDefault : ScriptableObject
{
	private static List<AiBusinessDefault> AIBusinessDefaults;

	private const string AddressableLabel = "WorkoutGroups";

	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	public string businessName;

	public string buildingLayout;

	public string corporationRivalId;

	[ShowIf("ShowGoodsSource")]
	public AiBusinessGoodsSource goodsSource;

	public LogoSettings logoSettings;

	public SignAppearanceSettings signAppearanceSettings;

	public List<ScheduleDay> schedule;

	[ConsoleMethod("SetAIBusinessDefault", "Sets the Current Store to a Layout", new string[] { })]
	public static void SetAIBusinessDefault(string layout)
	{
		if (BuildingManager.IsInsideBuilding)
		{
			BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
			SetAIBusinessDefault(layout, buildingRegistration);
		}
	}

	public static void SetAIBusinessDefault(string layout, BuildingRegistration registration)
	{
		if (AIBusinessDefaults == null)
		{
			AIBusinessDefaults = Addressables.LoadAssetsAsync<AiBusinessDefault>("WorkoutGroups", null).WaitForCompletion().ToList();
		}
		AiBusinessDefault aiBusinessDefault = AIBusinessDefaults.FirstOrDefault((AiBusinessDefault x) => x.buildingLayout == layout);
		if (!(aiBusinessDefault == null))
		{
			registration.Layout = aiBusinessDefault.buildingLayout;
			registration.businessTypeName = aiBusinessDefault.businessTypeName;
			registration.BusinessName = aiBusinessDefault.businessName;
			registration.logoSettings = aiBusinessDefault.logoSettings;
			registration.signAppearanceSettings = aiBusinessDefault.signAppearanceSettings;
		}
	}

	private bool ShowGoodsSource()
	{
		return string.IsNullOrEmpty(corporationRivalId);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AIBusinessDefaults = null;
	}
}
