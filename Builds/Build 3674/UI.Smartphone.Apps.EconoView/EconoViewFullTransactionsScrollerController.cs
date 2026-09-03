using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewFullTransactionsScrollerController : BaTable<EconoViewTransactionCellView, TransactionModel>
{
	public void Load(IEnumerable<Transaction> transactions)
	{
		data.Clear();
		data = transactions.Select((Transaction transaction) => new TransactionModel(EconoViewOverview.GenerateTransactionData(transaction), transaction.timestamp.Day, transaction.transactionType, transaction.amount, transaction.balance)).ToList();
		ResetFilters();
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
