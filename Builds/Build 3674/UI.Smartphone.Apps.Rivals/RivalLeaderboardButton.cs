using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Rivals;

[RequireComponent(typeof(Button))]
public class RivalLeaderboardButton : MonoBehaviour
{
	[Header("Components")]
	public Button button;

	[SerializeField]
	private TextMeshProUGUI rankText;

	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private TextMeshProUGUI statsText;

	[SerializeField]
	private Image activeRivalryIcon;

	[SerializeField]
	private Image defeatedIcon;

	[SerializeField]
	private Image backgroundImage;

	[Header("Selected")]
	[SerializeField]
	private Sprite selectedBackground;

	[SerializeField]
	private Color selectedTextColor;

	[Header("Deselected")]
	[SerializeField]
	private Sprite deselectedBackground;

	[SerializeField]
	private Color deselectedTextColor;

	public RivalLeaderboardData data;

	private TextLocalizationComponent _statsLocalization;

	private RivalLeaderboard _leaderboard;

	public void SetUp(RivalLeaderboardData leaderboardData, RivalLeaderboard leaderboard, int rank, bool activeRivalry = false)
	{
		_statsLocalization = statsText.GetComponent<TextLocalizationComponent>();
		_leaderboard = leaderboard;
		data = leaderboardData;
		base.gameObject.name = "RivalLeaderboardButton_" + data.entryName;
		activeRivalryIcon.gameObject.SetActive(activeRivalry);
		if (rankText.isActiveAndEnabled)
		{
			rankText.SetText($"#{rank}");
		}
		nameText.SetText(data.entryName);
		SetDefeated(rank <= 0);
		button.onClick.AddListener(delegate
		{
			InstanceBehavior<RivalsApp>.Instance.ShowRival(data);
		});
	}

	public void Select()
	{
		backgroundImage.sprite = selectedBackground;
		rankText.color = selectedTextColor;
		nameText.color = selectedTextColor;
		statsText.color = selectedTextColor;
		activeRivalryIcon.color = selectedTextColor;
		defeatedIcon.color = selectedTextColor;
		_leaderboard.DeselectButtons(this);
	}

	public void Deselect()
	{
		backgroundImage.sprite = deselectedBackground;
		rankText.color = deselectedTextColor;
		nameText.color = deselectedTextColor;
		statsText.color = deselectedTextColor;
		activeRivalryIcon.color = deselectedTextColor;
		defeatedIcon.color = deselectedTextColor;
	}

	private void SetDefeated(bool defeated)
	{
		rankText.gameObject.SetActive(!defeated);
		defeatedIcon.gameObject.SetActive(defeated);
		if (defeated)
		{
			_statsLocalization.Key = "rivals_leaderboard_defeated";
		}
		else
		{
			_statsLocalization.SetData("rivals_leaderboard_stat".Localize(new
			{
				numBusinesses = data.ownedBusinesses.Count,
				weeklyIncome = data.weeklyIncome.ToShortCurrencyFormat(abbreviated: true)
			}));
		}
	}
}
