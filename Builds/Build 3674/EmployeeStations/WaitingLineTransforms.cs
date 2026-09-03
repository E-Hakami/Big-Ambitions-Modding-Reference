using System;
using UnityEngine;

namespace EmployeeStations;

[Serializable]
public class WaitingLineTransforms
{
	public Transform customerLookTarget;

	public Transform waitingLineObject;

	public Transform anchorTemplate;

	public Transform spotTemplate;
}
