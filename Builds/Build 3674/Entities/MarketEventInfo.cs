using NaughtyAttributes;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(menuName = "BigAmbitions/MarketEventInfo")]
public class MarketEventInfo : ScriptableObject
{
	public MarketEventType type;

	public int demandImpact;

	public int durationInDays;

	[HideIf("useItemNameAsEventTargetName")]
	public bool useBusinessTypeAsEventTargetName;

	[HideIf("useBusinessTypeAsEventTargetName")]
	public bool useItemNameAsEventTargetName;

	public Sprite boxImage;
}
