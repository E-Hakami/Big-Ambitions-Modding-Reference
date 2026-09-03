using BigAmbitions.Characters.Appearance;
using UI;
using UnityEngine;

public class WardrobeController : ItemController
{
	[Header("Wardrobe")]
	[SerializeField]
	protected AppearanceTag[] tags;

	public void PerformAction()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
			InstanceBehavior<UIs>.Instance.changeCharacterClothesUI.Show(tags);
		});
	}

	public virtual bool CanChangeClothes()
	{
		return true;
	}
}
