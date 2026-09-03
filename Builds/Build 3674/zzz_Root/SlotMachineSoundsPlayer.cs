using System.Collections;
using BigAmbitions.SoundSystem;
using Extensions;
using UnityEngine;

public class SlotMachineSoundsPlayer
{
	private readonly SoundType[] _playingSounds = new SoundType[3]
	{
		SoundType.SlotMachineWin,
		SoundType.SlotMachineJackpot,
		SoundType.SlotMachineLoose
	};

	private readonly WaitForSeconds _initialWaitForSeconds = new WaitForSeconds(2f);

	private readonly WaitForSeconds _reelWaitForSeconds = new WaitForSeconds(1f);

	private readonly WaitForSeconds _restartWaitForSeconds = new WaitForSeconds(4f);

	private SlotMachineController _slotMachineController;

	private Coroutine _playingCoroutine;

	public void StartPlaying(SlotMachineController slotMachineController)
	{
		_slotMachineController = slotMachineController;
		_playingCoroutine = _slotMachineController.StartCoroutine(PlaySlotMachineSounds());
	}

	private IEnumerator PlaySlotMachineSounds()
	{
		yield return _initialWaitForSeconds;
		while (true)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.SlotMachineStart, _slotMachineController.transform.position);
			yield return _reelWaitForSeconds;
			SoundType random = _playingSounds.GetRandom();
			InstanceBehavior<SfxManager>.Instance.PlayAudio(random, _slotMachineController.transform.position);
			yield return _restartWaitForSeconds;
		}
	}

	public void StopPlaying()
	{
		if (_playingCoroutine != null)
		{
			_slotMachineController?.StopCoroutine(_playingCoroutine);
		}
	}
}
