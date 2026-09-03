using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blueprints.Compatibility.BlueprintFixesEA11;
using BlueprintsUI;
using UnityEngine;

namespace Blueprints.Compatibility;

public static class BlueprintCompatibilityFixes
{
	private static int LatestCompatBuild;

	private static readonly List<(IBlueprintCompatibilityFix, int)> CompatibilityFixes = new List<(IBlueprintCompatibilityFix, int)>
	{
		(new UpdateLegacyIds(), 3517),
		(new UpdateWorkstationLegacyIds(), 3538)
	};

	public static async Task ApplyCompatibilityFixes(Blueprint blueprint, CompatibilityFixScope scope = CompatibilityFixScope.Both, string layoutPath = null)
	{
		if (scope == CompatibilityFixScope.None || blueprint?.metadata == null || CompatibilityFixes.Count == 0)
		{
			return;
		}
		if (string.IsNullOrEmpty(layoutPath) && blueprint.metadata.blueprintType == BlueprintType.SavedLocally)
		{
			layoutPath = Path.Combine(BlueprintsFolderLoader.GetBlueprintFolder(blueprint.name), "Layout.json");
		}
		BusinessLayoutSet businessLayoutSet = ((!scope.HasFlag(CompatibilityFixScope.Layout) || string.IsNullOrEmpty(layoutPath) || !File.Exists(layoutPath)) ? null : (await BlueprintsFolderLoader.LoadBlueprintLayout(layoutPath)));
		BusinessLayoutSet businessLayoutSet2 = businessLayoutSet;
		int buildNumber = blueprint.metadata.buildNumber;
		bool flag = businessLayoutSet2 != null;
		int layoutBuildNumber = (flag ? businessLayoutSet2.buildNumber : int.MaxValue);
		if (!NeedsFixes(scope, buildNumber, layoutBuildNumber, flag))
		{
			return;
		}
		bool flag2 = scope.HasFlag(CompatibilityFixScope.Layout) && businessLayoutSet2 != null && !string.IsNullOrEmpty(layoutPath);
		bool updateMetadata = scope.HasFlag(CompatibilityFixScope.Metadata) && blueprint.metadata.blueprintType == BlueprintType.SavedLocally;
		bool flag3 = false;
		foreach (var (blueprintCompatibilityFix, buildNumber2) in CompatibilityFixes)
		{
			if (ShouldApplyFix(scope, buildNumber, layoutBuildNumber, flag, buildNumber2))
			{
				blueprintCompatibilityFix.Apply(blueprint, businessLayoutSet2, scope);
				flag3 = true;
			}
		}
		if (flag3)
		{
			int currentBuildNumber = GameVersion.GetCurrent().buildNumber;
			if (flag2)
			{
				businessLayoutSet2.buildNumber = currentBuildNumber;
				await businessLayoutSet2.Serialize(layoutPath);
			}
			if (updateMetadata)
			{
				blueprint.metadata.buildNumber = currentBuildNumber;
				await blueprint.UpdateMetadata();
			}
		}
	}

	private static bool NeedsFixes(CompatibilityFixScope scope, int metadataBuildNumber, int layoutBuildNumber, bool hasLayout)
	{
		if (LatestCompatBuild == 0)
		{
			LatestCompatBuild = GetLatestCompatBuild();
		}
		if ((scope.HasFlag(CompatibilityFixScope.Layout) & hasLayout) && layoutBuildNumber <= LatestCompatBuild)
		{
			return true;
		}
		if (scope.HasFlag(CompatibilityFixScope.Metadata))
		{
			return metadataBuildNumber <= LatestCompatBuild;
		}
		return false;
	}

	private static bool ShouldApplyFix(CompatibilityFixScope scope, int metadataBuildNumber, int layoutBuildNumber, bool hasLayout, int buildNumber)
	{
		if ((scope.HasFlag(CompatibilityFixScope.Layout) & hasLayout) && layoutBuildNumber <= buildNumber)
		{
			return true;
		}
		if (scope.HasFlag(CompatibilityFixScope.Metadata))
		{
			return metadataBuildNumber <= buildNumber;
		}
		return false;
	}

	private static int GetLatestCompatBuild()
	{
		int num = 0;
		foreach (var compatibilityFix in CompatibilityFixes)
		{
			int item = compatibilityFix.Item2;
			if (item > num)
			{
				num = item;
			}
		}
		return num;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LatestCompatBuild = 0;
	}
}
