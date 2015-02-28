using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Dataspin; //Include this in order to use Dataspin SDK in your scripts

public class DataspinExample : MonoBehaviour {

	public Text configText;
	public Text statusText;
	public Text uuidText;

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
		startSessionButton.interactable = false;
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

	#region Listeners
	private void OnUserRegistered(string uuid) {
		statusText.text = "User registered, session not started";
		DataspinManager.Instance.RegisterDevice();
		uuidText.text = "UUID: "+uuid;
	}

	private void OnDeviceRegistered() {
		//DataspinManager.Instance.RegisterDevice();
		statusText.text = "User & device registered, session not started";
		startSessionButton.interactable = true;
	}

	private void OnSessionStarted() {
		statusText.text = "Session Started - All OK";
		sessionActions.SetActive(true);
		//DataspinManager.Instance.RegisterDevice();
	}

	private void OnItemsRetrieved() {
		//DataspinManager.Instance.RegisterDevice();
	}

	private void OnCustomEventListRetrieved() {
		//DataspinManager.Instance.RegisterDevice();
	}
	#endregion
}
