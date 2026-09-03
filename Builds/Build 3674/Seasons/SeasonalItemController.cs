using UnityEngine;

namespace Seasons;

public class SeasonalItemController : ItemController
{
	[Header("SeasonalItemController")]
	[SerializeField]
	private Transform seasonalItemsVisualsContainer;

	public override void Start()
	{
		base.Start();
		SeasonHelper.onSeasonalDecorationsOptionChanged.AddListener(delegate
		{
			SetUpSeasonalItem();
		});
	}

	private void OnEnable()
	{
		SetUpSeasonalItem();
	}

	private void SetUpSeasonalItem()
	{
		seasonalItemsVisualsContainer = ((seasonalItemsVisualsContainer != null) ? seasonalItemsVisualsContainer : base.transform);
		SeasonName seasonName = SeasonHelper.CurrentSeasonName;
		if (PlayerPrefSettings.SeasonalDecorations)
		{
			if (!(seasonalItemsVisualsContainer.Find(seasonName.ToString()) != null))
			{
				seasonName = SeasonName.None;
			}
		}
		else
		{
			seasonName = SeasonName.None;
		}
		foreach (Transform item in seasonalItemsVisualsContainer)
		{
			item.gameObject.SetActive(item.gameObject.name == seasonName.ToString());
		}
	}
}
