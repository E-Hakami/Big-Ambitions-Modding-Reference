using System;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public class CollapsibleFilterCategory : MonoBehaviour
{
	private const float ExpandedAngle = 90f;

	private const float AnimationDuration = 0.2f;

	private const float CollapsedAngle = 180f;

	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private Button collapseButton;

	[SerializeField]
	private RectTransform collapseArrow;

	[SerializeField]
	private GameObject content;

	[SerializeField]
	private Toggle toggleAll;

	private Action<bool> _onCollapsedChanged;

	private Action<bool> _onToggleAll;

	public bool IsCollapsed { get; private set; }

	public void SetUp(string labelKey, bool startCollapsed = false, Action<bool> onCollapsedChanged = null)
	{
		label.Key = labelKey;
		_onCollapsedChanged = onCollapsedChanged;
		SetCollapsedState(startCollapsed, animate: false);
	}

	public void OnCollapseClick()
	{
		SetCollapsedState(!IsCollapsed);
		_onCollapsedChanged?.Invoke(IsCollapsed);
	}

	public void SetUpToggleAll(Action<bool> onToggleAll)
	{
		_onToggleAll = onToggleAll;
		toggleAll.gameObject.SetActive(value: true);
	}

	public void OnToggleAllClick(bool isOn)
	{
		_onToggleAll?.Invoke(isOn);
	}

	public void SetToggleAllWithoutNotify(bool isOn)
	{
		toggleAll.SetIsOnWithoutNotify(isOn);
	}

	protected virtual void SetCollapsedState(bool collapsed, bool animate = true)
	{
		IsCollapsed = collapsed;
		content.SetActive(!IsCollapsed);
		float z = (IsCollapsed ? 180f : 90f);
		if (animate)
		{
			collapseArrow.transform.DOLocalRotate(new Vector3(0f, 0f, z), 0.2f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		}
		else
		{
			collapseArrow.transform.localEulerAngles = new Vector3(0f, 0f, z);
		}
	}
}
