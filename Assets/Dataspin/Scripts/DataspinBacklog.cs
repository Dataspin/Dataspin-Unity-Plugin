using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using MiniJSON;

namespace Dataspin {
	public class DataspinBacklog : MonoBehaviour {

		#region Variables
		private static DataspinBacklog _instance;

		private const string logTag = "[DataspinBacklog]";

		public const string SessionsInitialValue = "{\"sessions\":[],\"lastPid\":-1}";
		public const string RequestsInitialValue = "{\"requests\":[]}";

		public const string SessionsPreferenceKey = "DATASPIN_OFFLINE_SESSIONS";
		public const string RequestsPreferenceKey = "DATASPIN_OFFLINE_REQUESTS";
		//Key used in PlayerPrefs encryption, just like preferences key, shouldn't be changed after release 
		public static string ENCRYPTION_KEY = "39de5d3b2a503633"; 

		private List<DataspinRequestMethod> backLogMethods;

		private int pidCounter = 1;
		private float timeUntilFlush = 1.0f;

		private bool isOfflineSession;
		private double offlineSessionStart;
		private int offlineSessionId;

		private Dictionary<string, object> backlogJson;
		private Dictionary<string, object> currentSession;

		private List<Dictionary<string, object>> backlogRequestsList; //List of stored offline sessions
		private List<Dictionary<string, object>> backlogSessionsList; //Lisr of stored offline requests

		public DataspinWebRequest[] tasksArray;
		private int tasksListIterator;

		public enum TaskType {
			Old_Session,
			Request,
			RawRequest
		}
		#endregion

		#region Singleton
		public static DataspinBacklog Instance {
			get {
				if(_instance == null) {
					GameObject g = GameObject.Find(DataspinManager.prefabName);
					if(g != null) return (DataspinBacklog) g.AddComponent<DataspinBacklog>();
					else DataspinManager.Instance.AddError(DataspinError.ErrorTypeEnum.INTERNAL_PLUGIN_ERROR, "DataspinManager prefab is MIA!");

				}
				return _instance;
			}
		}

		private void Awake() {
			backLogMethods = new List<DataspinRequestMethod>();
			backLogMethods.Add(DataspinRequestMethod.Dataspin_PurchaseItem);
			backLogMethods.Add(DataspinRequestMethod.Dataspin_RegisterEvent);

			_instance = this;
			backlogSessionsList = new List<Dictionary<string, object>>();
			backlogRequestsList = new List<Dictionary<string, object>>();

			try {
				backlogJson = (Dictionary<string, object>) Json.Deserialize(GetDecryptedBacklog(SessionsPreferenceKey));
				List<object> objList = (List<object>) backlogJson["sessions"];
				backlogSessionsList = CastObjListOntoDictionaryList(objList); //Cast List<object> onto List<Dictionary<string,object>>

				pidCounter = (int)(long) backlogJson["lastPid"];
				Log("Sessions Backlog parsed! Backlog sessions length: "+backlogSessionsList.Count+", lastPid: "+pidCounter.ToString());

				backlogJson = (Dictionary<string, object>) Json.Deserialize(GetDecryptedBacklog(RequestsPreferenceKey));
				objList = (List<object>) backlogJson["requests"];
				backlogRequestsList = CastObjListOntoDictionaryList(objList);

				Log("Requests Backlog parsed! Backlog sessions length: "+backlogRequestsList.Count);

			}
			catch(Exception e) {
				DataspinManager.Instance.AddError(DataspinError.ErrorTypeEnum.BACKLOG_CORRUPTED, "Unable to parse backlog! Detailed message: "+e.Message, e.StackTrace, null);
			}
		}
		#endregion

		#region Subscription
		private void OnEnable() {
			DataspinManager.OnSessionStarted += OnSessionStarted;
		}

		private void OnDisable() {
			DataspinManager.OnSessionStarted -= OnSessionStarted;
		}
		#endregion


		#region Requests
		private void OnSessionStarted() {
			offlineSessionStart = GetTimestamp(); //Case when session started online but some requests were executed when user was offline
			ReadBacklog();
		}

		private void ReadBacklog() {
			CreateTasksQueue();
		}

