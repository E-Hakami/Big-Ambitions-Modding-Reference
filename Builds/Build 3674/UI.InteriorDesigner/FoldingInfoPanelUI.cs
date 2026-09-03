using DG.Tweening;
using UnityEngine;

namespace UI.InteriorDesigner;

public abstract class FoldingInfoPanelUI : InfoPanelUI
{
	[Header("Foldout")]
	[SerializeField]
	private GameObject content;

	[SerializeField]
	private RectTransform arrow;

	[Range(0f, 360f)]
	[SerializeField]
	private int closedArrowRotation = 180;

	[Range(0f, 360f)]
	[SerializeField]
	private int openArrowRotation = 90;

	private void Start()
	{
		ConfigureArrowRotation();
	}

	public void ToggleFoldout()
	{
		content.gameObject.SetActive(!content.activeSelf);
		ConfigureArrowRotation();
	}

	private void ConfigureArrowRotation()
	{
		float z = (content.activeSelf ? openArrowRotation : closedArrowRotation);
		arrow.DOKill();
		arrow.DORotate(new Vector3(0f, 0f, z), 0.1f);
	}
}
