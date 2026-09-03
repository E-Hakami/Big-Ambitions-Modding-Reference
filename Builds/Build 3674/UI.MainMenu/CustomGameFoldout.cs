using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class CustomGameFoldout : MonoBehaviour
{
	public Action<bool> onToggleFoldout;

	[SerializeField]
	private Button toggleFoldoutButton;

	[SerializeField]
	private GameObject content;

	[SerializeField]
	private PreferredSizeFitter preferredSizeFitter;

	[Header("Arrow")]
	[SerializeField]
	private RectTransform arrow;

	[Range(0f, 360f)]
	[SerializeField]
	private int closedArrowRotation = 180;

	[Range(0f, 360f)]
	[SerializeField]
	private int openArrowRotation = 90;

	public bool IsExpanded => content.activeSelf;

	private void Start()
	{
		toggleFoldoutButton.onClick.AddListener(ToggleFoldout);
		ConfigureArrowRotation();
	}

	public void SetExpanded(bool expanded)
	{
		content.gameObject.SetActive(expanded);
		ConfigureArrowRotation();
		preferredSizeFitter?.ForceUpdate();
	}

	private void ToggleFoldout()
	{
		content.gameObject.SetActive(!content.activeSelf);
		ConfigureArrowRotation();
		preferredSizeFitter?.ForceUpdate();
		onToggleFoldout?.Invoke(content.activeSelf);
	}

	private void ConfigureArrowRotation()
	{
		float z = (content.activeSelf ? openArrowRotation : closedArrowRotation);
		arrow.DOKill();
		arrow.DORotate(new Vector3(0f, 0f, z), 0.1f).SetUpdate(isIndependentUpdate: true);
	}
}
