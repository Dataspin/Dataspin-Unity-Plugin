using UnityEngine;
using MiniJSON;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataspin {
	public class DataspinManager : MonoBehaviour {

		#region Singleton
        /// Ensure that there is no constructor. This exists only for singleton use.
        protected DataspinManager() {}

        private static DataspinManager _instance;
        public static DataspinManager Instance {
        	get {
        		if(_instance == null) {
        			GameObject g = GameObject.Find(prefabName);
        			if(g == null) {
        				g = new GameObject(prefabName);
        				g.AddComponent<DataspinManager>();
        			}
        			_instance = g.GetComponent<DataspinManager>();
        		}
        		return _instance;
        	}
        }

        private void Awake() {
            if(isDataspinEstablished == true) {
                Debug.Log("Dataspin prefab already detected in scene, deleting new instance!");
                Destroy(this.gameObject);
            }
            else isDataspinEstablished = true;

            DontDestroyOnLoad(this.gameObject); //Persist all scene loads and unloads

            dataspinErrors = new List<DataspinError>();

            if(this.gameObject.name != prefabName) this.gameObject.name = prefabName;
            currentConfiguration = getCurrentConfiguration();
            _instance = this;
        }
        #endregion Singleton


        #region Properties & Variables
        public const string version = "0.1";
        public const string prefabName = "DataspinManager";
        public const string logTag = "[Dataspin]";
        public Configurations configurations;
        public List<DataspinError> dataspinErrors;
        private Configuration currentConfiguration;

        public Configuration CurrentConfiguration {
            get {
                return currentConfiguration;
            }
        }
        #endregion

        #region Session and Player Specific Variables
        public static bool isDataspinEstablished;
        private string uuid;
        private string device_uuid;

        private bool isUserRegistered;
        private bool isDeviceRegistered;
        private bool isSessionStarted;
        #endregion

        #region Events
        public static event Action<string> OnUserRegistered;
        public static event Action OnDeviceRegistered;
        public static event Action OnSessionStarted;
        public static event Action OnItemsRetrieved;
        public static event Action OnEventRegistered;
        public static event Action OnCustomEventListRetrieved;
        #endregion


        #region Requests

        public void GetAuthToken() {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("secret", CurrentConfiguration.APIKey);

            new DataspinWebRequest(DataspinRequestMethod.Dataspin_GetAuthToken, HttpRequestMethod.HttpMethod_Post, parameters);
        }

        public void RegisterUser(string name = "", string surname = "", string email = "") {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("name", name);
            parameters.Add("surname", surname);
            parameters.Add("email", email);

            new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterUser, HttpRequestMethod.HttpMethod_Post, parameters);
        }

        public void RegisterDevice(string notification_id = "") {
            if(isUserRegistered) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("end_user", uuid);
                parameters.Add("platform", GetCurrentPlatform());
                parameters.Add("device", GetDevice());

                if(notification_id != "") parameters.Add("notification_id", notification_id);

                new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterUserDevice, HttpRequestMethod.HttpMethod_Post, parameters);
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.USER_NOT_REGISTERED, "User is not registered! UUID is missing. ", 
                        null, DataspinRequestMethod.Dataspin_RegisterUserDevice));
            }
        }

        public void StartSession() {
            if(isDeviceRegistered && !isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("end_user_device", device_uuid); //TODO: fix
                parameters.Add("app_version", CurrentConfiguration.AppVersion);

                new DataspinWebRequest(DataspinRequestMethod.Dataspin_StartSession, HttpRequestMethod.HttpMethod_Post, parameters);
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.USER_NOT_REGISTERED, "User device is not registered! Device_UUID is missing. ", 
                        null, DataspinRequestMethod.Dataspin_StartSession));
            }
        }

        public void GetItems() {
            if(isSessionStarted)
                new DataspinWebRequest(DataspinRequestMethod.Dataspin_GetItems, HttpRequestMethod.HttpMethod_Get);
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!", 
                        null, DataspinRequestMethod.Dataspin_GetItems));
            }
        }

        public void GetCustomEvents() {
            if(isSessionStarted)
                new DataspinWebRequest(DataspinRequestMethod.Dataspin_GetCustomEvents, HttpRequestMethod.HttpMethod_Get);
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!", 
                        null, DataspinRequestMethod.Dataspin_GetCustomEvents));
            }
        }
        #endregion

        #region Response Handler

        public void OnRequestSuccessfullyExecuted(DataspinWebRequest request) {
            LogInfo("Processing request "+request.DataspinMethod.ToString() +", Response: "+request.Response+".");
            try {
                Dictionary<string, object> responseDict = Json.Deserialize(request.Response) as Dictionary<string, object>;

                if(responseDict != null) {
                    switch(request.DataspinMethod) {
                        case DataspinRequestMethod.Dataspin_RegisterUser:
                            this.uuid = (string) responseDict["uuid"];
                            isUserRegistered = true;
                            if(OnUserRegistered != null) OnUserRegistered(this.uuid);
                            LogInfo("User Registered! UUID: "+this.uuid);
                            break;

                        case DataspinRequestMethod.Dataspin_RegisterUserDevice:
                            this.device_uuid = (string) responseDict["uuid"];
                            isDeviceRegistered = true;
                            if(OnDeviceRegistered != null) OnDeviceRegistered();
                            LogInfo("Device registered! UUID: "+this.device_uuid);
                            break;

                        case DataspinRequestMethod.Dataspin_StartSession:
                            isSessionStarted = true;
                            if(OnSessionStarted != null) OnSessionStarted();
                            LogInfo("Session started!");
                            break;

                        case DataspinRequestMethod.Dataspin_GetItems:
                            if(OnItemsRetrieved != null) OnItemsRetrieved();
                            LogInfo("Items list retrieved: "+request.Response);
                            break;

                        default:
                            break;
                    }
                }
                else {
                    dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.JSON_PROCESSING_ERROR, "Response dictionary is null!"));
                }
            }
            catch(System.Exception e) {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.JSON_PROCESSING_ERROR, e.Message, e.StackTrace));
            }
        }

        #endregion


        #region Helper Functions
        public void StartChildCoroutine(IEnumerator coroutineMethod) {
            StartCoroutine(coroutineMethod);
        }

        private Dictionary<string, object> GetDevice() {
            Dictionary<string, object> deviceDictionary = new Dictionary<string, object>();
            deviceDictionary.Add("manufacturer", GetDeviceManufacturer());
            deviceDictionary.Add("model", SystemInfo.deviceModel);
            deviceDictionary.Add("screen_width", Screen.width);
            deviceDictionary.Add("screen_height", Screen.height);
            deviceDictionary.Add("dpi", Screen.dpi);

            return deviceDictionary;
        }

        private string GetDeviceManufacturer() {
            #if UNITY_IPHONE
                return "Apple";
            #endif

            try {
                return SystemInfo.deviceModel.Substring(0, SystemInfo.deviceModel.IndexOf(' '));
            }
            catch(Exception e) {
                LogInfo("Couldn't determine device manufacturer, probably space in SystemInfo.deviceModel missing. Message: "+e.Message);
                return "Unknown";
            }
        }

        private int GetCurrentPlatform() {
            #if UNITY_ANDROID   
                return 2;
            #elif UNITY_IOS
                return 1;
            #endif
            return 2; //Default = Android
        }

        public Hashtable GetAuthHeader() {
            Hashtable headers = new Hashtable();
            if(CurrentConfiguration.APIKey != null) {
                headers.Add("Authorization", "Token "+currentConfiguration.APIKey);
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.API_KEY_NOT_PROVIDED, "API Key not provided!" +
                "Please fill it in configuration settings under current platform. Api Key can be obtained from website dataspin.io->Console. "));
                //Debug purpouse
                headers.Add("Authorization", "Token <auth_token_here>");

            }
            return headers;
        }

        public String GetStringAuthHeader() {
            if(CurrentConfiguration.APIKey != null) {
                return "Token "+currentConfiguration.APIKey;
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.API_KEY_NOT_PROVIDED, "API Key not provided!" +
                "Please fill it in configuration settings under current platform. Api Key can be obtained from website dataspin.io->Console. "));

                //Debug purpouse
                return "Token <auth_token_here>";
            }
        }

        public void LogInfo(string msg) {
            if(currentConfiguration.logDebug) Debug.Log(logTag + ": " + msg);
        }

        public void LogError(string msg) {
            if(currentConfiguration.logDebug) Debug.LogError(logTag + ": " + msg);
        }

        private Configuration getCurrentConfiguration() {
            #if UNITY_EDITOR
                return configurations.editor;
            #elif UNITY_ANDROID
                return configurations.android;
            #elif UNITY_IPHONE
                return configurations.iOS;
            #elif UNITY_WEBPLAYER
                return configurations.webplayer;
            #elif UNITY_METRO || UNITY_WP8 || UNITY_WINRT || UNITY_STANDALONE_WIN
                return configurations.WP8;
            #else 
                dataspinErrors.Add(new DataspinError.ErrorTypeEnum.UNRECOGNIZED_PLATFORM, "Current platform not supported! Please send email to rafal@dataspin.io");
                return configurations.editor;
            #endif
        }
        #endregion
	}

	[System.Serializable]
	#region Configuration Collections Class
	public class Configuration {
		protected const string API_VERSION = "v1";                                    // API version to use
        protected const string SANDBOX_BASE_URL = "http://127.0.0.1:8000";        // URL for sandbox configurations to make calls to
        protected const string LIVE_BASE_URL = "http://54.247.78.173:8888";           // URL for live configurations to mkae calls to

        protected const string AUTH_TOKEN = "/api/{0}/auth_token/";
        protected const string PLAYER_REGISTER = "/api/{0}/register_user/";
        protected const string DEVICE_REGISTER = "/api/{0}/register_user_device/";
        protected const string START_SESSION = "/api/{0}/start_session/";
        protected const string REGISTER_EVENT = "/api/{0}/register_event/";
        protected const string GET_EVENTS = "/api/{0}/custom_events/";
        protected const string PURCHASE_ITEM = "/api/{0}/purchase/";

        protected const string GET_ITEMS = "/api/{0}/items/";
        protected const string GET_PLATFORMS = "/api/{0}/platforms/";

        private const bool includeAuthHeader = false;

        public string AppName;
        public string AppVersion;
        public string APIKey; //App Secret
        public bool logDebug;
        public bool sandboxMode;

        public string BaseUrl {
        	get {
        		if(sandboxMode) return SANDBOX_BASE_URL;
        		else return LIVE_BASE_URL;
        	}
        }

        public virtual string GetAuthTokenURL() {
            return BaseUrl + System.String.Format(AUTH_TOKEN, API_VERSION);
        }

        public virtual string GetPlayerRegisterURL() {
        	return BaseUrl + System.String.Format(PLAYER_REGISTER, API_VERSION);
        }

        public virtual string GetDeviceRegisterURL() {
            return BaseUrl + System.String.Format(DEVICE_REGISTER, API_VERSION);
        }

        public virtual string GetStartSessionURL() {
            return BaseUrl + System.String.Format(START_SESSION, API_VERSION);
        }

        public virtual string GetRegisterEventURL() {
            return BaseUrl + System.String.Format(REGISTER_EVENT, API_VERSION);
        }

        public virtual string GetEventsListURL() {
            return BaseUrl + System.String.Format(GET_EVENTS, API_VERSION);
        }

        public virtual string GetPurchaseItemURL() {
            return BaseUrl + System.String.Format(PURCHASE_ITEM, API_VERSION);
        }

        public virtual string GetItemsURL() {
            return BaseUrl + System.String.Format(GET_ITEMS, API_VERSION);
        }

        public string GetMethodCorrespondingURL(DataspinRequestMethod dataspinMethod) {
            switch(dataspinMethod) {
                case DataspinRequestMethod.Dataspin_GetAuthToken:
                    return GetAuthTokenURL();
                case DataspinRequestMethod.Dataspin_RegisterUser:
                    return GetPlayerRegisterURL();
                case DataspinRequestMethod.Dataspin_RegisterUserDevice:
                    return GetDeviceRegisterURL();
                case DataspinRequestMethod.Dataspin_StartSession:
                    return GetStartSessionURL();
                case DataspinRequestMethod.Dataspin_RegisterEvent:
                    return GetRegisterEventURL();
                case DataspinRequestMethod.Dataspin_PurchaseItem:
                    return GetPurchaseItemURL();
                case DataspinRequestMethod.Dataspin_GetItems:
                    return GetItemsURL();
                case DataspinRequestMethod.Dataspin_GetCustomEvents:
                    return GetEventsListURL();
                default:
                    DataspinManager.Instance.dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CORRESPONDING_URL_MISSING, 
                        "Corresponing URL Missing, please contact rafal@dataspin.io", "-", dataspinMethod));
                    return null;
            }
        }

        public override string ToString() {
            return AppName + " " + AppVersion + ", APIKey: "+APIKey+", SandboxMode? "+sandboxMode;
        }
	}

    [System.Serializable]
    public class Configurations
    { 
        public Configuration editor;             // Configuration settings for the Unity editor
        public Configuration webplayer;          // Configuration settings for the webplayer live interface 
        public Configuration iOS;                // Configuration settings for the live iOS interface
        public Configuration android;            // Configuration settings for the live Android interface
        public Configuration WP8;                // Configuration settings for the live WP8 interface
    }
    #endregion Configuration Collections Class


    #region Enums

    public enum HttpRequestMethod {
    	HttpMethod_Get = 0,
    	HttpMethod_Put = 1,
    	HttpMethod_Post = 2,
    	HttpMethod_Delete = -1
	}

	public enum DataspinRequestMethod {
        Unknown = -1234,
        Dataspin_GetAuthToken = -1,
		Dataspin_RegisterUser = 0,
		Dataspin_RegisterUserDevice = 1,
        Dataspin_StartSession = 2,
        Dataspin_RegisterEvent = 3,
        Dataspin_PurchaseItem = 4,
        Dataspin_GetItems = 5,
        Dataspin_GetCustomEvents = 6
	}

	public enum DataspinType {
		Dataspin_Item,
		Dataspin_Coinpack,
		Dataspin_Purchase
	}

	public enum DataspinNotificationDeliveryType {
		tapped,
		received
	}

	#endregion
}