using Buildings;
using UnityEngine;

namespace BigAmbitions.Rivals;

[CreateAssetMenu(fileName = "NewSpecialRival", menuName = "BigAmbitions/Rivals/SpecialRival")]
public class SpecialRival : ScriptableObject
{
	public Sprite monologueSprite;

	public Sprite rivalAppSprite;

	public RivalData rivalData;

	public string primaryNeighborhood;

	[Tooltip("The rate the player has to pay for overtaking a business from this rival. 1.0 means the player has to pay the exact value of the business.")]
	public float businessOvertakeAcceptRate = 1f;

	[Header("Timeline")]
	public string entranceMessageKey;

	public AudioClip entranceAudioClip;

	public string rentBuildingMessageKey;

	public AudioClip rentBuildingAudioClip;

	public RivalTimeline timeline;

	public Building hamptonsBuilding;

	public void CheckTimeline()
	{
		timeline.Check(this);
	}
}
