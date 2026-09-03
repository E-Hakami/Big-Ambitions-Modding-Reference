using System;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using TMPro;
using UnityEngine;

namespace UI.InteriorDesigner;

public class InteriorScoreInfoPanelUI : InfoPanelUI
{
	[SerializeField]
	private TMP_Text interiorScoreText;

	public override bool ShouldShow()
	{
		return BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.hasinteriorscore);
	}

	public override void OnEnterInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onUpdateInteriorScore = (Action<float>)Delegate.Combine(InteriorDesignerInfoPanelEvents.onUpdateInteriorScore, new Action<float>(UpdateInteriorScore));
		UpdateInteriorScore(InteriorDesignerController.InteriorScorePercentage);
	}

	public override void OnExitInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onUpdateInteriorScore = (Action<float>)Delegate.Remove(InteriorDesignerInfoPanelEvents.onUpdateInteriorScore, new Action<float>(UpdateInteriorScore));
	}

	private void UpdateInteriorScore(float score)
	{
		interiorScoreText.text = $"{Mathf.CeilToInt(score)}%";
		interiorScoreText.color = ColoredTextHelper.GetInteriorScoreColor(score);
	}
}
