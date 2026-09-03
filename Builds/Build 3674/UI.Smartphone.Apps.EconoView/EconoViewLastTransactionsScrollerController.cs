using System.Collections;
using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewLastTransactionsScrollerController : BaTable<EconoViewLastTransactionCellView, EconoViewLastTransactionModel>
{
	[SerializeField]
	private float cellSize = 100f;

	private Coroutine _reloadCoroutine;

	private void OnDisable()
	{
		if (_reloadCoroutine != null)
		{
			StopCoroutine(_reloadCoroutine);
			_reloadCoroutine = null;
		}
	}

	public void LoadLatest()
	{
		data.Clear();
		Queue<Transaction> transactions = SaveGameManager.Current.Transactions;
		if (transactions == null)
		{
			ReloadData();
			return;
		}
		Transaction[] array = transactions.ToArray();
		for (int num = array.Length - 1; num >= 0; num--)
		{
			data.Add(CreateModel(array[num]));
		}
		SortByDayDescending();
		ReloadData();
	}

	private static EconoViewLastTransactionModel CreateModel(Transaction transaction)
	{
		return new EconoViewLastTransactionModel(EconoViewOverview.GenerateTransactionData(transaction), transaction.timestamp.Day, transaction.amount);
	}

	private void SortByDayDescending()
	{
		for (int i = 1; i < data.Count; i++)
		{
			EconoViewLastTransactionModel econoViewLastTransactionModel = data[i];
			int num = i - 1;
			while (num >= 0 && data[num].day < econoViewLastTransactionModel.day)
			{
				data[num + 1] = data[num];
				num--;
			}
			data[num + 1] = econoViewLastTransactionModel;
		}
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return cellSize;
	}

	private void ReloadData()
	{
		if (!(scroller == null))
		{
			scroller.Delegate = this;
			ScheduleReload();
		}
	}

	private void ScheduleReload()
	{
		if (_reloadCoroutine != null)
		{
			StopCoroutine(_reloadCoroutine);
		}
		_reloadCoroutine = StartCoroutine(ReloadWhenReady());
	}

	private IEnumerator ReloadWhenReady()
	{
		while (scroller != null && scroller.Container == null)
		{
			yield return null;
		}
		yield return null;
		_reloadCoroutine = null;
		if (!(scroller == null))
		{
			scroller.Delegate = this;
			scroller.ReloadData();
		}
	}
}