		//Basing on JSON, create tasks queue in following order: Session->requests[]->Session->requests-> ... -> raw_requests
		private void CreateTasksQueue() {
			tasksListIterator = 0;
			tasksArray = new DataspinWebRequest[backlogSessionsList.Count + backlogRequestsList.Count];

			for(int i = 0; i < backlogSessionsList.Count; i++) {
				Dictionary<string, object> oldSessionDataDict = new Dictionary<string, object>(); 
				oldSessionDataDict["end_user_device"] = DataspinManager.Instance.Device_UUID;
				oldSessionDataDict["app_version"] = DataspinManager.Instance.CurrentConfiguration.AppVersion;
				oldSessionDataDict["dt"] = (int)(GetTimestamp() - (double) backlogSessionsList[i]["start_timestamp"]);
				oldSessionDataDict["length"] = (double) backlogSessionsList[i]["end_timestamp"] - (double) backlogSessionsList[i]["start_timestamp"];

				try {
					tasksArray[i] = new DataspinWebRequest(DataspinRequestMethod.Dataspin_RegisterOldSession, 
					HttpRequestMethod.HttpMethod_Post, oldSessionDataDict, 
					(int)(long)backlogSessionsList[i]["fake_id"], (string) backlogSessionsList[i]["url"]);
				}
				catch(Exception e) {
					Debug.Log("Error while creating tasksArray: "+e.Message+", Stack: "+e.StackTrace);
				}
			}

			// Session Enqueued. Now execute all requests. If request is executed,
			// remove it from backlogSessionsList[i][requests] list. 
			// If all requests are executed, remove session also

			// After this request is executed, session id is retrieved. 
			// Assign this session id into all subrequests and then

			for(int j = 0; j < backlogRequestsList.Count; j++) {
				tasksArray[j + backlogSessionsList.Count] = new DataspinWebRequest( (DataspinRequestMethod) (int)(long)backlogRequestsList[j]["dataspin_method"],
					(HttpRequestMethod)(int)(long) backlogRequestsList[j]["http_method"], (Dictionary<string, object>) backlogRequestsList[j]["post_data"], 
					(int)(long) backlogRequestsList[j]["task_pid"], (string) backlogRequestsList[j]["url"]);
			}

			Log("TasksArray Length: "+tasksArray.Length);
			ExecuteNextTask();
		}

		private void ExecuteNextTask() {
			// Execute all offline sessions. 
			// Once one session is executed, find all requests with corresponding, 
			// negative ID and replace it with new, obtained from server.

			if(tasksArray.Length > tasksListIterator) {
				Log("Executing next task from queue: "+tasksArray[tasksListIterator].ToString());
				DataspinWebRequest req = tasksArray[tasksListIterator];
				//req.PostData["dt"] = (int)req.PostData["dt"];
				req.Fire();
			}
			else {
				Log("All tasks from queue executed!");
			}
		}

		//Determine whether request should be put on backlog
		public bool ShouldPutMethodOnBacklog(DataspinRequestMethod method) {
			for(int i = 0; i < backLogMethods.Count; i++) {
				if(method == backLogMethods[i]) return true;
			}
			return false;
		}

		//Create json based on DataspinWebRequest and then put into session
		public void PutRequestOnBacklog(DataspinWebRequest request) {
			Log("Putting "+ (pidCounter-1).ToString()+" PID request on backlog: "+request.ToString());
			pidCounter--;

			if(isOfflineSession) {
				request.PostData["dt"] = (int)(GetTimestamp() - offlineSessionStart);
				request.PostData["session"] = offlineSessionId;
			}
			else {
				request.PostData["dt"] = (int)(GetTimestamp() - DataspinManager.Instance.sessionTimestamp);
				request.PostData["session"] = DataspinManager.Instance.SessionId;
			}

			Dictionary<string, object> requestDict = new Dictionary<string, object>();
			requestDict["url"] = request.URL;
			requestDict["dataspin_method"] = (int) request.DataspinMethod;
			requestDict["http_method"] = (int) request.HttpMethod;
			requestDict["post_data"] = request.PostData;
			requestDict["task_pid"] = pidCounter;
			//backlogRequestsList.Add(requestDict);

			backlogRequestsList.Add(requestDict);

			ResetFlushTimer();
		}

