using UnityEngine;
using MiniJSON;

using System;
using System.Collections;
using System.Collections.Generic;


//////////////////////////////////////////////////////////////////
/// Dataspin SDK for Unity3D (Universal - works with all possible platforms) 
/// Version 0.31
//////////////////////////////////////////////////////////////////

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
            currentConfiguration = getCurrentConfiguration();

            //Android only stuff
            #if UNITY_ANDROID && !UNITY_EDITOR
                helperClass = new AndroidJavaClass("io.dataspin.unityhelpersdk.DataspinUnityHelper");
                helperInstance = helperClass.CallStatic<AndroidJavaObject>("GetInstance");
                helperClass.CallStatic("SetApiKey", currentConfiguration.APIKey);

                // Get UnityPlayer context
                unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                unityInstance = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                unityContext = unityInstance.Call<AndroidJavaObject>("getApplicationContext");

                //Retrieve AdId
                helperInstance.Call("GetAdvertisingInfo", unityContext);
            #endif


            //Singleton Stuff
            if(isDataspinEstablished == true) {
                Debug.Log("Dataspin prefab already detected in scene, deleting new instance!");
                Destroy(this.gameObject);
            }
            else isDataspinEstablished = true;

            DontDestroyOnLoad(this.gameObject); //Persist all scene loads and unloads

            dataspinErrors = new List<DataspinError>();

            if(this.gameObject.name != prefabName) this.gameObject.name = prefabName;
            _instance = this;
        }
        #endregion Singleton


        #region Properties & Variables
        public const string version = "0.31";
        public const string prefabName = "DataspinManager";
        public const string logTag = "[Dataspin]";
        private const string USER_UUID_PREFERENCE_KEY = "dataspin_user_uuid";
        private const string DEVICE_UUID_PREFERENCE_KEY = "dataspin_device_uuid";
        private Configuration currentConfiguration;

        public Configurations configurations;
        public List<DataspinError> dataspinErrors;
        
        public Configuration CurrentConfiguration {
            get {
                if(currentConfiguration == null) currentConfiguration = getCurrentConfiguration();
                return currentConfiguration;
            }
        }
        #endregion

        #region Session and Player Specific Variables
        public static bool isDataspinEstablished;
        private string uuid;
        private string device_uuid;
        private string advertisingId;

        private bool isDeviceRegistered;
        private bool isSessionStarted;
        private bool isUserRegistered;
        private int sessionId;

        public int SessionId {
            get {
                return sessionId;
            }
        }

        public List<DataspinItem> dataspinItems;
        public List<DataspinCustomEvent> dataspinCustomEvents;
        public List<DataspinWebRequest> OnGoingTasks;

        public List<DataspinItem> Items {
            get {
                return dataspinItems;
            }
        }

        public List<DataspinCustomEvent> CustomEvents {
            get {
                return dataspinCustomEvents;
            }
        }
        #endregion

        #if UNITY_ANDROID
        private AndroidJavaClass helperClass;
        private AndroidJavaClass unityClass;
        private AndroidJavaObject unityInstance;
        private AndroidJavaObject helperInstance;
        private AndroidJavaObject unityContext;
        #endif

        #region Events
        public static event Action<string> OnUserRegistered;
        public static event Action OnDeviceRegistered;
        public static event Action OnSessionStarted;
        public static event Action OnSessionEnded;
        public static event Action OnEventRegistered;
        public static event Action<DataspinItem> OnItemPurchased;
        public static event Action<List<DataspinItem>> OnItemsRetrieved;
        public static event Action<List<DataspinCustomEvent>> OnCustomEventsRetrieved;

        public static event Action<DataspinError> OnErrorOccured;
        #endregion


        #region Requests
        public void RegisterUser(string name = "", string surname = "", string email = "", string google_plus_id = "", string facebook_id = "", string gamecenter_id = "" ) {
            if(!PlayerPrefs.HasKey(USER_UUID_PREFERENCE_KEY)) {
                LogInfo("User not registered yet!");
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                if(name != "") parameters.Add("name", name);
                if(surname != "") parameters.Add("surname", surname);
                if(email != "") parameters.Add("email", email);
                if(google_plus_id != "") parameters.Add("google_plus", google_plus_id);
                if(facebook_id != "") parameters.Add("facebook", facebook_id);
                if(gamecenter_id != "") parameters.Add("gamecenter", gamecenter_id);


                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterUser, HttpRequestMethod.HttpMethod_Post, parameters));
            }
            else {
                LogInfo("User already registered, acquiring UUID from local storage.");
                this.uuid = PlayerPrefs.GetString(USER_UUID_PREFERENCE_KEY);
                isUserRegistered = true;

                OnUserRegistered(this.uuid); 
            }
        }

        public void RegisterDevice(string notification_id = "", string ad_id = "") {
            if(!PlayerPrefs.HasKey(DEVICE_UUID_PREFERENCE_KEY)) {
                if(isUserRegistered) {
                    LogInfo("Device not registered yet!");
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters.Add("end_user", this.uuid);
                    parameters.Add("platform", GetCurrentPlatform());
                    parameters.Add("device", GetDevice());
                    parameters.Add("uuid", GetDeviceId());

                    if(ad_id != "") parameters.Add("ads_id", advertisingId);
                    if(notification_id != "") parameters.Add("notification_id", notification_id);

                    CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterUserDevice, HttpRequestMethod.HttpMethod_Post, parameters));
                }
                else {
                    dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.USER_NOT_REGISTERED, "User is not registered! UUID is missing. "));
                }
            }
            else {
                LogInfo("Device already registered, acquiring UUID from local storage.");
                device_uuid = PlayerPrefs.GetString(DEVICE_UUID_PREFERENCE_KEY);
                isDeviceRegistered = true;

                OnDeviceRegistered();
            }
        }

        public void StartSession(string carrier_name = "") {
            if(Application.internetReachability != NetworkReachability.NotReachable) {
                if(isDeviceRegistered && !isSessionStarted) {
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters.Add("end_user_device", device_uuid);
                    parameters.Add("app_version", CurrentConfiguration.AppVersion);
                    parameters.Add("connectivity_type", (int) GetConnectivity());

                    #if UNITY_ANDROID && !UNITY_EDITOR
                        parameters.Add("carrier_name", helperInstance.Call<string>("GetCarrier", unityContext));
                    #else
                        parameters.Add("carrier_name", carrier_name);
                    #endif


                    CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_StartSession, HttpRequestMethod.HttpMethod_Post, parameters));
                }
                else if(isSessionStarted) {
                    LogInfo("Session already in progress! No need to call new one.");
                }
                else {
                    dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.USER_NOT_REGISTERED, "User device is not registered! Device_UUID is missing. "));
                }
            }
            else {
                DataspinBacklog.Instance.CreateOfflineSession();
            }
        }

        //Backlog related task, recreate offline session and send to server
        public void CreateOldSession(int id, int deltaTime, int length) {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("end_user_device", device_uuid);
            parameters.Add("app_version", CurrentConfiguration.AppVersion);
            parameters.Add("carrier_name", "");
            parameters.Add("dt", deltaTime);
            parameters.Add("length", length);

            CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterOldSession, HttpRequestMethod.HttpMethod_Post, parameters, id));
        }

        public void EndSession(string carrier_name = "") {
            if(isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("end_user_device", device_uuid);
                parameters.Add("app_version", CurrentConfiguration.AppVersion);
                parameters.Add("connectivity_type", (int) GetConnectivity());
                
                #if UNITY_ANDROID && !UNITY_EDITOR
                        parameters.Add("carrier_name", helperInstance.Call<string>("GetCarrier", unityContext));
                #else
                    parameters.Add("carrier_name", carrier_name);
                #endif

                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_EndSession, HttpRequestMethod.HttpMethod_Post, parameters));
            }
            else {
                LogInfo("Cannot End Session, there is no session active!");
            }
        }

        public void RegisterCustomEvent(string custom_event, string extraData = null, int forced_sess_id = -1) {
            if(isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("custom_event", custom_event);
                parameters.Add("end_user_device", device_uuid);
                parameters.Add("app_version", CurrentConfiguration.AppVersion);

                if(extraData != null) parameters.Add("data", extraData);

                if(forced_sess_id == -1) parameters.Add("session", SessionId);
                else parameters.Add("session", forced_sess_id);

                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterEvent, HttpRequestMethod.HttpMethod_Post, parameters));
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!"));
            }
        }

        public void PurchaseItem(string internal_id, int amount = 1, int forced_sess_id = -1) {
            if(isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("item", internal_id); //FindItemByName(item_name).InternalId
                parameters.Add("end_user_device", device_uuid);
                parameters.Add("app_version", CurrentConfiguration.AppVersion);
                parameters.Add("amount", amount);

                if(forced_sess_id == -1) parameters.Add("session", SessionId);
                else parameters.Add("session", forced_sess_id);

                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_PurchaseItem, HttpRequestMethod.HttpMethod_Post, parameters));
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!"));
            }
        }

        public void GetItems() {
            if(isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("app_version", CurrentConfiguration.AppVersion);
                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_GetItems, HttpRequestMethod.HttpMethod_Get, parameters));
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!"));
            }
        }

        public void GetCustomEvents() {
            if(isSessionStarted) {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("app_version", CurrentConfiguration.AppVersion);
                CreateTask(new DataspinWebRequest(DataspinRequestMethod.Dataspin_GetCustomEvents, HttpRequestMethod.HttpMethod_Get, parameters));
            }
            else {
                dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.SESSION_NOT_STARTED, "Session not started!"));
            }
        }
        #endregion

        #region Response Handler

        public void OnRequestSuccessfullyExecuted(DataspinWebRequest request) {
            RemoveFromOnGoingTasks(request);
            LogInfo("Processing request "+request.DataspinMethod.ToString() +", Response: "+request.Response+".");
            try {
                Dictionary<string, object> responseDict = Json.Deserialize(request.Response) as Dictionary<string, object>;

                if(responseDict != null) {
                    switch(request.DataspinMethod) {
                        case DataspinRequestMethod.Dataspin_RegisterUser:
                            this.uuid = (string) responseDict["uuid"];
                            PlayerPrefs.SetString(USER_UUID_PREFERENCE_KEY, this.uuid);
                            isUserRegistered = true;
                            if(OnUserRegistered != null) OnUserRegistered(this.uuid);
                            LogInfo("User Registered! UUID: "+this.uuid);
                            break;

                        case DataspinRequestMethod.Dataspin_RegisterUserDevice:
                            PlayerPrefs.SetString(DEVICE_UUID_PREFERENCE_KEY, GetDeviceId());
                            isDeviceRegistered = true;
                            this.device_uuid = GetDeviceId(); 
                            if(OnDeviceRegistered != null) OnDeviceRegistered();
                            LogInfo("Device registered! UUID: "+GetDeviceId());
                            break;

                        case DataspinRequestMethod.Dataspin_StartSession:
                            isSessionStarted = true;
                            sessionId = (int)(long) responseDict["id"];
                            if(OnSessionStarted != null) OnSessionStarted();
                            LogInfo("Session started!");
                            break;

                        case DataspinRequestMethod.Dataspin_EndSession:
                            isSessionStarted = false;
                            if(OnSessionEnded != null) OnSessionEnded();
                            LogInfo("Session ended!");
                            break;

                        case DataspinRequestMethod.Dataspin_RegisterOldSession:
                            isSessionStarted = true;
                            break;

                        case DataspinRequestMethod.Dataspin_PurchaseItem:
                            DataspinItem item = FindItemById((string) request.PostData["item"]);
                            if(item != null) {
                                if(OnItemPurchased != null) OnItemPurchased(item);
                                LogInfo("Item "+ item.FullName +" purchased.");
                            }
                            break;

                        case DataspinRequestMethod.Dataspin_GetItems:
                            dataspinItems = new List<DataspinItem>();
                            List<object> items = (List<object>) responseDict["results"];
                            for(int i = 0; i < items.Count; i++) {
                                Dictionary<string, object> itemDict = (Dictionary<string, object>) items[i];
                                dataspinItems.Add(new DataspinItem(itemDict));
                            }
                            if(OnItemsRetrieved != null) OnItemsRetrieved(dataspinItems);
                            LogInfo("Items list retrieved: "+request.Response);
                            break;

                        case DataspinRequestMethod.Dataspin_GetCustomEvents:
                            dataspinCustomEvents = new List<DataspinCustomEvent>();
                            List<object> events = (List<object>) responseDict["results"];
                            for(int i = 0; i < events.Count; i++) {
                                Dictionary<string, object> eventDict = (Dictionary<string, object>) events[i];
                                dataspinCustomEvents.Add(new DataspinCustomEvent(eventDict));
                            }
                            if(OnCustomEventsRetrieved != null) OnCustomEventsRetrieved(dataspinCustomEvents);
                            LogInfo("Custom events list retrieved: "+request.Response);
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

        public void ExternalTaskCompleted(string json) {
            Dictionary<string, object> dict = (Dictionary <string, object>) Json.Deserialize(json);
            json = json.Replace("\\","");
            json = json.Replace("\\\\","");
            LogInfo("External task data received: "+json);
            LogInfo("Response: "+dict["response"]);
            DataspinWebRequest task = SearchOnGoingTask((string)dict["pid"]);
            task.ProcessResponse((string) dict["response"], dict.ContainsKey("error") ? (string) dict["error"] : null);
        }

        public void OnAdIdReceived(string adId) {
            Debug.Log("Advertising ID: "+adId);
            this.advertisingId = adId;
        }

        #endregion


        #region Helper Functions

        public void StartExternalTask(DataspinWebRequest request) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Dictionary<string, object> dict = new Dictionary<string, object>();
            // dict["url"] = request.URL;
            // dict["ds_method"] = (int) request.DataspinMethod;
            // dict["http_method"] = (int) request.HttpMethod;
            // dict["post_data"] = Json.Serialize(request.PostData);
            // dict["task_pid"] = request.ExternalTaskPid;
            // helperInstance.Call("MakeConnection", Json.Serialize(dict));

            helperInstance.Call("MakeConnection", request.URL, (int) request.DataspinMethod, (int) request.HttpMethod, Json.Serialize(request.PostData), request.ExternalTaskPid);
            #endif
        }

        private DataspinWebRequest SearchOnGoingTask(string externalPid) {
            foreach(DataspinWebRequest req in OnGoingTasks) {
                if(req.ExternalTaskPid == externalPid) return req;
            }
            //Request not found!
            return null;
        }

        private void CreateTask(DataspinWebRequest request) {
            OnGoingTasks.Add(request);
        }

        private void RemoveFromOnGoingTasks(DataspinWebRequest request) {
            OnGoingTasks.Remove(request);
        }

        public void StartChildCoroutine(IEnumerator coroutineMethod) {
            StartCoroutine(coroutineMethod);
        }

        public DataspinItem FindItemByName(string name) {
            for(int i = 0; i < dataspinItems.Count; i++) {
                if(dataspinItems[i].FullName == name) return dataspinItems[i];
            }

            LogInfo("DataspinItem "+name+" not found!");

            return null;
        }

        public DataspinItem FindItemById(string id) {
             for(int i = 0; i < dataspinItems.Count; i++) {
                if(dataspinItems[i].InternalId == id) return dataspinItems[i];
            }

            LogInfo("DataspinItem with id "+id+" not found!");

            return null;
        }

        public DataspinCustomEvent FindEventByName(string name) {
             for(int i = 0; i < dataspinCustomEvents.Count; i++) {
                if(dataspinCustomEvents[i].Name == name) return dataspinCustomEvents[i];
            }

            LogInfo("dataspinEvents with name "+name+" not found!");

            return null;
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
                return 1;
            #elif UNITY_IOS
                return 2;
            #endif
            return 1; //Default = Android
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

        public void AddError(DataspinError.ErrorTypeEnum errorType, string message, string stackTrace = null, DataspinWebRequest request = null) {
            dataspinErrors.Add(new DataspinError(errorType, message, stackTrace, request));
        }

        public void LogInfo(string msg) {
            if(currentConfiguration.logDebug) Debug.Log(logTag + ": " + msg);
        }

        public void LogError(string msg) {
            if(currentConfiguration == null) currentConfiguration = getCurrentConfiguration();
            if(currentConfiguration.logDebug) Debug.LogError(logTag + ": " + msg);
        }

        public void FireErrorEvent(DataspinError err) {
            if(OnErrorOccured != null) OnErrorOccured(err);
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

        public string GetDeviceId() {
            #if UNITY_EDITOR
                return Md5Sum(SystemInfo.deviceUniqueIdentifier);
            #elif UNITY_IPHONE
                return Md5Sum(SystemInfo.deviceUniqueIdentifier);
            #else
                return SystemInfo.deviceUniqueIdentifier;
            #endif  
        }

        public string GetCarrier() {

            return null;
        }

        public  string Md5Sum(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);
         
            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
         
            // Convert the encrypted bytes back to a string (base 16)
            string hashString = "";
         
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }
         
            return hashString.PadLeft(32, '0');
        }

        public Connectivity_Type GetConnectivity() {
            if(Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {
                return Connectivity_Type.Cellular;
            }
            else return Connectivity_Type.Wifi;
        }


        public void ParseError(DataspinWebRequest request) {
            switch(request.DataspinMethod) {
                case DataspinRequestMethod.Dataspin_PurchaseItem:
                    if(request.Error.Contains("410"))
                        #if UNITY_5
                            dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.QUANTITY_ERROR, request.Error + request.Body, null, request));
                        #else
                            dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.QUANTITY_ERROR, request.Error, null, request));
                        #endif
                    else 
                        #if UNITY_5
                            dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CONNECTION_ERROR, request.Error + request.Body, null, request));
                        #else
                            dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CONNECTION_ERROR, request.Error, null, request));
                        #endif
                    break;
                default:
                    #if UNITY_5
                        dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CONNECTION_ERROR, request.Error + request.Body, null, request));
                    #else
                        dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CONNECTION_ERROR, request.Error, null, request));
                    #endif
                    break;
            }
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
        protected const string REGISTER_OLD_SESSION = "/api/{0}/register_old_session/";
        protected const string END_SESSION = "/api/{0}/end_session/";
        protected const string REGISTER_EVENT = "/api/{0}/register_event/";
        protected const string PURCHASE_ITEM = "/api/{0}/purchase/";

        protected const string GET_EVENTS = "/api/{0}/custom_events/";
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

        public virtual string GetRegisterOldSessionURL() {
            return BaseUrl + System.String.Format(REGISTER_OLD_SESSION, API_VERSION);
        }

        public virtual string GetEndSessionURL() {
            return BaseUrl + System.String.Format(END_SESSION, API_VERSION);
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

        private string GetCurrentPlatform() {
            #if UNITY_EDITOR
                return "Editor";    
            #elif UNITY_ANDROID
                return "Android";
            #elif UNITY_IPHONE
                return "iOS";
            #elif UNITY_WEBPLAYER
                return "Webplayer";
            #elif UNITY_METRO || UNITY_WP8 || UNITY_WINRT || UNITY_STANDALONE_WIN
                return "Windows";
            #else 
                return "unknown";
            #endif
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
                case DataspinRequestMethod.Dataspin_EndSession:
                    return GetEndSessionURL();
                case DataspinRequestMethod.Dataspin_RegisterEvent:
                    return GetRegisterEventURL();
                case DataspinRequestMethod.Dataspin_PurchaseItem:
                    return GetPurchaseItemURL();
                case DataspinRequestMethod.Dataspin_GetItems:
                    return GetItemsURL();
                case DataspinRequestMethod.Dataspin_GetCustomEvents:
                    return GetEventsListURL();
                case DataspinRequestMethod.Dataspin_RegisterOldSession:
                    return GetRegisterOldSessionURL();
                default:
                    DataspinManager.Instance.dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CORRESPONDING_URL_MISSING, 
                        "Corresponing URL Missing, please contact rafal@dataspin.io"));
                    return null;
            }
        }

        public override string ToString() {
            return AppName + " " + AppVersion + ", Platform: "+ GetCurrentPlatform() +", APIKey: "+APIKey+", SandboxMode? "+sandboxMode;
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

    public enum Connectivity_Type {
        Offline = 0,
        Wifi = 1,
        Cellular = 2
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
        Dataspin_GetCustomEvents = 6,
        Dataspin_EndSession = 7,
        Dataspin_RegisterOldSession = 666
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