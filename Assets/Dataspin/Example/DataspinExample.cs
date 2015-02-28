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
	public InputField itemNameField;
	public InputField customEventNameField;
	public InputField customEventData;

	public Button startSessionButton;
	public GameObject sessionActions;

	private void OnEnable() { //Subscribe to desired events
		DataspinManager.OnUserRegistered += OnUserRegistered;
		DataspinManager.OnDeviceRegistered += OnDeviceRegistered;
		DataspinManager.OnSessionStarted += OnSessionStarted;
		DataspinManager.OnItemsRetrieved += OnItemsRetrieved;
		DataspinManager.OnCustomEventListRetrieved += OnCustomEventListRetrieved;
	}

	private void OnDisable() { //Unsubscribe after game quit/scene reload etc.
		DataspinManager.OnUserRegistered -= OnUserRegistered;
		DataspinManager.OnDeviceRegistered -= OnDeviceRegistered;
		DataspinManager.OnSessionStarted -= OnSessionStarted;
		DataspinManager.OnItemsRetrieved -= OnItemsRetrieved;
		DataspinManager.OnCustomEventListRetrieved -= OnCustomEventListRetrieved;
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
		DataspinManager.Instance.PurchaseItem(itemNameField.text);
	}

	public void RegisterCustomEvent() {
		DataspinManager.Instance.RegisterCustomEvent(customEventNameField.text, customEventData.text);
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

	private void OnItemsRetrieved(List<DataspinItem> dataspinItemsList) {
		Debug.Log("OnItemsRetrieved: "+dataspinItemsList.Count);
		logText.text = "";
		for(int i = 0; i < dataspinItemsList.Count; i++) {
			logText.text += dataspinItemsList[i].ToString();
		}
	}

	private void OnCustomEventListRetrieved(List<DataspinCustomEvent> dataspinEventsList) {
		Debug.Log("OnCustomEventListRetrieved: "+dataspinEventsList.Count);
		logText.text = "";
		for(int i = 0; i < dataspinEventsList.Count; i++) {
			logText.text += dataspinEventsList[i].ToString();
		}
	}
	#endregion
}
