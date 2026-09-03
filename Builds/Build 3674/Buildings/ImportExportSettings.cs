using System;
using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/ImportExportSettings")]
public class ImportExportSettings : SpecialServiceSettings
{
	[AutocompleteDropdown("Items")]
	public List<string> itemsAvailable;

	[AutocompleteDropdown("Items")]
	public List<string> additionalItemsAvailableIfGameSettingEnabled;

	[NonSerialized]
	private List<string> _allItemsAvailable;

	public IReadOnlyList<string> GetItemsAvailable(GameInstance gameInstance = null, bool forceAllItemsAvailable = false)
	{
		if (gameInstance == null)
		{
			gameInstance = SaveGameManager.Current;
		}
		if (!gameInstance.gameVariables.allProductsAvailableFromImporters && !forceAllItemsAvailable)
		{
			return itemsAvailable;
		}
		if (_allItemsAvailable != null)
		{
			return _allItemsAvailable;
		}
		_allItemsAvailable = new List<string>(itemsAvailable);
		_allItemsAvailable.AddRange(additionalItemsAvailableIfGameSettingEnabled);
		return _allItemsAvailable;
	}
}
