using System.Collections;
using BigAmbitions.Characters;
using Entities;
using Helpers;
using UI;
using UI.Notification;
using UI.Purchase;
using UnityEngine;

public class IRSEmployee : Employee
{
	protected override void Update()
	{
		if (IsEmployeeAvailable())
		{
			CallForNextCustomer();
			TryStartToiletCoroutine();
		}
	}

	public override IEnumerator CancelCurrentOrder()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
			activeCoroutine = null;
			customer = null;
		}
		yield return new WaitForEndOfFrame();
	}

	protected override IEnumerator ServeCustomer()
	{
		if (customer == null)
		{
			customer = null;
			activeCoroutine = null;
			yield break;
		}
		bool isCustomerPlayer = customer.CompareTag("Player");
		if (isCustomerPlayer)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.cancelButton.interactable = false;
		}
		customer.state = CustomerState.BeingServed;
		yield return employeeTpc.animator.RunAnimation(AnimationType.UsingCashRegister);
		yield return new WaitForSeconds(0.5f);
		if (isCustomerPlayer)
		{
			PurchaseUI purchaseUI = InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI;
			if (!((purchaseUI.CurrentTaxPaymentType == TaxPaymentType.BackTaxes) ? TaxHelper.PayBackTaxes(purchaseUI.CurrentTaxPaymentAmount) : TaxHelper.PayCurrentTaxes(purchaseUI.CurrentTaxPaymentAmount)))
			{
				Notifications.ShowInsufficientMoney();
			}
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
		}
		yield return new WaitForSeconds(0.1f);
		TellCurrentCustomerToLeave();
	}

	private void TellCurrentCustomerToLeave()
	{
		if ((bool)customer)
		{
			customer.Leave();
			employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
			customer = null;
			activeCoroutine = null;
		}
	}
}
