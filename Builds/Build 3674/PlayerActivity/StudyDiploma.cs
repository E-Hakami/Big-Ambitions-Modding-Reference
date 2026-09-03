using System;
using UnityEngine;

namespace PlayerActivity;

[Serializable]
public class StudyDiploma : IPlayerActivityType
{
	public DiplomaName diplomaName;

	public Transform chair;

	public Transform exitPosition;

	public ItemController computer;

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return new StudyActivity(this, attachedEntity);
	}
}
