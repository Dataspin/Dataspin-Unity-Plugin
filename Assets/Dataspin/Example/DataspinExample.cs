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

	public InputField itemAmountField;
	public InputField customEventData;
	public ComboBox comboItemsBox;
	public ComboBox comboEventsBox;

	public Button comboItemsButton;
	public Button comboEventsButton;

	public Button startSessionButton;
	public GameObject sessionActions;

	private void OnEnable() { //Subscribe to desired events
		DataspinManager.OnUserRegistered += OnUserRegistered;
		DataspinManager.OnDeviceRegistered += OnDeviceRegistered;
		DataspinManager.OnSessionStarted += OnSessionStarted;
		DataspinManager.OnEventRegistered += OnEventRegistered;
		DataspinManager.OnItemPurchased += OnItemPurchased;
		DataspinManager.OnItemsRetrieved += OnItemsRetrieved;
		DataspinManager.OnCustomEventsRetrieved += OnCustomEventsRetrieved;
		DataspinManager.OnErrorOccured += OnErrorOccured;
	}

	private void OnDisable() { //Unsubscribe after game quit/scene reload etc.
		DataspinManager.OnUserRegistered -= OnUserRegistered;
		DataspinManager.OnDeviceRegistered -= OnDeviceRegistered;
		DataspinManager.OnSessionStarted -= OnSessionStarted;
		DataspinManager.OnItemsRetrieved -= OnItemsRetrieved;
		DataspinManager.OnEventRegistered -= OnEventRegistered;
		DataspinManager.OnCustomEventsRetrieved -= OnCustomEventsRetrieved;
		DataspinManager.OnErrorOccured -= OnErrorOccured;
	}

	//On Start we Register User
	void Start () {
		sessionActions.SetActive(false);
		statusText.text = "Offline";
		configText.text = "Current config: "+DataspinManager.Instance.CurrentConfiguration.ToString();
		DataspinManager.Instance.RegisterUser();
	}

	public void StartSession() {
		DataspinManager.Instance.StartSession();
	}

	public void GetItems() {
		DataspinManager.Instance.GetItems();
	}

	public void GetCustomEvents() {
		DataspinManager.Instance.GetCustomEvents();
	}

	public void ReloadScene() {
		Application.LoadLevel(Application.loadedLevel);
	}

	public void PurchaseItem() {
		if(itemAmountField.text.Length > 0) {
			int amount = int.Parse(itemAmountField.text);
			DataspinManager.Instance.PurchaseItem(comboItemsBox.selected.textComponent.text, amount);
		}
		else {
			DataspinManager.Instance.PurchaseItem(comboItemsBox.selected.textComponent.text);
		}
	}

	public void RegisterCustomEvent() {
		DataspinCustomEvent ev = DataspinManager.Instance.FindEventByName(comboEventsBox.selected.textComponent.text);
		DataspinManager.Instance.RegisterCustomEvent(ev.Id, customEventData.text);
	}

	#region Listeners
	private void OnUserRegistered(string uuid) {
		statusText.text = "User registered, session not started";
		DataspinManager.Instance.RegisterDevice();
		uuidText.text = "UUID: "+uuid;
	}

	private void OnDeviceRegistered(string uuid) {
		//DataspinManager.Instance.RegisterDevice();
		statusText.text = "User & device registered, session not started";
		startSessionButton.interactable = true;
		deviceUuidText.text = "Device UUID: "+uuid;
	}

	private void OnSessionStarted() {
		statusText.text = "Session Started - All OK";
		sessionActions.SetActive(true);
	}

	private void OnEventRegistered() {
		logText.text = "Event registered!";
	}

	private void OnItemPurchased(DataspinItem item) {
		Debug.Log("OnItemPurchased: "+item.ToString());
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

	private void OnCustomEventsRetrieved(List<DataspinCustomEvent> dataspinEventsList) {
		Debug.Log("OnCustomEventsRetrieved: "+dataspinEventsList.Count);
		logText.text = "";
		for(int i = 0; i < dataspinEventsList.Count; i++) {
			logText.text += dataspinEventsList[i].ToString() + "\n";	
			comboEventsBox.AddItem(dataspinEventsList[i].Name);
		}
		comboEventsButton.interactable = true;
	}

	private void OnErrorOccured(DataspinError error) {
		logText.text = error.ToString();
	}
	#endregion
}
