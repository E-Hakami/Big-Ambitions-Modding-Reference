using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.Characters;
using BigAmbitions.InputSystem;
using BigAmbitions.Rivals;
using Buildings.Office.Headquarters;
using DG.Tweening;
using Dialogs;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UI.Components;
using UI.Notification;
using UI.Smartphone.Apps.MyEmployees;
using UI.Smartphone.Apps.Rivals;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Contacts;

public class ContactsApp : DialogController
{
	public static UnityEvent onContactAdded;

	public static UnityEvent onContactRemoved;

	public static List<string> contactIdsToWithCallButton = new List<string> { "auto_tow_service_ny", "uncle_fred", "speedy_bites" };

	private static readonly List<ContextButton> ContextButtons = new List<ContextButton>();

	public ContactScrollerController contactScrollerController;

	[SerializeField]
	private Transform timestampTemplate;

	[SerializeField]
	private Transform splitterTemplate;

	[SerializeField]
	private Button markAllAsReadButton;

	[SerializeField]
	private Button deleteAllButton;

	[Header("Conversation")]
	[SerializeField]
	private GameObject conversationPanel;

	[SerializeField]
	private CanvasGroup conversationViewportCanvas;

	[SerializeField]
	private CanvasGroup conversationScrollbarCanvas;

	[SerializeField]
	private GameObject scheduleDemandsGameObject;

	[SerializeField]
	private GameObject wageGameObject;

	[SerializeField]
	private GameObject skillLevelGameObject;

	[SerializeField]
	private Button callButton;

	[SerializeField]
	private Button viewOnMapButton;

	[SerializeField]
	private Button manageEmployeeButton;

	[SerializeField]
	private Button manageScheduleButton;

	[SerializeField]
	private Button manageDeliveryButton;

	[SerializeField]
	private GameObject candidatesButton;

	[SerializeField]
	private Button clearChatButton;

	[SerializeField]
	private Button rivalButton;

	[SerializeField]
	private Button taxesButton;

	[SerializeField]
	private ContextButton contextButtonPrefab;

	[Header("Header")]
	[SerializeField]
	private ContactIconView contactIconView;

	[SerializeField]
	private GameObject contactInfo;

	[SerializeField]
	private GameObject employeeInfo;

	[SerializeField]
	private GameObject addressGameObject;

	[SerializeField]
	private TextLocalizationComponent addressLabel;

	[SerializeField]
	private GameObject openingHoursGameObject;

	[SerializeField]
	private TextLocalizationComponent openingHoursLabel;

	[NonSerialized]
	public Contact selectedContact;

	private Coroutine _conversationTransitionCoroutine;

	private Coroutine _showConversationCoroutine;

	private object _lastTimeStamp;

	private static readonly List<(string contactName, Sprite contactIcon)> CachedContactIconSprites = new List<(string, Sprite)>();

