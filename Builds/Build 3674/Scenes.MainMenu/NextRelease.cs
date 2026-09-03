using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Localizor.LanguageChangeEvent;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class NextRelease : MonoBehaviour
{
	[SerializeField]
	private Image barFiller;

	[SerializeField]
	private TextLocalizationComponent daysLabel;

	private List<RoadmapEntry> _roadmapEntries;

	private void Start()
	{
		StartCoroutine(SetRoadmapData());
	}

	private IEnumerator SetRoadmapData()
	{
		using UnityWebRequest request = UnityWebRequest.Get("https://www.bigambitionsgame.com/api/roadmap.html");
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(request.error);
			yield break;
		}
		JArray jArray = JArray.Parse(request.downloadHandler.text);
		_roadmapEntries = new List<RoadmapEntry>();
		foreach (JToken item2 in jArray)
		{
			JObject jObject = item2.ToObject<JObject>();
			if (jObject != null && !string.IsNullOrEmpty(jObject["estimate"]?.ToString()) && !string.IsNullOrEmpty(jObject["done"]?.ToString()))
			{
				RoadmapEntry item = new RoadmapEntry
				{
					releaseDate = Convert.ToDateTime(jObject["estimate"].ToString()),
					released = bool.Parse(jObject["done"].ToString())
				};
				_roadmapEntries.Add(item);
			}
		}
		float daysUntilNextRelease = GetDaysUntilNextRelease();
		daysUntilNextRelease = Mathf.Max(daysUntilNextRelease, 0f);
		float daysSincePreviousRelease = GetDaysSincePreviousRelease();
		float fillAmount = daysSincePreviousRelease / (daysSincePreviousRelease + daysUntilNextRelease);
		barFiller.fillAmount = fillAmount;
		daysLabel.Prefix = $"{Mathf.FloorToInt(daysUntilNextRelease)} ";
	}

	private float GetDaysUntilNextRelease()
	{
		RoadmapEntry roadmapEntry = (from x in _roadmapEntries
			where !x.released
			orderby x.releaseDate
			select x).FirstOrDefault();
		if (roadmapEntry == null)
		{
			return 999f;
		}
		return (float)(roadmapEntry.releaseDate - DateTime.Today).TotalHours / 24f;
	}

	private float GetDaysSincePreviousRelease()
	{
		RoadmapEntry roadmapEntry = (from x in _roadmapEntries
			where x.released
			orderby x.releaseDate descending
			select x).FirstOrDefault();
		if (roadmapEntry == null)
		{
			return 0f;
		}
		return (float)(DateTime.Today - roadmapEntry.releaseDate).TotalHours / 24f;
	}
}
