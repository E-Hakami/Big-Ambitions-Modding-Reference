using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Shared;

public abstract class BaseFilterToggle<TModel> : MonoBehaviour
{
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	protected TextLocalizationComponent label;

	private UnityAction<bool> _onValueChanged;

	public bool IsOn => toggle.isOn;

	public FilterToggleGroup Group { get; private set; }

	private void Awake()
	{
		FetchComponents();
	}

	private void OnEnable()
	{
		if (_onValueChanged != null)
		{
			toggle.onValueChanged.AddListener(_onValueChanged);
		}
	}

	private void OnDisable()
	{
		if (_onValueChanged != null)
		{
			toggle.onValueChanged.RemoveListener(_onValueChanged);
		}
	}

	public void Initialize(FilterToggleGroup group)
	{
		Group = group;
		FetchComponents();
	}

	public void SetUp(UnityAction onFilterChanged)
	{
		if (_onValueChanged != null)
		{
			toggle.onValueChanged.RemoveListener(_onValueChanged);
		}
		_onValueChanged = delegate
		{
			onFilterChanged?.Invoke();
		};
		if (base.gameObject.activeInHierarchy)
		{
			toggle.onValueChanged.AddListener(_onValueChanged);
		}
	}

	public abstract bool PassesFilter(TModel item);

	public void SetToggleWithoutNotify(bool isOn)
	{
		toggle.SetIsOnWithoutNotify(isOn);
	}

	private void FetchComponents()
	{
		toggle = GetComponentInChildren<Toggle>(includeInactive: true);
		label = GetComponentInChildren<TextLocalizationComponent>(includeInactive: true);
	}
}
