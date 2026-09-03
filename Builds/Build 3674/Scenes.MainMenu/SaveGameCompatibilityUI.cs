using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace Scenes.MainMenu;

public class SaveGameCompatibilityUI : MonoBehaviour
{
	[SerializeField]
	private GameObject preUpgradePanel;

	[SerializeField]
	private GameObject afterUpgradePanel;

	[SerializeField]
	private TMP_Text eaVersionLabel;

	[SerializeField]
	private TMP_Text changelogLabel;

	[SerializeField]
	private TextLocalizationComponent saveGamesUpgradedLabel;

	[SerializeField]
	private LoadGame loadGame;

	public async void UpgradeSaveGames()
	{
		LoadingSpinner.Show();
		base.gameObject.SetActive(value: false);
		preUpgradePanel.SetActive(value: false);
		SaveGamePathHelper.CreateCurrentVersionSaveGameFolder();
		SetSaveGamesUpgradedLabel(await SaveGameCompatibilityHelper.CopySaveGamesBetweenPreviousAndCurrentVersion());
		afterUpgradePanel.SetActive(value: true);
		base.gameObject.SetActive(value: true);
		InstanceBehavior<MainMenuController>.Instance.SetUpButtons();
		loadGame.LoadSaveGames();
		LoadingSpinner.Hide();
	}

	public void SkipUpgrade()
	{
		base.gameObject.SetActive(value: false);
	}

	public void ShowIfNeeded()
	{
		if (!SaveGameCompatibilityHelper.HasCurrentVersionSaveGamesFolder() && SaveGameCompatibilityHelper.HasSaveGamesInPreviousVersion())
		{
			Show();
		}
	}

	private void Show()
	{
		SetVersionInfo(MainMenuController.currentVersion);
		afterUpgradePanel.SetActive(value: false);
		preUpgradePanel.SetActive(value: true);
		base.gameObject.SetActive(value: true);
	}

	private void SetVersionInfo(GameVersion gameVersion)
	{
		eaVersionLabel.text = GameVersion.GetVersionString(gameVersion.buildNumber);
		changelogLabel.text = gameVersion.GetChangelog();
	}

	private void SetSaveGamesUpgradedLabel(int saveGamesUpgraded)
	{
		saveGamesUpgradedLabel.Arguments = new { saveGamesUpgraded };
	}
}
