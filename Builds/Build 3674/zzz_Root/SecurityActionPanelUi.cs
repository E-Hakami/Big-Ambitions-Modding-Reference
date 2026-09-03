using System;
using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Tags;
using Buildings.Indoors.InteriorDesign;
using Helpers;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using UI.InteriorDesigner;
using UnityEngine;

public class SecurityActionPanelUi : ItemActionPanelUI
{
	public static Action onCamerasCoverageUpdated;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite statusGradientRed;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite statusGradientGreen;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite camOnIcon;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite camOffIcon;

	[SerializeField]
	private TextLocalizationComponent titleLabel;

	[SerializeField]
	private TextLocalizationComponent securityRatingLabel;

	public override ToolName[] ToolNames => new ToolName[1] { ToolName.Security };

	protected override bool UseOverlay => true;

	protected override void OnEnable()
	{
		base.OnEnable();
		onCamerasCoverageUpdated = (Action)Delegate.Combine(onCamerasCoverageUpdated, new Action(OnOpen));
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		onCamerasCoverageUpdated = (Action)Delegate.Remove(onCamerasCoverageUpdated, new Action(OnOpen));
	}

	public override void OnOpen()
	{
		bool flag = BusinessTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.buildingRegistration).HasTag(TagRef.Businesstag.allowtheft);
		securityRatingLabel.gameObject.SetActive(flag);
		titleLabel.Key = (flag ? "interior_designer_security_coverage" : "interior_designer_security_coverage_not_required");
		List<ItemController> getSecurityPanelItemControllers = InteriorDesignerController.GetSecurityPanelItemControllers;
		getSecurityPanelItemControllers.Sort((ItemController a, ItemController b) => a.ItemInstance.isSecured.CompareTo(b.ItemInstance.isSecured));
		allItemControllers.Clear();
		allItemControllers.AddRange(getSecurityPanelItemControllers);
		getOverlayBackground = (ItemController x) => (!x.ItemInstance.isSecured) ? statusGradientRed : statusGradientGreen;
		getOverlayIcon = (ItemController x) => (!x.ItemInstance.isSecured) ? camOffIcon : camOnIcon;
		if (!flag)
		{
			base.OnOpen();
			return;
		}
		int num = Mathf.FloorToInt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.securityLevelPercentage);
		string text = ((num < 50) ? ((num >= 30) ? ColorUtility.ToHtmlStringRGB(InstanceBehavior<GlobalReferences>.Instance.colors.orange) : ColorUtility.ToHtmlStringRGB(InstanceBehavior<GlobalReferences>.Instance.colors.red)) : ((num >= 90) ? ColorUtility.ToHtmlStringRGB(InstanceBehavior<GlobalReferences>.Instance.colors.lime) : ColorUtility.ToHtmlStringRGB(InstanceBehavior<GlobalReferences>.Instance.colors.green)));
		string arg = text;
		securityRatingLabel.Arguments = new
		{
			amount = $"<color=#{arg}>{num}%</color>"
		};
		base.OnOpen();
	}
}
