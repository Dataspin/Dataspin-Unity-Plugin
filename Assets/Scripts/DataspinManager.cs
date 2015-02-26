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
        	_instance = this;
        }
        #endregion Singleton


        #region Properties & Variables
        public const string version = "1.3.0b1";
        public const string prefabName = "DataspinManager";
        public Configurations configurations;
        private Configuration currentConfiguration;
        #endregion


	}

	[System.Serializable]
	#region Configuration Collections Class
	public class Configuration {
		protected const string API_VERSION = "v1.0";                                    // API version to use
        protected const string SANDBOX_BASE_URL = "https://sb.bspot.com";               // URL for sandbox configurations to make calls to
        protected const string LIVE_BASE_URL = "https://www.bspot.com";                 // URL for live configurations to mkae calls to

        protected const string PLAYER_REGISTER = "/api/{0}/player";

        public string AppName;
        public string AppVersion;
        public string APIKey;
        public bool sandboxMode;

        public string BaseUrl {
        	get {
        		if(sandboxMode) return SANDBOX_BASE_URL;
        		else return LIVE_BASE_URL;
        	}
        }

        public virtual string GetPlayerRegisterURL() {
        	return BaseUrl + System.String.Format(PLAYER_REGISTER, API_VERSION);
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
}