		public void CreateOfflineSession() {
			Log("Putting on backlog new offline session!");

			isOfflineSession = true;
			offlineSessionStart = GetTimestamp();

			currentSession = new Dictionary<string, object>();
			currentSession["url"] = DataspinManager.Instance.CurrentConfiguration.GetMethodCorrespondingURL(DataspinRequestMethod.Dataspin_RegisterOldSession);
			offlineSessionId = UnityEngine.Random.Range(-10000000,-1);;
			currentSession["fake_id"] = offlineSessionId; //Assign fake Session ID just for dictinction
			currentSession["start_timestamp"] = offlineSessionStart;
			currentSession["end_timestamp"] = offlineSessionStart + 60;
			backlogSessionsList.Add(currentSession);

			DataspinManager.Instance.isSessionStarted = true;

			ResetFlushTimer();

			StopBacklogRefresh();
			StartCoroutine("UpdateOfflineSessionLength");
		}

		public void ReportTaskCompletion(DataspinWebRequest request, bool succeded) {
			Log("Request "+ request.DataspinMethod.ToString() + ", pid: "+(int)(long)request.TaskPid+", Succeed? "+succeded);
			if(succeded) {
				if(request.DataspinMethod == DataspinRequestMethod.Dataspin_RegisterOldSession) { //If it's session

					for(int i = 0; i < tasksArray.Length; i++) {
						if(tasksArray[i].PostData.ContainsKey("session")) { //Find requests only with post_data["session"] key
							Debug.Log("Entry with session key! "+tasksArray[i].ToString());
							if(tasksArray[i].PostData["session"].ToString() == request.TaskPid.ToString()) {
								tasksArray[i].PostData["session"] = GetIdFromResponse(request.Response);
								tasksArray[i].UpdateWWW();
							}
							else {
								Debug.Log("Session != taskPid: "+request.TaskPid.ToString()+" vs "+tasksArray[i].PostData["session"].ToString());
							}
						}
					}

					for(int i = 0; i < backlogRequestsList.Count; i++) { //Iterate through all requests, if(session.fake_id == backlogRequestsList[i].PostData["session"]) backlogRequestsList[i].PostData["session"] = session.
						Dictionary<string, object> d = (Dictionary<string, object>) backlogRequestsList[i]["post_data"];
						string sessionId = d["session"].ToString();
						if(sessionId == request.TaskPid.ToString()) {
							Debug.Log("Replacing "+request.TaskPid.ToString()+" with "+GetIdFromResponse(request.Response));
							((Dictionary<string, object>) backlogRequestsList[i]["post_data"])["session"] = GetIdFromResponse(request.Response); //Assign just got session id

						}
					}

					//REMOVE THAT SESSION FROM LIST
					for(int i = 0; i < backlogSessionsList.Count; i++) {
						if(request.TaskPid == (int)(long) backlogSessionsList[i]["fake_id"]) {
							Log("Removing session at pos: "+i);
							backlogSessionsList.RemoveAt(i);
						}
					}
				}
				else { //It's request
					Log("Removing request! PID: "+request.TaskPid);
					for(int i = 0; i < backlogRequestsList.Count; i++) {
						if(request.TaskPid == (int)(long) backlogRequestsList[i]["task_pid"]) {
							Log("Removing request at pos: "+i);
							backlogRequestsList.RemoveAt(i);
						}
					}
				}
			}

			tasksListIterator++;

			ExecuteNextTask();
			ResetFlushTimer();

		}
		#endregion

		#region Helpers
		private void Log(string msg) {
			if(DataspinManager.Instance.CurrentConfiguration.logDebug) Debug.Log(logTag + ": " + msg);
		}

		private int GetIdFromResponse(string text) {
			Dictionary<string, object> jsonDict = (Dictionary<string, object>) Json.Deserialize(text);
			return (int)(long) jsonDict["id"];
		}

		private List<Dictionary<string, object>> CastObjListOntoDictionaryList(List<object> objList) {
			List<Dictionary<string, object>> dictList = new List<Dictionary<string, object>>();
			for(int i = 0; i < objList.Count; i++) {
				dictList.Add( (Dictionary<string, object>) objList[i] );
			}
			return dictList;
		}

