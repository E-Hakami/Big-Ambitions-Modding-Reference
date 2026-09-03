using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity.Tennis;

public class TennisScoreLine : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI pointsText;

	[SerializeField]
	private TextMeshProUGUI gamesText;

	[SerializeField]
	private TextMeshProUGUI setsText;

	[SerializeField]
	private Image ball;

	public void UpdateScore(TennisSideScore sideScore, bool advantage, bool hasBall)
	{
		TextMeshProUGUI textMeshProUGUI = pointsText;
		string text = ((!advantage) ? (sideScore.points switch
		{
			0 => "0", 
			1 => "15", 
			2 => "30", 
			_ => "40", 
		}) : "AD");
		textMeshProUGUI.text = text;
		gamesText.text = sideScore.games.ToString();
		setsText.text = sideScore.sets.ToString();
		ball.enabled = hasBall;
	}
}
