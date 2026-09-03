using System.Collections;
using BigAmbitions.Characters;
using UI;

public class TicketBoothEmployee : SelfServiceEmployee
{
	protected override IEnumerator ServeCustomer()
	{
		if (!customer)
		{
			customer = null;
			activeCoroutine = null;
			yield break;
		}
		customer.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_customerservice") * (employeeInstance.satisfaction / 100f);
		bool isCustomerPlayer = customer.isPlayer;
		if (isCustomerPlayer)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.cancelButton.interactable = false;
		}
		customer.state = CustomerState.BeingServed;
		yield return employeeTpc.animator.RunAnimation(AnimationType.UsingCashRegister, 3f);
		if (customer.order.Pay(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, base.transform.position, isCustomerPlayer))
		{
			if (isCustomerPlayer)
			{
				SelfServiceEmployee.UpdatePlayerPurchase();
			}
		}
		else if (isCustomerPlayer)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
		}
		employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
		TellCurrentCustomerToLeave();
	}
}