		private string GetDecryptedBacklog(string preference_key) {
			string backlogString = PlayerPrefs.GetString(preference_key);
			if(backlogString.Length < 2) {
				if(preference_key.Contains("SESSIONS")) {
					Log("Sessions preference was empty! Creating entry: "+SessionsInitialValue);
					PlayerPrefs.SetString(preference_key, Encrypt(SessionsInitialValue));
					return SessionsInitialValue;
				}
				else {
					Log("Requests preference was empty! Creating entry: "+RequestsInitialValue);
					PlayerPrefs.SetString(preference_key, Encrypt(RequestsInitialValue));
					return RequestsInitialValue;
				}
			}
			else {
				backlogString = Decrypt(backlogString);
				Log("Decrypted backlog: "+backlogString);
				return backlogString;
			}
		}

		private void ResetFlushTimer() {
			Log("Resetting flush timer, flush in 1.0 seconds!");
			StopCoroutine("FlushTimer");
			StartCoroutine("FlushTimer");
		}

		IEnumerator FlushTimer() {
			float startTime = Time.realtimeSinceStartup;
			while(startTime + timeUntilFlush > Time.realtimeSinceStartup) {
				yield return new WaitForSeconds(0.25f);
			}
			Flush();
		}

		private void Flush() {
			string data = "";
			Dictionary<string, object> dataDict = new Dictionary<string, object>();

			try {
				//Divide process into two preference keys
				dataDict.Add("sessions", backlogSessionsList);
				dataDict.Add("lastPid", pidCounter);
				data = Json.Serialize(dataDict);
				Log("New sessions backlog data: "+data);
				data = Encrypt(data);
				PlayerPrefs.SetString(SessionsPreferenceKey, data);

				dataDict = new Dictionary<string, object>();
				dataDict.Add("requests",backlogRequestsList);
				data = Json.Serialize(dataDict);
				Log("New backlog requests data: "+data);
				data = Encrypt(data);
				PlayerPrefs.SetString(RequestsPreferenceKey, data);
			}
			catch(Exception e) {
				DataspinManager.Instance.AddError(DataspinError.ErrorTypeEnum.BACKLOG_FLUSH_ERROR, "Failed to create/serialize data! Details: "+e.Message, e.StackTrace, null);
			}
		}

		private Dictionary<string, object> FindSessionByFakeId(int fake_id) {
			for(int i = 0; i < backlogSessionsList.Count; i++) { //Searching
				if((int)(long)backlogSessionsList[i]["fake_id"] == fake_id) { //Session found
					return backlogSessionsList[i];
				}
			}
			return null;
		}

		public void StopBacklogRefresh() {
			StopCoroutine("UpdateOfflineSessionLength");
		}

		IEnumerator UpdateOfflineSessionLength() {
			while(true) {
				for(int i = 0; i < backlogSessionsList.Count; i++) {
					if (offlineSessionId.ToString() == backlogSessionsList[i]["fake_id"].ToString()) {
						backlogSessionsList[i]["end_timestamp"] = (double) backlogSessionsList[i]["end_timestamp"]+ 10;
						Log("Ticking...");
						ResetFlushTimer();
						break;
					}
				}
				yield return new WaitForSeconds(10f);
			}
		}

		private double GetTimestamp() {
			DateTime epochStart = new System.DateTime(1970, 1, 1, 1, 0, 0, System.DateTimeKind.Utc);
 			double timestamp = (System.DateTime.UtcNow - epochStart).TotalSeconds;
 			return timestamp;
		}

		public static string Encrypt (string toEncrypt)
		{
			byte[] keyArray = System.Text.UTF8Encoding.UTF8.GetBytes (ENCRYPTION_KEY);
			byte[] toEncryptArray = System.Text.UTF8Encoding.UTF8.GetBytes (toEncrypt);
			RijndaelManaged rDel = new RijndaelManaged ();
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;

			ICryptoTransform cTransform = rDel.CreateEncryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			return Convert.ToBase64String (resultArray, 0, resultArray.Length);
		}
		 
		public static string Decrypt (string toDecrypt)
		{
			byte[] keyArray = System.Text.UTF8Encoding.UTF8.GetBytes (ENCRYPTION_KEY);
			byte[] toEncryptArray = Convert.FromBase64String (toDecrypt);
			RijndaelManaged rDel = new RijndaelManaged ();
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;

			ICryptoTransform cTransform = rDel.CreateDecryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			return System.Text.UTF8Encoding.UTF8.GetString (resultArray);
		}
		#endregion
	}
}
