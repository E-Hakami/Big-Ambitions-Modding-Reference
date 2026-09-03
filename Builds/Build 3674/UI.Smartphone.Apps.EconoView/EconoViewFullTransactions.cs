using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using UI.Elements;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewFullTransactions : MonoBehaviour
{
	private const string TransactionsCSVFileName = "Transactions.csv";

	[SerializeField]
	private Dropdown typesDropdown;

	[SerializeField]
	private Dropdown amountDropdown;

	[SerializeField]
	private Dropdown daysDropdown;

	[SerializeField]
	private EconoViewFullTransactionsScrollerController scrollerController;

	private IEnumerable<Transaction> _transactions;

	private List<string> _transactionTypes;

	private List<int> _days;

	private string _selectedTransactionType;

	private Transaction.AmountOption _selectedAmountOption;

	private int _selectedDay;

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		_transactions = SaveGameManager.Current.Transactions.AsEnumerable();
		SetUpTypesDropdown();
		SetUpAmountsDropdown();
		SetUpDaysDropdown();
		_selectedTransactionType = string.Empty;
		_selectedAmountOption = Transaction.AmountOption.All;
		_selectedDay = -1;
		CoroutineUtility.RunAfterOneFrame(RefreshTransactionsList);
	}

	private void SetUpTypesDropdown()
	{
		_transactionTypes = new List<string>();
		List<string> list = new List<string> { "econoview_full_transactions_all_types" };
		_transactionTypes.Add(string.Empty);
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Transaction transaction in _transactions)
		{
			string transactionType = transaction.transactionType;
			if (!string.IsNullOrEmpty(transactionType) && hashSet.Add(transactionType))
			{
				list.Add(transactionType + "_label");
				_transactionTypes.Add(transactionType);
			}
		}
		typesDropdown.SetOptions(list, localize: true, 0);
		typesDropdown.onOptionSelected.AddListener(SelectType);
	}

	private void SelectType(int index)
	{
		_selectedTransactionType = _transactionTypes[index];
		RefreshTransactionsList();
	}

	private void SetUpAmountsDropdown()
	{
		List<string> list = new List<string>();
		Transaction.AmountOption[] array = (Transaction.AmountOption[])Enum.GetValues(typeof(Transaction.AmountOption));
		foreach (Transaction.AmountOption amountOption in array)
		{
			list.Add(amountOption.GetLocalizeKey());
		}
		amountDropdown.SetOptions(list, localize: true, 0);
		amountDropdown.onOptionSelected.AddListener(SelectAmountOption);
	}

	private void SelectAmountOption(int index)
	{
		_selectedAmountOption = (Transaction.AmountOption)index;
		RefreshTransactionsList();
	}

	private void SetUpDaysDropdown()
	{
		_days = new List<int> { -1 };
		List<string> list = new List<string> { "econoview_full_transactions_all_days".Localize().ToString() };
		for (int i = 0; i < 60; i++)
		{
			int num = SaveGameManager.Current.Day - i;
			_days.Add(num);
			list.Add("common_day_number".Localize(new
			{
				number = num
			}).ToString());
			if (SaveGameManager.Current.Day - i == 0)
			{
				break;
			}
		}
		daysDropdown.SetOptions(list, localize: false, 0);
		daysDropdown.onOptionSelected.AddListener(SelectDay);
	}

	private void SelectDay(int index)
	{
		_selectedDay = _days[index];
		RefreshTransactionsList();
	}

	public void RefreshTransactionsList()
	{
		_transactions = SaveGameManager.Current.Transactions.AsEnumerable();
		if (_transactions == null)
		{
			return;
		}
		if (_selectedDay != -1)
		{
			_transactions = _transactions.Where((Transaction x) => x.timestamp.Day == _selectedDay);
		}
		if (!string.IsNullOrEmpty(_selectedTransactionType))
		{
			_transactions = _transactions.Where((Transaction x) => x.transactionType == _selectedTransactionType);
		}
		if (_selectedAmountOption == Transaction.AmountOption.Positive)
		{
			_transactions = _transactions.Where((Transaction x) => x.amount > 0f);
		}
		else if (_selectedAmountOption == Transaction.AmountOption.Negative)
		{
			_transactions = _transactions.Where((Transaction x) => x.amount < 0f);
		}
		_transactions = from x in _transactions.Reverse()
			orderby x.timestamp.Day descending
			select x;
		scrollerController.Load(_transactions);
	}

	public void ExportToCsv()
	{
		string characterFolderPath = SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId);
		CreateCsvFile(characterFolderPath);
		FolderHelper.OpenFolder(characterFolderPath);
	}

	private void CreateCsvFile(string folderPath)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Transaction transaction in _transactions)
		{
			string text = EconoViewOverview.GenerateTransactionData(transaction).ToString().Replace("\"", "\"\"");
			int day = transaction.timestamp.Day;
			string text2 = (transaction.transactionType + "_label").Localize().ToString();
			float amount = transaction.amount;
			float balance = transaction.balance;
			string value = $"\"{text}\",\"{day}\",\"{text2}\",\"{amount}\",\"{balance}\"";
			stringBuilder.AppendLine(value);
		}
		File.WriteAllText(Path.Combine(folderPath, "Transactions.csv"), stringBuilder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
