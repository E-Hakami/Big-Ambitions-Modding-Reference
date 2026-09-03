using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using UnityEngine;

namespace Boats;

public class BoatManager : InstanceBehavior<BoatManager>
{
	public const int YearlyMaintenancePercentage = 10;

	private Boat[] _boats;

	public static float BoatSellReturn => ItemHelper.GetSellingMultiplier();

	private void Start()
	{
		GlobalEvents.onNewDay = (Action)Delegate.Combine(GlobalEvents.onNewDay, new Action(CheckYearlyMaintenances));
	}

	public void LoadBoats()
	{
		_boats = base.gameObject.GetComponentsInChildren<Boat>();
		Boat[] boats = _boats;
		foreach (Boat boat in boats)
		{
			BoatData boatData = SaveGameManager.Current.playerBoats.FirstOrDefault((BoatData x) => x.id == boat.id);
			boat.Load(boatData);
		}
	}

	private void CheckYearlyMaintenances()
	{
		foreach (BoatData boat in SaveGameManager.Current.playerBoats)
		{
			if (boat.nextMaintenanceDay > SaveGameManager.Current.Day)
			{
				continue;
			}
			boat.nextMaintenanceDay += SaveGameManager.Current.gameVariables.daysPerYear;
			BoatType boatType = _boats.FirstOrDefault((Boat x) => x.id == boat.id)?.boatTypeName.GetBoatType();
			if (boatType == null)
			{
				Debug.LogError("Boat with ID " + boat.id + " not found");
				continue;
			}
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"boatName",
				boatType.type.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_boatyearlymaintenance", data);
			if (boatType.taxDeductible)
			{
				transactionInfo.SetTaxDeductibleName(boatType.type.GetLocalizeKey());
			}
			GameManager.ChangeMoneySafe(-Mathf.RoundToInt((float)boatType.price * 0.1f), transactionInfo, null, null, force: true);
		}
	}
}
