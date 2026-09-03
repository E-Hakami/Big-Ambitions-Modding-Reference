using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine;

public class Road : MonoBehaviour
{
	private const float RoadNameLabelOffsetY = 1f;

	public static string Prefix = "<mark=#00000070 padding=40,40,1,1><color=white>";

	public static string Suffix = "</color></mark>";

	[SerializeField]
	private string streetName;

	[SerializeField]
	private TextLocalizationComponent roadNameLabel;

	private void Start()
	{
		roadNameLabel.transform.localPosition += Vector3.up * 1f;
		roadNameLabel.enabled = false;
		roadNameLabel.Key = string.Empty;
		roadNameLabel.SetValue(Prefix + AddressHelper.GetStreetNameLocalized(streetName) + Suffix);
	}

	public static void LogRoadsWithoutReflectionProbes()
	{
		Road[] array = Object.FindObjectsByType<Road>(FindObjectsSortMode.None);
		foreach (Road road in array)
		{
			if (road.GetComponentInChildren<ReflectionProbe>() == null)
			{
				Debug.Log(road.name, road);
			}
		}
		Intersection[] array2 = Object.FindObjectsByType<Intersection>(FindObjectsSortMode.None);
		foreach (Intersection intersection in array2)
		{
			if (intersection.GetComponentInChildren<ReflectionProbe>() == null)
			{
				Debug.Log(intersection.name, intersection);
			}
		}
	}
}
