using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/CityMapFilterEnabled")]
public class TutorialPointerHideConditionIfCityMapFilterEnabled : TutorialPointerHideCondition
{
	[SerializeField]
	private string cityMapFilter;

	protected override bool ConditionMetInternal()
	{
		return SaveGameManager.Current.SelectedCitymapFilters.Contains(cityMapFilter);
	}
}
