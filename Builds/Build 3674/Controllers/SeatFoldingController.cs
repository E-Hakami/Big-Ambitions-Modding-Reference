using System;
using System.Collections;
using UnityEngine;

namespace Controllers;

public class SeatFoldingController : SeatController
{
	private const float FoldingDuration = 0.25f;

	private const float FoldAngle = 75f;

	private static readonly Quaternion FoldedRotation = Quaternion.Euler(-75f, 0f, 0f);

	[SerializeField]
	private Transform[] foldingParts;

	private Coroutine[] _foldingCoroutines;

	public override void Awake()
	{
		base.Awake();
		_foldingCoroutines = new Coroutine[foldingParts.Length];
		Transform[] array = foldingParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].localRotation = FoldedRotation;
		}
	}

	public override void OnSittingChanged(Transform seat, bool isSitting)
	{
		base.OnSittingChanged(seat, isSitting);
		int num = Array.IndexOf(sittingPositions, seat);
		if (num != -1 && num < foldingParts.Length)
		{
			Transform seatPart = foldingParts[num];
			if (_foldingCoroutines[num] != null)
			{
				StopCoroutine(_foldingCoroutines[num]);
			}
			_foldingCoroutines[num] = StartCoroutine(FoldSeat(seatPart, isSitting, num));
		}
	}

	private IEnumerator FoldSeat(Transform seatPart, bool toUnfolderPosition, int index)
	{
		Quaternion targetRotation = (toUnfolderPosition ? Quaternion.identity : FoldedRotation);
		float t = 0f;
		Quaternion initialRotation = seatPart.localRotation;
		while (t < 0.25f)
		{
			t += Time.deltaTime;
			seatPart.localRotation = Quaternion.RotateTowards(initialRotation, targetRotation, t / 0.25f * 75f);
			yield return null;
		}
		seatPart.localRotation = targetRotation;
		_foldingCoroutines[index] = null;
	}
}
