using UnityEngine;
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
            if(this.gameObject.name != prefabName) this.gameObject.name = prefabName;
            currentConfiguration = getCurrentConfiguration();
            _instance = this;
        }
        #endregion Singleton


        #region Properties & Variables
        public const string version = "1.3.0b1";
        public const string prefabName = "DataspinManager";
        public const string logTag = "[Dataspin]";
        public Configurations configurations;
        private Configuration currentConfiguration;

        public Configuration CurrentConfiguration {
            get {
                return currentConfiguration;
            }
        }
        #endregion

        #region Session and Player Specific Variables
        private string authToken;
        private string uuid;
        #endregion


        #region Helper Functions
        public void StartChildCoroutine(IEnumerator coroutineMethod) {
            StartCoroutine(coroutineMethod);
        }

        public string GetAuthHeader() {
            return authToken;
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
                LogError("Unrecognized platform platform!");
                return configurations.editor;
            #endif
        }
        #endregion
	}

	[System.Serializable]
	#region Configuration Collections Class
	public class Configuration {
		protected const string API_VERSION = "v1";                                    // API version to use
        protected const string SANDBOX_BASE_URL = "http://54.247.78.173:8888";               // URL for sandbox configurations to make calls to
        protected const string LIVE_BASE_URL = "http://54.247.78.173:8888";                 // URL for live configurations to mkae calls to

        protected const string AUTH_TOKEN = "/api/{0}/auth_token";
        protected const string PLAYER_REGISTER = "/api/{0}/register_user";
        protected const string DEVICE_REGISTER = "/api/{0}/register_user_device";

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

        public string GetMethodCorrespondingURL(DataspinRequestMethod dataspinMethod) {
            switch(dataspinMethod) {
                case DataspinRequestMethod.Dataspin_GetAuthToken:
                    return GetAuthTokenURL();
                case DataspinRequestMethod.Dataspin_RegisterUser:
                    return GetPlayerRegisterURL();
                case DataspinRequestMethod.Dataspin_RegisterUserDevice:
                    return GetDeviceRegisterURL();
                default:
                    DataspinManager.Instance.LogError("Corresponding URL missing!");
                    return null;
            }
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
        Dataspin_GetAuthToken = -1,
		Dataspin_RegisterUser = 0,
		Dataspin_RegisterUserDevice = 1
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