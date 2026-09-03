using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Help/HasViewedHelpPageForTargetBusiness")]
public class HasViewedHelpPageForTargetBusiness : QuestRequirement
{
	[SerializeField]
	private HelpSlugOverrider helpSlugOverrider;

	public override bool CheckIfCompleted()
	{
		if (!HelpSystem.IsVisible)
		{
			return false;
		}
		string targetHelpSlug = helpSlugOverrider.GetTargetHelpSlug();
		if (!string.IsNullOrEmpty(targetHelpSlug))
		{
			return InstanceBehavior<HelpSystem>.Instance.CurrentSlug == targetHelpSlug;
		}
		return false;
	}
}