	[SerializeField]
	private EmployeeContactIconSettings iconSettings;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onContactAdded = null;
		onContactRemoved = null;
		CachedContactIconSprites.Clear();
		ContextButtons.Clear();
	}

	private void Awake()
	{
		if (onContactAdded == null)
		{
			onContactAdded = new UnityEvent();
		}
		if (onContactRemoved == null)
		{
			onContactRemoved = new UnityEvent();
		}
		textTemplate.gameObject.SetActive(value: false);
		inputTemplate.gameObject.SetActive(value: false);
		timestampTemplate.gameObject.SetActive(value: false);
		splitterTemplate.gameObject.SetActive(value: false);
		conversationPanel.SetActive(value: false);
	}

	private void OnDestroy()
	{
		onContactAdded.RemoveAllListeners();
		onContactRemoved.RemoveAllListeners();
	}

	private void OnEnable()
	{
		if (SaveGameManager.Current == null)
		{
			return;
		}
		if (SaveGameManager.Current.PlayerDefaults.contactsLastName != null)
		{
			Contact contact = SaveGameManager.Current.Contacts.FirstOrDefault((Contact x) => x.id == SaveGameManager.Current.PlayerDefaults.contactsLastName && x.category == SaveGameManager.Current.PlayerDefaults.contactsLastCategoryName);
			if (contact != null)
			{
				OpenAppWithContact(contact);
			}
		}
		else
		{
			LoadContactsList(null);
			ResetMessages();
		}
	}

	private void OnDisable()
	{
		selectedContact = null;
		CancelDialog();
		ResetMessages();
	}

	public void MarkAllAsRead()
	{
		markAllAsReadButton.interactable = false;
		SaveGameManager.Current.Contacts.Where((Contact x) => !ContactCategorySelection.SelectedCategory.HasValue || x.category == ContactCategorySelection.SelectedCategory.Value).ForEach(delegate(Contact x)
		{
			x.messagesQueue.ForEach(delegate(TextMessage y)
			{
				y.read = true;
			});
		});
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
		LoadContactsList(ContactCategorySelection.SelectedCategory, scrollToSelected: false);
	}

	public void DeleteAll()
	{
		LanguageChangeEventDataHolder bodyData = "contacts_delete_all_button_confirm".Localize();
		Action onConfirmAction = OnDeleteConfirmed;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	private void OnDeleteConfirmed()
	{
		deleteAllButton.interactable = false;
		List<string> permanentIds = ContactsHelper.GetPermanentIds();
		int num = SaveGameManager.Current.Contacts.RemoveAll((Contact x) => !permanentIds.Contains(x.id) && (!ContactCategorySelection.SelectedCategory.HasValue || x.category == ContactCategorySelection.SelectedCategory.Value));
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
		ResetMessages();
		ShowDeleteAllContactsNotification();
		LoadContactsList(null);
		if (num > 0)
		{
			onContactRemoved?.Invoke();
		}
	}

	private static void ShowDeleteAllContactsNotification()
	{
		if (ContactCategorySelection.SelectedCategory.HasValue)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"name",
				ContactCategorySelection.SelectedCategory.Value.GetLocalizeKey()
			} };
			Notifications.Show(NotificationType.Success, "notification_deleted_all_contacts_from_category", notificationData);
		}
		else
		{
			Notifications.Show(NotificationType.Success, "notification_deleted_all_contacts");
		}
	}

	public bool ShowMessageIfOpen(Contact contact, TextMessage message)
	{
		if (selectedContact != contact)
		{
			return false;
		}
		ShowMessage(message);
		return true;
	}

	public void LoadContactsList(ContactCategoryName? category, bool scrollToSelected = true)
	{
		ContactsHelper.UpdateCategories();
		Contact[] array = (from x in SaveGameManager.Current.Contacts
			where !category.HasValue || x.category == category.Value
			orderby x.HasUnreadMessages descending, x.lastTimeUpdated
			select x).ToArray();
		contactScrollerController.LoadList(array, scrollToSelected, iconSettings);
		markAllAsReadButton.interactable = array.Any((Contact x) => x.HasUnreadMessages);
		deleteAllButton.interactable = array.Length != 0 && category.HasValue;
		ContactCategorySelection.SelectedCategory = category;
	}

	public static Sprite GetContactIcon(Contact contact)
	{
		if (contact == null)
		{
			return CreateContactIconSprite(null, LogoHelper.GetNullTexture());
		}
		Sprite sprite = contact.GetPredefinedIconSprite();
		if ((object)sprite == null)
		{
			sprite = GetCachedContactIcon(contact.id);
		}
		if (sprite != null)
		{
			return sprite;
		}
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(BuildingHelper.GetBuildingRegistration(contact.Address)?.BusinessName, LogoSize.SquareSign);
		return CreateContactIconSprite(contact.id, businessLogoTexture);
	}

	private static Sprite GetCachedContactIcon(string contactId)
	{
		foreach (var cachedContactIconSprite in CachedContactIconSprites)
		{
			if (cachedContactIconSprite.contactName == contactId)
			{
				return cachedContactIconSprite.contactIcon;
			}
		}
		return null;
	}

	private static Sprite CreateContactIconSprite(string contactId, Texture2D texture)
	{
		if (texture == null)
		{
			return null;
		}
		Rect rect = new Rect(0f, 0f, texture.width, texture.height);
		Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
		if (!string.IsNullOrEmpty(contactId))
		{
			CachedContactIconSprites.Add((contactId, sprite));
		}
		return sprite;
	}

	private void ShowContactConversation(Contact contact)
	{
		if (_showConversationCoroutine != null)
		{
			StopCoroutine(_showConversationCoroutine);
		}
		_showConversationCoroutine = StartCoroutine(ShowContactConversationIEnumerator(contact));
	}

	private IEnumerator ShowContactConversationIEnumerator(Contact contact)
	{
		ResetMessages();
		selectedContact = contact;
		contactScrollerController.UpdateSelectedCurrentContact();
		SetUpHeader(contact);
		yield return null;
		EnableConversationPanel();
		KeyboardInputHelper.SelectNextFrame(callButton);
		ShowContactMessages(contact);
		contact.ReadAllMessages();
		if ((bool)contact.contactCellView)
		{
			contact.contactCellView.ClearUnreadMessages();
		}
		FadeInConversationPanel();
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
		SaveGameManager.Current.PlayerDefaults.contactsLastName = contact.id;
		SaveGameManager.Current.PlayerDefaults.contactsLastCategoryName = contact.category;
	}

	public void OpenAppWithContact(Contact contact)
	{
		LoadContactsList(contact.category);
		ShowContactConversation(contact);
	}

	private void EnableConversationPanel()
	{
		conversationViewportCanvas.alpha = 0f;
		conversationViewportCanvas.DOKill();
		conversationScrollbarCanvas.alpha = 0f;
		conversationScrollbarCanvas.DOKill();
		conversationPanel.SetActive(value: true);
	}

	private void ShowContactMessages(Contact contact)
	{
		_lastTimeStamp = null;
		foreach (TextMessage item in contact.messagesQueue.AsQueryable().TakeLast(20))
		{
			ShowMessage(item);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(textTemplate.transform.parent.GetComponent<RectTransform>());
		ScrollConversationToBottom();
	}

	private void FadeInConversationPanel()
	{
		if (_conversationTransitionCoroutine != null)
		{
			StopCoroutine(_conversationTransitionCoroutine);
		}
		_conversationTransitionCoroutine = StartCoroutine(ConversationTransition());
	}

	private IEnumerator ConversationTransition()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		conversationViewportCanvas.DOFade(1f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		conversationScrollbarCanvas.DOFade(1f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
	}

	public void RemoveContact(Contact contact)
	{
		if (PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			RemoveContactFunction();
			return;
		}
		LanguageChangeEventDataHolder bodyData = "contacts_remove_contact_confirm".Localize();
		Action onConfirmAction = RemoveContactFunction;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		void RemoveContactFunction()
		{
			if (selectedContact == contact)
			{
				ResetMessages();
			}
			RemoveContextRelatedDataFromMessages(contact);
			SaveGameManager.Current.Contacts.Remove(contact);
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
			onContactRemoved?.Invoke();
			float normalizedScrollPosition = contactScrollerController.scroller.NormalizedScrollPosition;
			contactScrollerController.data.RemoveAll((ContactModel x) => x.contact == contact);
			contactScrollerController.scroller.ReloadData(normalizedScrollPosition);
		}
	}

	private void RemoveMessage(TextMessage message, Transform messageEntry, Transform splitter, Transform timeStamp)
	{
		if (PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			RemoveMessageFunction();
			return;
		}
		LanguageChangeEventDataHolder bodyData = "contacts_remove_message_confirm".Localize();
		Action onConfirmAction = RemoveMessageFunction;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		void RemoveMessageFunction()
		{
			if (splitter != null)
			{
				UnityEngine.Object.Destroy(splitter.gameObject);
			}
			if (timeStamp != null)
			{
				UnityEngine.Object.Destroy(timeStamp.gameObject);
			}
			List<TextMessage> list = selectedContact.messagesQueue.ToList();
			list.Remove(message);
			UnityEngine.Object.Destroy(messageEntry.gameObject);
			selectedContact.messagesQueue = new Queue<TextMessage>();
			foreach (TextMessage item in list)
			{
				selectedContact.messagesQueue.Enqueue(item);
			}
			if (message.contextAction != null)
			{
				if (!string.IsNullOrEmpty(message.contextAction.employeeInstanceId))
				{
					if (SaveGameManager.Current.CandidateEmployeeInstances.RemoveAll((EmployeeInstance x) => x.id == message.contextAction.employeeInstanceId) > 0)
					{
						EmployeeHelper.EmployeeInstancesDictionary.Remove(message.contextAction.employeeInstanceId);
					}
				}
				else if (!string.IsNullOrEmpty(message.contextAction.healthPlanOfferId))
				{
					SaveGameManager.Current.healthInsurancePlanOffers.RemoveAll((HealthInsurancePlanOffer x) => x.id == message.contextAction.healthPlanOfferId && !x.negotiationFinished);
				}
			}
			clearChatButton.interactable = selectedContact.messagesQueue.Count > 0;
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts);
		}
	}

	private void ShowMessage(TextMessage message)
	{
		Transform splitter = null;
		if (message.isNewInteraction)
		{
			splitter = UnityEngine.Object.Instantiate(splitterTemplate, splitterTemplate.parent);
			splitter.gameObject.SetActive(value: true);
		}
		var anon = new
		{
			day = message.timestamp.Day,
			time = message.timestamp.Hour.GetFormattedTime(message.timestamp.Minute)
		};
		Transform timeStamp = null;
		if (message.isNewInteraction || _lastTimeStamp != anon)
		{
			timeStamp = UnityEngine.Object.Instantiate(timestampTemplate, timestampTemplate.parent);
			timeStamp.GetComponent<TextLocalizationComponent>().SetData("timestamp_full".Localize(anon));
			timeStamp.gameObject.SetActive(value: true);
			_lastTimeStamp = anon;
		}
		Transform transform = (message.isFromPlayer ? inputTemplate.transform : textTemplate.transform);
		Transform messageEntry = UnityEngine.Object.Instantiate(transform, transform.parent);
		messageEntry.GetComponent<CanvasGroup>().alpha = 1f;
		if (message.messageKey == "ba:messagetype_taxes")
		{
			messageEntry.Find("SpecialMessage")?.gameObject.SetActive(value: false);
			messageEntry.Find("Message").gameObject.SetActive(value: false);
			messageEntry.Find("TaxesInfo").GetComponent<TaxesMessage>().SetData(message.additionalData?.taxes);
		}
		else if (message.messageKey == "ba:messagetype_contacts_taxes_repossession_message")
		{
			messageEntry.Find("SpecialMessage")?.gameObject.SetActive(value: false);
			messageEntry.Find("Message").gameObject.SetActive(value: false);
			messageEntry.Find("TaxesInfo").GetComponent<TaxesMessage>().SetData(message.additionalData?.listOfLabels, message.additionalData?.backTaxesOwed ?? 0f);
		}
		else
		{
			LanguageChangeEventDataHolder data = message.messageKey.Localize(message.messageData);
			if (message.isSpecialMessage)
			{
				messageEntry.Find("Message").gameObject.SetActive(value: false);
				messageEntry.GetLanguageChangeEventByName("SpecialMessage/MessageText").SetData(data);
			}
			else
			{
				messageEntry.Find("SpecialMessage")?.gameObject.SetActive(value: false);
				messageEntry.GetLanguageChangeEventByName("Message/MessageText").SetData(data);
			}
		}
		if (message.isFromPlayer)
		{
			messageEntry.Find("Message").gameObject.SetActive(value: true);
		}
		List<TextMessage.ContextButtonData> list = message.additionalData?.contextButtonData;
		if (list != null && list.Count > 0 && selectedContact.messagesQueue.Last() == message)
		{
			ShowButtonsContext(messageEntry, message);
		}
		if (message.contextAction != null)
		{
			switch (message.contextAction.type)
			{
			case TextMessage.ContextAction.ContextActionType.HealthInsurancePlanOffer:
				ShowHealthInsuranceInfo(messageEntry, message);
				break;
			case TextMessage.ContextAction.ContextActionType.SalaryNegotiation:
				ShowCandidateSalaryNegotiationInfo(messageEntry, message);
				break;
			}
		}
		Button buttonByName = messageEntry.GetButtonByName("Message/DeleteButton");
		buttonByName.onClick.AddListener(delegate
		{
			RemoveMessage(message, messageEntry, splitter, timeStamp);
		});
		buttonByName.gameObject.SetActive(value: true);
		messageEntry.gameObject.SetActive(value: true);
	}

	public void ContactButtonOnClick(ContactModel contactModel)
	{
		if (selectedContact != contactModel.contact)
		{
			ShowContactConversation(contactModel.contact);
		}
	}

	private void ShowButtonsContext(Transform messageEntry, TextMessage message)
	{
		Transform transform = messageEntry.Find("Buttons");
		transform.gameObject.SetActive(value: true);
		string groupId = Guid.NewGuid().ToString();
		foreach (TextMessage.ContextButtonData contextButtonDatum in message.additionalData.contextButtonData)
		{
			InitializeContextButton(transform, groupId, contextButtonDatum, message.additionalData);
		}
	}

	private void ShowHealthInsuranceInfo(Transform messageEntry, TextMessage message)
	{
		HealthInsurancePlanOffer healthInsurancePlanOffer = SaveGameManager.Current.healthInsurancePlanOffers.FirstOrDefault((HealthInsurancePlanOffer x) => x.id == message.contextAction.healthPlanOfferId);
		if (healthInsurancePlanOffer == null)
		{
			return;
		}
		Transform transform = messageEntry.Find("HealthInsuranceOfferInfo");
		if (healthInsurancePlanOffer.negotiationFinished)
		{
			if (healthInsurancePlanOffer.accepted)
			{
				transform.Find("Accepted").gameObject.SetActive(value: true);
			}
			else
			{
				transform.Find("Declined").gameObject.SetActive(value: true);
			}
		}
		else
		{
			InitHealthInsuranceButtons(messageEntry, message, healthInsurancePlanOffer, transform);
		}
		transform.GetLanguageChangeEventByName("PlanType").Key = healthInsurancePlanOffer.planType.GetLocalizeKey();
		string text = HrManagerHelper.GetPlanFromId(healthInsurancePlanOffer.hrManagerPlanId)?.HrManagerInstance?.characterData.name;
		transform.GetLabelByName("HRManager").text = text ?? "-";
		transform.GetLanguageChangeEventByName("Price").Arguments = new
		{
			price = healthInsurancePlanOffer.initialOfferPrice.ToCurrencyFormat()
		};
		transform.gameObject.SetActive(value: true);
	}

	private void InitHealthInsuranceButtons(Transform messageEntry, TextMessage message, HealthInsurancePlanOffer healthInsuranceOffer, Transform healthInsuranceOfferInfo)
	{
		Transform transform = messageEntry.Find("Buttons");
		transform.gameObject.SetActive(value: true);
		string groupId = Guid.NewGuid().ToString();
		InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_decline_button", ContextButton.BackgroundColor.gray, delegate
		{
			if (contact == null)
			{
				healthInsuranceOffer.DeclineOffer();
				healthInsuranceOfferInfo.Find("Declined").gameObject.SetActive(value: true);
			}
		}), message.additionalData);
		InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_negotiate_button", ContextButton.BackgroundColor.orange, delegate
		{
			if (contact == null)
			{
				callButton.interactable = false;
				contact = selectedContact;
				HealthInsuranceNegotiationDialog.planOffer = healthInsuranceOffer;
				DialogController.current = this;
				dialog = CallDialogFactory.GetDialog(CallDialogType.HealthInsuranceNegotiationDialog);
			}
		}), message.additionalData);
		InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_accept_button", ContextButton.BackgroundColor.blue, delegate
		{
			if (contact == null)
			{
				healthInsuranceOffer.AcceptOffer(healthInsuranceOffer.initialOfferPrice);
				healthInsuranceOfferInfo.Find("Accepted").gameObject.SetActive(value: true);
			}
		}), message.additionalData);
	}

	private void ShowCandidateSalaryNegotiationInfo(Transform messageEntry, TextMessage message)
	{
		CandidateSalaryNegotiation candidateSalaryNegotiation = SaveGameManager.Current.candidateSalaryNegotiations.FirstOrDefault((CandidateSalaryNegotiation x) => x.id == message.contextAction.salaryNegotiationId);
		if (candidateSalaryNegotiation == null)
		{
			return;
		}
		Transform candidateSalaryOffer = messageEntry.Find("CandidateSalaryOfferInfo");
		if (candidateSalaryNegotiation.completed)
		{
			if (candidateSalaryNegotiation.accepted)
			{
				candidateSalaryOffer.Find("Accepted").gameObject.SetActive(value: true);
			}
			else
			{
				candidateSalaryOffer.Find("Declined").gameObject.SetActive(value: true);
			}
		}
		else
		{
			Transform transform = messageEntry.Find("Buttons");
			transform.gameObject.SetActive(value: true);
			string groupId = Guid.NewGuid().ToString();
			InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_decline_button", ContextButton.BackgroundColor.gray, delegate
			{
				if (contact == null)
				{
					candidateSalaryNegotiation.DeclineOffer();
					candidateSalaryOffer.Find("Declined").gameObject.SetActive(value: true);
				}
			}), message.additionalData);
			InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_negotiate_button", ContextButton.BackgroundColor.orange, delegate
			{
				if (contact == null)
				{
					callButton.interactable = false;
					contact = selectedContact;
					CandidateSalaryNegotiationDialog.negotiation = candidateSalaryNegotiation;
					DialogController.current = this;
					dialog = CallDialogFactory.GetDialog(CallDialogType.CandidateSalaryNegotiationDialog);
				}
			}), message.additionalData);
			InitializeContextButton(transform, groupId, new TextMessage.ContextButtonData("dialog_accept_button", ContextButton.BackgroundColor.blue, delegate
			{
				if (contact == null)
				{
					if (candidateSalaryNegotiation.signingBonus > SaveGameManager.Current.Money)
					{
						Notifications.ShowInsufficientMoney();
					}
					else
					{
						candidateSalaryNegotiation.AcceptOffer(candidateSalaryNegotiation.hourlyWage, candidateSalaryNegotiation.signingBonus);
						candidateSalaryOffer.Find("Accepted").gameObject.SetActive(value: true);
					}
				}
			}), message.additionalData);
		}
		candidateSalaryOffer.GetLabelByName("Candidate Name").text = candidateSalaryNegotiation.employeeInstance.characterData.name ?? "-";
		candidateSalaryOffer.GetLanguageChangeEventByName("Hourly Wage").Arguments = new
		{
			price = candidateSalaryNegotiation.hourlyWage.ToCurrencyFormat()
		};
		candidateSalaryOffer.gameObject.SetActive(value: true);
	}

	private void InitializeContextButton(Transform parentTransform, string groupId, TextMessage.ContextButtonData buttonData, AdditionalMessageData messageData)
	{
		ContextButton contextButton = UnityEngine.Object.Instantiate(contextButtonPrefab, parentTransform);
		contextButton.SetUp(groupId, buttonData, messageData);
		ContextButtons.Add(contextButton);
	}

	public static void SetContextButtonsNoninteractable(string groupId)
	{
		List<ContextButton> buttons = ContextButtons.Where((ContextButton x) => x.groupId == groupId).ToList();
		buttons.ForEach(delegate(ContextButton x)
		{
			x.SetInteractable(interactable: false);
		});
		ContextButtons.RemoveAll((ContextButton x) => buttons.Contains(x));
		InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.RefreshHeader();
	}

	public void RefreshHeader()
	{
		SetUpHeader(selectedContact);
	}

	private void SetUpHeader(Contact contact)
	{
		EmployeeInstance employeeInstance = null;
		bool isCandidate = false;
		bool flag = false;
		if (contact.IsEmployeeContact)
		{
			foreach (EmployeeInstance employeeInstance2 in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				excludeBeingReplaced = true
			}))
			{
				if (employeeInstance2 != null && !(employeeInstance2.characterData.name != contact.id))
				{
					employeeInstance = employeeInstance2;
					break;
				}
			}
			if (employeeInstance == null)
			{
				isCandidate = true;
				foreach (CandidateSalaryNegotiation candidateSalaryNegotiation in SaveGameManager.Current.candidateSalaryNegotiations)
				{
					if (candidateSalaryNegotiation?.employeeInstance != null && !(candidateSalaryNegotiation.employeeInstance.characterData.name != contact.id))
					{
						employeeInstance = candidateSalaryNegotiation.employeeInstance;
						break;
					}
				}
			}
			if (iconSettings != null && ContactsHelper.TryGetFirstNameInitial(contact.id, out var initial))
			{
				Gender? gender = employeeInstance?.characterData.gender;
				ContactIconData contactIconData = iconSettings.Resolve(initial, gender);
				contactIconView.SetLetterIcon(contactIconData.Sprite, contactIconData.Tint);
				flag = true;
			}
		}
		if (!flag)
		{
			contactIconView.SetSquareIcon(GetContactIcon(contact));
		}
		if (employeeInstance != null)
		{
			employeeInfo.transform.GetLabelByName("ContactName").text = contact.id;
			employeeInfo.transform.GetLanguageChangeEventByName("EmployeeType").Key = employeeInstance.GetPrimarySkill();
			employeeInfo.transform.GetLabelByName("EmployeeBusiness").text = (employeeInstance.IsAssignedToAnyBusiness() ? BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress).BusinessName : "common_unassigned".GetLocalization());
			StringBuilder stringBuilder = new StringBuilder();
			bool flag2 = true;
			foreach (string demand in employeeInstance.demands)
			{
				if (JobDemandHelper.GetByName(demand) is IScheduleDemand)
				{
					if (!flag2)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(demand.GetLocalization());
					flag2 = false;
				}
			}
			scheduleDemandsGameObject.transform.GetLabelByName("ScheduleDemandsLabel").text = stringBuilder.ToString();
			scheduleDemandsGameObject.SetActive(value: true);
			wageGameObject.transform.GetLanguageChangeEventByName("WageLabel").Arguments = new
			{
				wage = employeeInstance.hourlyWage.ToCurrencyFormat()
			};
			wageGameObject.SetActive(value: true);
			skillLevelGameObject.transform.GetLabelByName("SkillLevelLabel").text = $"{Mathf.FloorToInt(employeeInstance.GetSkillValue(employeeInstance.GetPrimarySkill()))}%";
			skillLevelGameObject.SetActive(value: true);
			SetupEmployeeButtons(employeeInstance, isCandidate);
			contactInfo.SetActive(value: false);
			employeeInfo.SetActive(value: true);
		}
		else
		{
			TextLocalizationComponent languageChangeEventByName = contactInfo.transform.GetLanguageChangeEventByName("ContactName");
			if (LocalizorManager.IsLocalizedKey(contact.id))
			{
				languageChangeEventByName.Key = contact.id;
			}
			else
			{
				languageChangeEventByName.Key = "";
				languageChangeEventByName.SetValue(contact.id);
			}
			contactInfo.transform.GetLanguageChangeEventByName("ContactDescription").Key = contact.description;
			scheduleDemandsGameObject.SetActive(value: false);
			wageGameObject.SetActive(value: false);
			skillLevelGameObject.SetActive(value: false);
			manageScheduleButton.gameObject.SetActive(value: false);
			manageEmployeeButton.gameObject.SetActive(value: false);
			employeeInfo.SetActive(value: false);
			contactInfo.SetActive(value: true);
		}
		SetupManageDeliveryButton(contact);
		clearChatButton.interactable = contact.messagesQueue.Count > 0;
		bool flag3 = contact.category == ContactCategoryName.Rivals;
		rivalButton.gameObject.SetActive(flag3);
		if (flag3)
		{
			SpecialRival specialRival = RivalsHelper.GetSpecialRivals().FirstOrDefault((SpecialRival x) => x.rivalData.rivalName == contact.id);
			if (specialRival != null)
			{
				RivalLeaderboardData rivalLeaderboardData = RivalLeaderboard.GetRivalLeaderboardData(specialRival.rivalData);
				if (rivalLeaderboardData != null)
				{
					rivalButton.onClick.RemoveAllListeners();
					rivalButton.onClick.AddListener(delegate
					{
						InstanceBehavior<UIs>.Instance.smartphoneUI.OpenApp("Rivals");
						InstanceBehavior<RivalsApp>.Instance.ShowRival(rivalLeaderboardData);
					});
				}
			}
		}
		bool active = TaxHelper.IsIrsContact(contact);
		taxesButton.gameObject.SetActive(active);
		if (contact.Address.IsUndefined())
		{
			addressGameObject.SetActive(value: false);
			openingHoursGameObject.gameObject.SetActive(value: false);
			if (contact.callDialogTypeOverride != CallDialogType.NotImplemented || contactIdsToWithCallButton.Contains(contact.id))
			{
				callButton.onClick.RemoveAllListeners();
				callButton.onClick.AddListener(delegate
				{
					CallButtonOnClick(contact, businessIsOpen: true);
				});
				callButton.gameObject.SetActive(value: true);
			}
			else
			{
				callButton.gameObject.SetActive(value: false);
			}
			viewOnMapButton.gameObject.SetActive(value: false);
			candidatesButton.SetActive(value: false);
			return;
		}
		candidatesButton.SetActive(BuildingHelper.GetBuildingRegistration(contact.Address).businessTypeName == "ba:businesstype_recruitmentagency");
		manageEmployeeButton.gameObject.SetActive(value: false);
		addressLabel.SetValue(contact.Address.ToFormattedString(), clearKey: true);
		addressGameObject.SetActive(value: true);
		if (contact.id == "hospital_health_insurance_manager")
		{
			openingHoursGameObject.SetActive(value: false);
			callButton.onClick.RemoveAllListeners();
			callButton.onClick.AddListener(delegate
			{
				CallButtonOnClick(contact, businessIsOpen: true);
			});
			callButton.gameObject.SetActive(value: true);
			viewOnMapButton.onClick.RemoveAllListeners();
			viewOnMapButton.onClick.AddListener(delegate
			{
				ViewOnMapButtonOnClick(contact);
			});
			viewOnMapButton.gameObject.SetActive(value: true);
			return;
		}
		bool businessIsOpen = false;
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(contact.Address);
		List<ScheduleDay> scheduleDays = buildingRegistration.scheduleDays;
		if (scheduleDays != null && scheduleDays.Count != 0)
		{
			ScheduleDay scheduleDay = scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
			OpeningHourSlot openingHourSlot = scheduleDay?.openingHourSlots?.FirstOrDefault();
			string key = "contacts_today_opening_hours_closed";
			object arguments = null;
			if (openingHourSlot != null && scheduleDay.isOpen)
			{
				key = "contacts_today_opening_hours_open";
				arguments = new
				{
					startingHour = openingHourSlot.startingHour.GetFormattedTime(),
					endingHour = openingHourSlot.endingHour.GetFormattedTime()
				};
				if (SaveGameManager.Current.Hour.InRange(openingHourSlot.startingHour, openingHourSlot.endingHour - 1))
				{
					businessIsOpen = true;
				}
			}
			openingHoursLabel.SetData(key.Localize(arguments));
			openingHoursGameObject.SetActive(value: true);
		}
		else
		{
			Debug.LogError("Opening hours not found for " + buildingRegistration.BusinessName);
			openingHoursGameObject.SetActive(value: false);
		}
		callButton.onClick.RemoveAllListeners();
		BusinessType data = BusinessTypeHelper.GetData(BuildingHelper.GetBuildingRegistration(contact.Address));
		if ((object)data == null || data.callDialogType != CallDialogType.NotImplemented)
		{
			callButton.onClick.AddListener(delegate
			{
				CallButtonOnClick(contact, businessIsOpen);
			});
			callButton.gameObject.SetActive(value: true);
		}
		else
		{
			callButton.gameObject.SetActive(value: false);
		}
		viewOnMapButton.onClick.RemoveAllListeners();
		viewOnMapButton.onClick.AddListener(delegate
		{
			ViewOnMapButtonOnClick(contact);
		});
		viewOnMapButton.gameObject.SetActive(value: true);
	}

	private void SetupEmployeeButtons(EmployeeInstance employeeInstance, bool isCandidate)
	{
		if (isCandidate)
		{
			manageEmployeeButton.gameObject.SetActive(value: false);
			manageScheduleButton.gameObject.SetActive(value: false);
			return;
		}
		manageEmployeeButton.gameObject.SetActive(value: true);
		manageEmployeeButton.onClick.RemoveAllListeners();
		manageEmployeeButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(employeeInstance);
		});
		manageScheduleButton.gameObject.SetActive(value: true);
		manageScheduleButton.onClick.RemoveAllListeners();
		manageScheduleButton.onClick.AddListener(delegate
		{
			UI.Smartphone.Apps.MyEmployees.MyEmployees.ManageSchedule(employeeInstance);
		});
	}

	private void SetupManageDeliveryButton(Contact contact)
	{
		if (contact.id != "logistics_alerts")
		{
			manageDeliveryButton.gameObject.SetActive(value: false);
			return;
		}
		List<Address> headquartersAddresses = GetHeadquartersAddressesWithPlans();
		if (headquartersAddresses.Count == 0)
		{
			manageDeliveryButton.gameObject.SetActive(value: false);
			return;
		}
		manageDeliveryButton.gameObject.SetActive(value: true);
		manageDeliveryButton.onClick.RemoveAllListeners();
		manageDeliveryButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
			if (headquartersAddresses.Count == 1)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(headquartersAddresses[0], "LogisticsManagers");
			}
			else
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open();
			}
		});
	}

	private static List<Address> GetHeadquartersAddressesWithPlans()
	{
		List<Address> list = new List<Address>();
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			if (logisticsManagerPlan != null && !logisticsManagerPlan.headquartersAddress.IsUndefined() && !list.Contains(logisticsManagerPlan.headquartersAddress))
			{
				list.Add(logisticsManagerPlan.headquartersAddress);
			}
		}
		return list;
	}

	public void StartCall()
	{
		callButton.onClick.Invoke();
	}

	public void ClearChat()
	{
		if (PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			ClearChatFunction();
			return;
		}
		LanguageChangeEventDataHolder bodyData = "contacts_clear_chat_confirm".Localize();
		Action onConfirmAction = ClearChatFunction;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		void ClearChatFunction()
		{
			CancelDialog();
			RemoveContextRelatedDataFromMessages(selectedContact);
			selectedContact.messagesQueue = new Queue<TextMessage>();
			clearChatButton.interactable = false;
			ResetEntriesComponents();
		}
	}

	public static void RemoveContextRelatedDataFromMessages(Contact fromContact)
	{
		foreach (TextMessage message in fromContact.messagesQueue)
		{
			if (message.contextAction == null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(message.contextAction.employeeInstanceId))
			{
				if (SaveGameManager.Current.CandidateEmployeeInstances.RemoveAll((EmployeeInstance x) => x.id == message.contextAction.employeeInstanceId) > 0)
				{
					EmployeeHelper.EmployeeInstancesDictionary.Remove(message.contextAction.employeeInstanceId);
				}
			}
			else if (!string.IsNullOrEmpty(message.contextAction.healthPlanOfferId))
			{
				SaveGameManager.Current.healthInsurancePlanOffers.RemoveAll((HealthInsurancePlanOffer x) => x.id == message.contextAction.healthPlanOfferId && !x.negotiationFinished);
			}
			if (string.IsNullOrEmpty(message.contextAction.salaryNegotiationId))
			{
				continue;
			}
			CandidateSalaryNegotiation candidateSalaryNegotiation = SaveGameManager.Current.candidateSalaryNegotiations.FirstOrDefault((CandidateSalaryNegotiation x) => x.id == message.contextAction.salaryNegotiationId);
			if (candidateSalaryNegotiation != null)
			{
				if (!candidateSalaryNegotiation.completed)
				{
					candidateSalaryNegotiation.DeclineOffer();
				}
				SaveGameManager.Current.candidateSalaryNegotiations.Remove(candidateSalaryNegotiation);
			}
		}
	}

	private void CallButtonOnClick(Contact contact, bool businessIsOpen)
	{
		clearChatButton.interactable = true;
		if (!businessIsOpen)
		{
			TextMessage textMessage = new TextMessage("ba:messagetype_contacts_message_calling_outside_working_hours", null, read: true);
			contact.SendMessage(textMessage);
			ShowMessage(textMessage);
			ScrollConversationToBottom();
		}
		else if (!contact.Address.IsUndefined() && contact.Address == InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.Address)
		{
			TextMessage textMessage2 = new TextMessage("ba:messagetype_contacts_message_occupied", null, read: true);
			contact.SendMessage(textMessage2);
			ShowMessage(textMessage2);
			ScrollConversationToBottom();
		}
		else
		{
			callButton.interactable = false;
			StartCall(contact);
		}
	}

	public void ShowCandidates()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.initialTab = "Candidates";
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
	}

	public void OnTaxButtonClick()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.EconoView);
		InstanceBehavior<UIs>.Instance.fullMenu.econoView.OpenTaxes();
	}

	public void OnConfirm()
	{
		ConfirmCurrentEntry();
	}

	public void OnSecondOption()
	{
		SecondOptionCurrentEntry();
	}

	private void StartCall(Contact contactToCall)
	{
		DialogController.current = this;
		contact = contactToCall;
		if (contactToCall.callDialogTypeOverride != CallDialogType.NotImplemented)
		{
			dialog = CallDialogFactory.GetDialog(contactToCall.callDialogTypeOverride);
			return;
		}
		switch (contact.id)
		{
		case "auto_tow_service_ny":
			dialog = CallDialogFactory.GetDialog(CallDialogType.AutoTowServiceDialog);
			return;
		case "uncle_fred":
			dialog = CallDialogFactory.GetDialog(CallDialogType.UncleFredDialog);
			return;
		case "hospital_health_insurance_manager":
			dialog = CallDialogFactory.GetDialog(CallDialogType.HealthInsuranceManagerDialog);
			return;
		case "speedy_bites":
			dialog = CallDialogFactory.GetDialog(CallDialogType.FoodDeliveryDialog);
			return;
		}
		CallDialogType callDialogType = BusinessTypeHelper.GetData(BuildingHelper.GetBuildingRegistration(contact.Address)).callDialogType;
		if (callDialogType == CallDialogType.NotImplemented)
		{
			CallANotImplementedBusiness();
		}
		dialog = CallDialogFactory.GetDialog(callDialogType);
	}

	public override void FinishDialog()
	{
		if (contact != null)
		{
			base.FinishDialog();
			DialogController.current = null;
			dialog = null;
			contact = null;
			callButton.interactable = true;
		}
	}

	public override void CancelDialog()
	{
		if (contact != null)
		{
			base.CancelDialog();
			CancelMessageFading();
			DialogController.current = null;
			dialog = null;
			contact = null;
			callButton.interactable = true;
		}
	}

	private void ViewOnMapButtonOnClick(Contact contactToView)
	{
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(contactToView.Address);
		InstanceBehavior<CityManager>.Instance.cityMap.buildingToFocus = cityBuildingController;
		InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
		if (CityMap.IsOpen)
		{
			Transform parent = InstanceBehavior<GameManager>.Instance.citymapCamera.transform.parent;
			Vector3 position = cityBuildingController.entranceDoors[0].doorTransform.position;
			Vector3 position2 = parent.position;
			position2.x = position.x;
			position2.z = position.z;
			parent.position = position2;
			InstanceBehavior<CityManager>.Instance.cityMap.cityMapCam.ForceUpdateCameraPosition();
		}
		else
		{
			InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
		}
	}

	private void ResetMessages()
	{
		CancelDialog();
		conversationPanel.SetActive(value: false);
		selectedContact = null;
		ResetEntriesComponents();
	}

	private void CallANotImplementedBusiness()
	{
		string businessTypeName = BuildingHelper.GetBuildingRegistration(contact.Address).businessTypeName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "business_type", businessTypeName } };
		TextMessage textMessage = new TextMessage("ba:messagetype_contacts_message_not_implemented", messageData, read: true);
		contact.SendMessage(textMessage);
		ShowMessage(textMessage);
		ScrollConversationToBottom();
		DialogController.current = null;
		contact = null;
		callButton.interactable = true;
	}

	public static void OpenAndStartCall(Contact contact)
	{
		SaveGameManager.Current.PlayerDefaults.contactsLastName = contact.id;
		SaveGameManager.Current.PlayerDefaults.contactsLastCategoryName = contact.category;
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.StartCall();
		});
	}
}
