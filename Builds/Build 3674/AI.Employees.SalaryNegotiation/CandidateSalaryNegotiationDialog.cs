using System;
using System.Collections.Generic;
using System.Linq;
using Dialogs;
using Entities;
using Extensions;
using Localizor;
using UI.Notification;
using UnityEngine;

namespace AI.Employees.SalaryNegotiation;

public class CandidateSalaryNegotiationDialog : Dialog
{
	public static CandidateSalaryNegotiation negotiation;

	private readonly CandidateSalaryNegotiator _negotiator;

	public CandidateSalaryNegotiationDialog()
	{
		_negotiator = new CandidateSalaryNegotiator(negotiation);
		DialogController.current.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_negotiation_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_health_insurance_negotiation_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ShowPlayerOfferInput());
			}
		};
	}

	private DialogEntry ShowPlayerOfferInput()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_send_offer_button".Localize(),
			CancelTextOverride = "dialog_decline_button",
			InputTemplate = DialogEntry.InputTemplateName.SalaryOffer,
			OnConfirm = OnPlayerOfferSet,
			OnCancel = CancelOffer,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call"),
			OnVisible = delegate
			{
				DialogController.current.GetInputComponent<SalaryOfferSettings>().SetInitialAmount(negotiation.offers.LastOrDefault((CandidateSalaryOffer x) => x.fromCandidate), _negotiator.GetMaxSigningBonus());
			}
		};
	}

	private DialogEntry OnPlayerOfferSet()
	{
		SalaryOfferSettings inputComponent = DialogController.current.GetInputComponent<SalaryOfferSettings>();
		CandidateSalaryOffer candidateSalaryOffer = default(CandidateSalaryOffer);
		if (!inputComponent.GetAmounts(ref candidateSalaryOffer) || candidateSalaryOffer.hourlyWage <= 0f || candidateSalaryOffer.signingBonus < 0f)
		{
			Notifications.ShowError("common_notification_invalid_amount");
			return null;
		}
		if (candidateSalaryOffer.signingBonus > SaveGameManager.Current.Money)
		{
			Notifications.ShowInsufficientMoney();
			return null;
		}
		negotiation.offers.Add(candidateSalaryOffer);
		var (flag, text) = _negotiator.EvaluateOffer(candidateSalaryOffer);
		if (flag)
		{
			negotiation.AcceptOffer(candidateSalaryOffer.hourlyWage, candidateSalaryOffer.signingBonus);
			SendProposedCandidateSalaryOfferMessage(candidateSalaryOffer);
			DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_candidate_salary_accepted_player_offer", null, read: true));
			return new DialogEntry
			{
				messageData = text.Localize(),
				OnVisible = DialogController.current.FinishDialog
			};
		}
		if (negotiation.IsDeclinedByMood)
		{
			negotiation.DeclineOffer();
			SendProposedCandidateSalaryOfferMessage(candidateSalaryOffer);
			DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_candidate_salary_declined_player_offer", null, read: true));
			return new DialogEntry
			{
				messageData = "dialog_candidate_salary_declined_player_offer".Localize(),
				OnVisible = DialogController.current.FinishDialog
			};
		}
		return ShowFeedback(text);
	}

	private static void SendProposedCandidateSalaryOfferMessage(CandidateSalaryOffer offerAmounts)
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				offerAmounts.hourlyWage.ToCurrencyFormat()
			},
			{
				"amount2",
				offerAmounts.signingBonus.ToCurrencyFormat()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_candidate_salary_player_offer", messageData, read: true));
	}

	private DialogEntry ShowFeedback(string feedbackKey)
	{
		(Sprite, Sprite) moodIcon = NegotiationHelper.GetMoodIcon(_negotiator);
		Sprite item = moodIcon.Item1;
		Sprite item2 = moodIcon.Item2;
		DialogEntry dialogEntry = new DialogEntry
		{
			messageData = feedbackKey.Localize(new
			{
				employeeName = negotiation.employeeInstance.characterData.name,
				amount = ((float)Math.Round(_negotiator.GetCounterOffer().Total, 2)).ToCurrencyFormat()
			}),
			Template = DialogEntry.TemplateType.Text,
			Icon = item,
			SecondaryIcon = item2
		};
		NegotiationOptions[] options = _negotiator.GetOptions();
		if (options.Contains(NegotiationOptions.Negotiate))
		{
			dialogEntry.SecondOptionTextOverride = "dialog_negotiate_button";
			dialogEntry.OnSecondOption = NegotiateCounterOffer;
		}
		if (options.Contains(NegotiationOptions.Accept))
		{
			dialogEntry.ConfirmTextOverride = "dialog_accept_button".Localize();
			dialogEntry.OnConfirm = AcceptCounterOffer;
		}
		if (options.Contains(NegotiationOptions.Decline))
		{
			dialogEntry.CancelTextOverride = "dialog_decline_button";
			dialogEntry.OnCancel = DeclineCounterOffer;
			dialogEntry.onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call");
		}
		return dialogEntry;
	}

	private DialogEntry AcceptCounterOffer()
	{
		if (_negotiator.GetCounterOffer().hourlyWage > SaveGameManager.Current.Money)
		{
			Notifications.ShowInsufficientMoney();
			return null;
		}
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"employeeName",
				negotiation.employeeInstance.characterData.name
			},
			{
				"amount",
				_negotiator.GetCounterOffer().Total.ToCurrencyFormat()
			}
		};
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_candidate_salary_counter_offer", messageData, read: true));
		return new DialogEntry
		{
			messageData = "dialog_candidate_salary_accepted_player_offer".Localize(),
			OnVisible = delegate
			{
				negotiation.AcceptOffer(_negotiator.GetCounterOffer().hourlyWage, _negotiator.GetCounterOffer().signingBonus);
				DialogController.current.FinishDialog();
			}
		};
	}

	private void DeclineCounterOffer()
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"employeeName",
				negotiation.employeeInstance.characterData.name
			},
			{
				"amount",
				_negotiator.GetCounterOffer().Total.ToCurrencyFormat()
			}
		};
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_candidate_salary_counter_offer", messageData, read: true));
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_negotiation_player_declined_counter_offer", null, read: true));
		negotiation.DeclineOffer();
		DialogController.current.FinishDialog();
	}

	private DialogEntry NegotiateCounterOffer()
	{
		return new DialogEntry
		{
			messageData = "dialog_negotiation_negotiate_counter_offer".Localize(),
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ShowPlayerOfferInput());
			}
		};
	}

	private void CancelOffer()
	{
		negotiation.DeclineOffer();
		DialogController.current.CancelDialog();
	}
}
