using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.PlacementSystem;
using Dialogs;
using Extensions;
using Helpers;
using IngameDebugConsole;
using PlayerActivity;
using UI;
using UI.Notification;
using UnityEngine;

public class SlotMachineController : ItemController
{
	[Serializable]
	public class SlotElementWeight
	{
		public SlotMachineDialog.SlotElement slotElement;

		public float reelOneWeight;

		public float reelTwoWeight;

		public float reelThreeWeight;
	}

	[SerializeField]
	private List<SlotElementWeight> elementsProbabilities;

	[SerializeField]
	private PlayerActivityBalanceConfig gambledBalanceConfig;

	[HideInInspector]
	public int startingBet;

	private ItemController _chair;

	private Dialog _dialog;

	private PlayerController _playerController;

	private List<SlotMachineDialog.SlotElement> _slotElements;

	private List<float> _weightsReelOne;

	private List<float> _weightsReelThree;

	private List<float> _weightsReelTwo;

	private SlotMachineSoundsPlayer _slotMachineSoundsPlayer;

	public PlayerActivityBalanceConfig GambledBalanceConfig => gambledBalanceConfig;

	public override bool ShouldReactToIoEnter()
	{
		return true;
	}

	public override void Start()
	{
		base.Start();
		if (int.TryParse(customValue, out var result))
		{
			SetStartingBet(result);
		}
		_chair = childItemControllers.FirstOrDefault();
		_slotElements = new List<SlotMachineDialog.SlotElement>();
		_weightsReelOne = new List<float>();
		_weightsReelTwo = new List<float>();
		_weightsReelThree = new List<float>();
		foreach (SlotElementWeight elementsProbability in elementsProbabilities)
		{
			_slotElements.Add(elementsProbability.slotElement);
			_weightsReelOne.Add(elementsProbability.reelOneWeight);
			_weightsReelTwo.Add(elementsProbability.reelTwoWeight);
			_weightsReelThree.Add(elementsProbability.reelThreeWeight);
		}
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			return false;
		}
		if (Occupied)
		{
			Notifications.ShowError("notification_slot_machine_is_occupied");
			return false;
		}
		_playerController = InstanceBehavior<GameManager>.Instance.playerController;
		SetChairOccupied(occupied: true);
		SitPlayerOnChair();
		_playerController.Character.LinkToPointAndClickObject(this);
		InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.ShowDialog(CallDialogType.SlotMachineDialog, NavigationBlocker.SlotMachine);
		return true;
	}

	private void SetStartingBet(int newStartingBet)
	{
		startingBet = newStartingBet;
	}

	private void SitPlayerOnChair()
	{
		if (_chair == null)
		{
			Debug.LogError("Chair not found in " + base.gameObject.name, base.gameObject);
		}
		else if (_chair.SittingPosition == null)
		{
			Debug.LogError("Sitting position not found in chair " + _chair.gameObject.name, _chair.gameObject);
		}
		else
		{
			_playerController.Character.SitOnChair(_chair.SittingPosition);
		}
		_playerController.Character.animator.SetBool(PermanentAnimationType.UsingSlotMachine);
	}

	public SlotMachineDialog.SlotElement GetRandomElement(int reelNumber)
	{
		return reelNumber switch
		{
			1 => _slotElements.GetRandomWeighted(_weightsReelOne), 
			2 => _slotElements.GetRandomWeighted(_weightsReelTwo), 
			_ => _slotElements.GetRandomWeighted(_weightsReelThree), 
		};
	}

	public void SetChairOccupied(bool occupied)
	{
		_chair.Occupied = occupied;
	}

	public void ReleaseSlotMachine()
	{
		SetChairOccupied(occupied: false);
		_playerController.Character.animator.SetBool(PermanentAnimationType.UsingSlotMachine, state: false);
	}

	public void ShowVictoryAnimation()
	{
		_playerController.Character.animator.RunAnimationLength(AnimationType.VictorySitting);
	}

	public void ShowDefeatAnimation()
	{
		_playerController.Character.animator.RunAnimationLength(AnimationType.DefeatSitting);
	}

	public void PlaySlotMachineSounds()
	{
		if (_slotMachineSoundsPlayer == null)
		{
			_slotMachineSoundsPlayer = new SlotMachineSoundsPlayer();
		}
		_slotMachineSoundsPlayer.StartPlaying(this);
	}

	public void StopSlotMachineSounds()
	{
		_slotMachineSoundsPlayer?.StopPlaying();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		StopSlotMachineSounds();
	}

	[ConsoleMethod("SetStartingBet", "Sets the starting bet to the Slot Machine currently in placement", new string[] { })]
	public static void Command_SetStartingBet(int value)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SlotMachineController slotMachineController)
		{
			slotMachineController.SetStartingBet(value);
			slotMachineController.customValue = value.ToString();
			Debug.Log("Slot Machine starting bet successfully set to " + value.ToShortCurrencyFormat() + "!");
		}
		else
		{
			Debug.LogError("Couldn't find a Slot Machine. Ensure you have one in hand and are in placement mode");
		}
	}
}
