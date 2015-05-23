using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Dataspin; //Include this in order to use Dataspin SDK in your scripts

public class DataspinExample : MonoBehaviour {

	public Text configText;
	public Text statusText;
	public Text uuidText;
	public Text deviceUuidText;
	public Text logText;
	public Text nameText;
	public Text surnameText;
	public Text emailText;
	public Text sessionsCounter;

	public InputField itemAmountField;
	public InputField customEventId;
	public InputField customEventData;
	public ComboBox comboItemsBox;

	public Button comboItemsButton;

	public Button startSessionButton;
	public GameObject sessionActions;

	private int sessionsCount;

	private void OnEnable() { //Subscribe to desired events
		DataspinManager.OnUserRegistered += OnUserRegistered;
		DataspinManager.OnDeviceRegistered += OnDeviceRegistered;
		DataspinManager.OnSessionStarted += OnSessionStarted;
		DataspinManager.OnSessionEnded += OnSessionEnded;
		DataspinManager.OnEventRegistered += OnEventRegistered;
		DataspinManager.OnItemPurchased += OnItemPurchased;
		DataspinManager.OnItemsRetrieved += OnItemsRetrieved;
		DataspinManager.OnErrorOccured += OnErrorOccured;
		DataspinManager.OnItemPurchaseVerification += OnItemPurchaseVerification;
	}

	private void OnDisable() { //Unsubscribe after game quit/scene reload etc.
		DataspinManager.OnUserRegistered -= OnUserRegistered;
		DataspinManager.OnDeviceRegistered -= OnDeviceRegistered;
		DataspinManager.OnSessionStarted -= OnSessionStarted;
		DataspinManager.OnItemsRetrieved -= OnItemsRetrieved;
		DataspinManager.OnEventRegistered -= OnEventRegistered;
		DataspinManager.OnEventRegistered -= OnEventRegistered;
		DataspinManager.OnItemPurchased -= OnItemPurchased;
		DataspinManager.OnErrorOccured -= OnErrorOccured;
		DataspinManager.OnItemPurchaseVerification -= OnItemPurchaseVerification;
	}

	//On Start we Register User
	void Start () {
		sessionActions.SetActive(false);
		statusText.text = "Offline";
		configText.text = "Current config: "+DataspinManager.Instance.CurrentConfiguration.ToString();
		DataspinManager.Instance.RegisterUser();
	}

	public void UpdateUser() {
		DataspinManager.Instance.RegisterUser(nameText.text, surnameText.text, emailText.text,null,null,null, true);
	}

	public void StartSession() {
		DataspinManager.Instance.StartSession();
	}

	public void EndSession() {
		DataspinManager.Instance.EndSession();
	}

	public void GetItems() {
		DataspinManager.Instance.GetItems();
	}

	public void GetCustomEvents() {
		// DEPRECATED
		// DataspinManager.Instance.GetCustomEvents();
	}

	public void ReloadScene() {
		Application.LoadLevel(Application.loadedLevel);
	}

	public void RawPurchaseItem(string name) {
		DataspinManager.Instance.PurchaseItem(name, 1);
	}

	public void PurchaseItem() {
		if(itemAmountField.text.Length > 0) { 
			int amount = int.Parse(itemAmountField.text);
			DataspinManager.Instance.PurchaseItem(DataspinManager.Instance.FindItemByName(comboItemsBox.selected.textComponent.text).InternalId, amount);
		}
		else {
			DataspinItem item = DataspinManager.Instance.FindItemByName(comboItemsBox.selected.textComponent.text);
			if(item != null)
				DataspinManager.Instance.PurchaseItem(DataspinManager.Instance.FindItemByName(comboItemsBox.selected.textComponent.text).InternalId);
			else 
				Debug.Log("No such item!");
		}
	}

	public void RegisterCustomEvent() {
		DataspinManager.Instance.RegisterCustomEvent(customEventId.text, customEventData.text);
	}

	#region Listeners
	private void OnUserRegistered(string uuid) {
		statusText.text = "User registered, session not started";
		//User registered, Now you can register device automatically or provide some parameters and call it manually after this event
		DataspinManager.Instance.RegisterDevice();
		uuidText.text = "UUID: "+uuid;
	}

	private void OnDeviceRegistered() {
		//DataspinManager.Instance.RegisterDevice();
		statusText.text = "User & device registered, session not started";
		startSessionButton.interactable = true;
		deviceUuidText.text = "Device UUID: "+DataspinManager.Instance.Device_UUID;
	}

	private void OnSessionStarted() {
		sessionsCount++;
		sessionsCounter.text = "Sessions Count: "+sessionsCount.ToString();
		statusText.text = "Session Started - All OK";
		sessionActions.SetActive(true);
	}

	private void OnSessionEnded() {
		statusText.text = "User & device registered, session not started";
		sessionActions.SetActive(false);
	}

	private void OnEventRegistered(string eventId) {
		logText.text = "Event " + eventId + "registered!";
	}

	private void OnItemPurchased(DataspinItem item) {
		Debug.Log("OnItemPurchased: "+item.ToString());
	}

	private void OnItemPurchaseVerification(string sku, bool isVerified) {
		Debug.Log("Purchase "+sku+" verified? "+isVerified);
	}

	private void OnItemsRetrieved(List<DataspinItem> dataspinItemsList) {
		Debug.Log("OnItemsRetrieved: "+dataspinItemsList.Count);
		logText.text = "";
		for(int i = 0; i < dataspinItemsList.Count; i++) {
			logText.text += dataspinItemsList[i].ToString() + "\n";
			comboItemsBox.AddItem(dataspinItemsList[i].FullName);
		}
		comboItemsButton.interactable = true;
	}

	private void OnErrorOccured(DataspinError error) {
		logText.text = error.ToString();
		
		if(error.Request.DataspinMethod != null) {
			switch(error.Request.DataspinMethod) {
				case(DataspinRequestMethod.Dataspin_StartSession):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_EndSession):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_RegisterEvent):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_PurchaseItem):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_RegisterUser):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_RegisterUserDevice):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_RegisterOldSession):
					// Do something
					break;
				case(DataspinRequestMethod.Dataspin_GetItems):
					// Do something
					break;
				default:
				break;
			}
		}
		
		switch(error.ErrorType) {
			case(DataspinError.ErrorTypeEnum.QUANTITY_ERROR):
				logText.text = "Item out of stock, Item: "+error.Request.PostData["item"];
				break;
			case(DataspinError.ErrorTypeEnum.API_KEY_NOT_PROVIDED):
				logText.text = "Api key not provided";
				break;
			case(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED):
				break;
			case(DataspinError.ErrorTypeEnum.INTERNAL_PLUGIN_ERROR):
				break;
			case(DataspinError.ErrorTypeEnum.INTERNET_NOTREACHABLE):
				break;
			case(DataspinError.ErrorTypeEnum.UNRECOGNIZED_PLATFORM):
				break;
			case(DataspinError.ErrorTypeEnum.USER_NOT_REGISTERED):
				break;
			case(DataspinError.ErrorTypeEnum.CORRESPONDING_URL_MISSING): //Impossible for now
				break;
			case(DataspinError.ErrorTypeEnum.JSON_PROCESSING_ERROR):
				break;
			case(DataspinError.ErrorTypeEnum.BACKLOG_CORRUPTED):
				break;
			case(DataspinError.ErrorTypeEnum.BACKLOG_FLUSH_ERROR):
				break;
			case(DataspinError.ErrorTypeEnum.CONNECTION_ERROR):
				break;
			default:
				break;
		}
	}
	#endregion
}
