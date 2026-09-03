using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ScheduleHour : MonoBehaviour
{
	public int timeValue;

	[FormerlySerializedAs("_text")]
	[SerializeField]
	private TMP_Text text;

	public void Awake()
	{
		if ((object)text == null)
		{
			text = GetComponentInChildren<TMP_Text>();
		}
	}

	public void OnEnable()
	{
		int num = timeValue;
		if (TimeHelper.use12h)
		{
			string arg = ((num > 11 && num < 24) ? "PM" : "AM");
			num %= 12;
			if (num == 0)
			{
				num = 12;
			}
			text.text = $"{num} <size=24><color=#8C8C8C>\n{arg}";
		}
		else
		{
			text.text = $"{num:D2}";
		}
	}
}
