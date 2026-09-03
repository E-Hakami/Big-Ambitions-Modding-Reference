using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.InteriorDesigner;
using UnityEngine;

public class MopController : ItemController
{
	public static MopController currentCleaningMop;

	private static Action OnStopCleaning;

	private static readonly List<DirtSpotObject> AffectedCells = new List<DirtSpotObject>();

	private static readonly Collider[] OverlapResults = new Collider[16];

	public void AssignToPlayer()
	{
		if (base.BuildingContext.IsPlayerOwnedBusiness)
		{
			InstanceBehavior<BuildingManager>.Instance.onFloorCellClick.AddListener(OnFloorCellClick);
		}
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onOpenInteriorDesigner, new Action(OnOpenInteriorDesigner));
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(OnCloseInteriorDesigner));
		EnableCleaningMode();
		InstanceBehavior<GameManager>.Instance.playerController.Character.animator.SetBool(PermanentAnimationType.CleaningIdle);
	}

	private static void EnableCleaningMode()
	{
		MouseController.cursorOnHover = CursorType.Mop;
		MouseController.SetRestrictedObjectTypes(InteractiveObjectType.Ground);
		BuildingCleanlinessHelper.ShowDirtinessHighlighting();
	}

	private void OnCloseInteriorDesigner()
	{
		EnableCleaningMode();
	}

	private void OnOpenInteriorDesigner()
	{
		DisableCleaningMode();
	}

	public void UnAssignFromPlayer()
	{
		if ((bool)currentCleaningMop)
		{
			InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.CleaningFloor);
		}
		currentCleaningMop = null;
		InstanceBehavior<GameManager>.Instance.playerController.Character.animator.SetBool(PermanentAnimationType.CleaningIdle, state: false);
		InstanceBehavior<GameManager>.Instance.playerController.Character.animator.SetBool(PermanentAnimationType.Cleaning, state: false);
		InstanceBehavior<BuildingManager>.Instance?.onFloorCellClick.RemoveListener(OnFloorCellClick);
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onOpenInteriorDesigner, new Action(OnOpenInteriorDesigner));
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(OnCloseInteriorDesigner));
		GameEvent.Invoke("ba:gameevent_cleanedfloor");
		DisableCleaningMode();
	}

	private static void DisableCleaningMode()
	{
		MouseController.Reset();
		BuildingCleanlinessHelper.HideDirtinessHighlighting();
	}

	private void OnFloorCellClick(DirtSpotObject dirtSpotObject)
	{
		StartCoroutine(FloorCellClick(dirtSpotObject));
	}

	private static void OnNewHour()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.RefreshMetaCleanliness();
		});
	}

	public static void SetOnStopCleaningAction(Action onStopCleaning)
	{
		OnStopCleaning = onStopCleaning;
	}

	private IEnumerator FloorCellClick(DirtSpotObject dirtSpotObject)
	{
		currentCleaningMop = this;
		AffectedCells.Clear();
		int num = Physics.OverlapBoxNonAlloc(dirtSpotObject.transform.position, Vector3.one, OverlapResults, Quaternion.identity, LayerHelper.groundLayerMask);
		for (int i = 0; i < num; i++)
		{
			DirtSpotObject component = OverlapResults[i].GetComponent<DirtSpotObject>();
			if (component != null)
			{
				AffectedCells.Add(component);
			}
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.CleaningFloor);
		InstanceBehavior<GameManager>.Instance.playerController.Character.animator.SetBool(PermanentAnimationType.Cleaning);
		float time = 0f;
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.MopOneShot, base.transform.position, 1f, isPlayerCreatedSound: true);
		while (AffectedCells.Max((DirtSpotObject x) => base.BuildingContext.Registration.dirtSpots[x.DirtSpot].dirtiness) > 0.1f)
		{
			AffectedCells.ForEach(delegate(DirtSpotObject fc)
			{
				base.BuildingContext.Registration.dirtSpots[fc.DirtSpot].dirtiness = Math.Max(base.BuildingContext.Registration.dirtSpots[fc.DirtSpot].dirtiness - 30f, 0f);
			});
			yield return new WaitForSeconds(0.3f);
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.MopOneShot, base.transform.position, 1f, isPlayerCreatedSound: true);
			time += 0.3f;
			AffectedCells.ForEach(delegate(DirtSpotObject fc)
			{
				fc.SetDirtiness();
			});
			yield return null;
		}
		yield return new WaitForSeconds(1f - time % 1f);
		StopCleaning();
	}

	public void StopCleaning()
	{
		StopCoroutine("FloorCellClick");
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.CleaningFloor);
		GameEvent.Invoke("ba:gameevent_cleanedfloor");
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.RefreshMetaCleanliness();
		InstanceBehavior<GameManager>.Instance.playerController.Character.animator.SetBool(PermanentAnimationType.Cleaning, state: false);
		OnStopCleaning?.Invoke();
		OnStopCleaning = null;
		currentCleaningMop = null;
	}

	public static bool HandleEscape()
	{
		if (PlayerHelper.IsHoldingItem)
		{
			Item itemInHands = PlayerHelper.ItemInHands;
			if ((object)itemInHands == null || itemInHands.HasTag(TagRef.Itemtag.ismop))
			{
				SetOnStopCleaningAction(null);
				InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
				PlayerHelper.RemoveItemsFromHands();
				return true;
			}
		}
		return false;
	}

	public override void OnDestroy()
	{
		InstanceBehavior<BuildingManager>.Instance?.allItemControllers?.Remove(this);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		OnStopCleaning = null;
		currentCleaningMop = null;
	}
}
