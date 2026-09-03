using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using UnityEngine;

namespace Entities;

public class Homeless : MonoBehaviour
{
	private static readonly AppearanceTag[] AppearanceTags = new AppearanceTag[1] { AppearanceTag.Homeless };

	[SerializeField]
	private BaseHuman human;

	private bool _hasHandObject;

	public void Init()
	{
		human.appearanceSetter.SetRandomAge();
		human.appearanceSetter.SetRandomAppearance(AppearanceTags);
	}

	public void Enable()
	{
		human.animator.SetBool(PermanentAnimationType.Drunk);
		if (!_hasHandObject)
		{
			string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType.Drunk);
			human.AddHandObject(handObjectNameFromPermanentAnimationType);
			_hasHandObject = true;
		}
	}
}
