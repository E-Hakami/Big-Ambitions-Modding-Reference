using System;
using BigAmbitions.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class EditableWorkstationHeader : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private Image icon;

	private EmployeeStationController _employeeStation;

	private ItemAliasUpdateListener _aliasUpdateListener;

	private string _currentName;

	public event Action<string> OnHeaderTextChanged;

	private void Awake()
	{
		_aliasUpdateListener = new ItemAliasUpdateListener(RefreshHeaderName);
	}

	private void Start()
	{
		if ((bool)inputField)
		{
			inputField.onEndEdit.AddListener(HandleEndEdit);
		}
	}

	private void OnDestroy()
	{
		if ((bool)inputField)
		{
			inputField.onEndEdit.RemoveListener(HandleEndEdit);
		}
		_aliasUpdateListener?.Clear();
	}

	public bool TryUpdateHeader(EntityController entityController, string headerText)
	{
		_employeeStation = entityController as EmployeeStationController;
		bool flag = InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && (bool)inputField && (bool)_employeeStation;
		if ((bool)inputField)
		{
			inputField.gameObject.SetActive(flag);
		}
		if ((bool)icon)
		{
			icon.gameObject.SetActive(flag);
		}
		if (!flag)
		{
			_aliasUpdateListener?.Clear();
			return false;
		}
		ItemInstance itemInstance = _employeeStation.ItemInstance;
		_aliasUpdateListener.ListenTo(itemInstance);
		if ((bool)icon && itemInstance != null)
		{
			icon.sprite = itemInstance.ItemCached.icon;
		}
		_currentName = headerText;
		inputField.SetTextWithoutNotify(headerText);
		return true;
	}

	private void HandleEndEdit(string value)
	{
		if (!(_currentName == value))
		{
			UpdateWorkstationName(value);
		}
	}

	private void UpdateWorkstationName(string inputValue)
	{
		if (!(_employeeStation == null))
		{
			_employeeStation.ItemInstance?.SetAlias(inputValue);
		}
	}

	private void RefreshHeaderName()
	{
		string overlayHeaderText = OverlayHelper.GetOverlayHeaderText(_employeeStation);
		OnHeaderTextChanged?.Invoke(overlayHeaderText);
	}
}
