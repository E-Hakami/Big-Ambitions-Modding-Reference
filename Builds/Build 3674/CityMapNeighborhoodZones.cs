using System;
using System.Linq;
using UnityEngine;

public class CityMapNeighborhoodZones : MonoBehaviour
{
	[Serializable]
	public class CityMapNeighborhoodZone
	{
		public string neighbourhood;

		public MeshRenderer zoneRenderer;

		public void Show()
		{
			zoneRenderer.gameObject.SetActive(value: true);
		}

		public void Hide()
		{
			zoneRenderer.gameObject.SetActive(value: false);
		}

		private void SetActive(bool isActive)
		{
			zoneRenderer.gameObject.SetActive(isActive);
		}
	}

	[SerializeField]
	private CityMapNeighborhoodZone[] zones;

	private static readonly int ZoneColor = Shader.PropertyToID("_UnlitColor");

	private static MaterialPropertyBlock _materialPropertyBlock;

	public static CityMapNeighborhoodZone[] Zones => InstanceBehavior<CityManager>.Instance.cityMap.cityMapNeighborhoodZones.zones;

	private static MaterialPropertyBlock MaterialPropertyBlock => _materialPropertyBlock ?? (_materialPropertyBlock = new MaterialPropertyBlock());

	private void Start()
	{
		HideAllZones();
		UpdateAllZoneColors();
	}

	public static void Toggle(string neighborhood, bool isVisible)
	{
		CityMapNeighborhoodZone cityMapNeighborhoodZone = Zones.First((CityMapNeighborhoodZone x) => x.neighbourhood == neighborhood);
		if (isVisible)
		{
			cityMapNeighborhoodZone.Show();
		}
		else
		{
			cityMapNeighborhoodZone.Hide();
		}
	}

	public static CityMapNeighborhoodZone GetNeighbourhoodZone(string neighborhood)
	{
		return Zones.First((CityMapNeighborhoodZone x) => x.neighbourhood == neighborhood);
	}

	private void UpdateAllZoneColors()
	{
		CityMapNeighborhoodZone[] array = zones;
		foreach (CityMapNeighborhoodZone cityMapNeighborhoodZone in array)
		{
			cityMapNeighborhoodZone.zoneRenderer.GetPropertyBlock(MaterialPropertyBlock);
			MaterialPropertyBlock.SetColor(ZoneColor, NeighborhoodHelper.GetData(cityMapNeighborhoodZone.neighbourhood).cityMapZoneColor);
			cityMapNeighborhoodZone.zoneRenderer.SetPropertyBlock(MaterialPropertyBlock);
		}
	}

	public static void HideAllZones()
	{
		CityMapNeighborhoodZone[] array = Zones;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Hide();
		}
	}
}
