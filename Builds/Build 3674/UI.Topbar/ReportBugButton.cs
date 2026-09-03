using UnityEngine;
using UnityEngine.UI;

namespace UI.Topbar;

public sealed class ReportBugButton : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private BasicTooltip tooltip;

	[SerializeField]
	private string modsActiveDescriptionKey = "menu_report_bug_disabled_mods_tooltip";

	private void OnEnable()
	{
		if (SaveGameManager.IsModdedSave)
		{
			button.interactable = false;
			tooltip.descriptionKey = modsActiveDescriptionKey;
		}
	}
}
