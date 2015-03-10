using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;
using System.Text;
using MiniJSON;

namespace Dataspin {
	[System.Serializable]
	public class DataspinWebRequest {
		private int taskPid;
		private string url;
		private string stringPostData;
		private WebRequest webRequest;
		private Dictionary<string,object> postData;
		private DataspinRequestMethod dataspinMethod;
		private HttpRequestMethod httpMethod;
		private WWW www;

		public int TaskPid {
			get {
				return taskPid;
			}
		}

		public WWW WWW {
			get {
				return www;
			}
		}

		public string URL {
			get {
				return url;
			}
		}

		public string Response {
			get {
				if(www.text.Length < 2)
					return "{}";
				return www.text;
			}
		}

		public Dictionary<string,object> PostData {
			get {
				return postData;
			}
			set {
				UpdateWWW();
				postData = value;
			}
		}

		public DataspinRequestMethod DataspinMethod {
			get {
				return dataspinMethod;
			}
		}

		public HttpRequestMethod HttpMethod {
			get {
				return httpMethod;
			}
		}

		public DataspinWebRequest (DataspinRequestMethod dataspinMethod, HttpRequestMethod httpMethod, Dictionary<string,object> postData = null, int taskPid = 0, string specialUrl = "-") {
			this.postData = postData;
			this.dataspinMethod = dataspinMethod;
			this.httpMethod = httpMethod;
			this.taskPid = taskPid; //If 0 then its not backlog task
			if(specialUrl == "-") this.url = DataspinManager.Instance.CurrentConfiguration.GetMethodCorrespondingURL(dataspinMethod);
			else this.url = specialUrl;

			UpdateWWW();

			if(specialUrl == "-") {
				Log("Special URL not supplied, executing request!");
				Fire();
			}
			else {
				Log("Special URL supplied, request suspended.");
			}
		}

		public void Fire() {
			DataspinManager.Instance.StartChildCoroutine(ExecuteRequest());
		}

		public void UpdateWWW() {
			Log("Updating DataspinWebRequest!");
			var encoding = new System.Text.UTF8Encoding();

			if(httpMethod == HttpRequestMethod.HttpMethod_Post) {
				if(dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) {
					this.stringPostData = Json.Serialize(this.postData);

					Hashtable postHeader = new Hashtable();
					postHeader.Add("Content-Type", "application/json");
					postHeader.Add("Content-Length", stringPostData.Length);

					#if UNITY_ANDROID && !UNITY_EDITOR
						postHeader.TokenAuthorization(DataspinManager.Instance.CurrentConfiguration.APIKey);
					#else
						postHeader.Add("Authorization", DataspinManager.Instance.GetStringAuthHeader());
					#endif

					foreach(DictionaryEntry kvp in postHeader) {
						Debug.Log(kvp.Key + " : " + kvp.Value);
					}

					this.www = new WWW(this.url, encoding.GetBytes(stringPostData), postHeader);
				}
				else 
					this.www = new WWW(this.url, new WWWForm());
			}
			else if(httpMethod == HttpRequestMethod.HttpMethod_Get) {
				Hashtable postHeader = new Hashtable();
				postHeader.Add("Authorization", DataspinManager.Instance.GetStringAuthHeader());

				int counter = 0;
				foreach(KeyValuePair<string, object> kvp in postData) {
					if(counter == 0) url += "?" + kvp.Key + "=" + WWW.EscapeURL((string)kvp.Value.ToString());
					else url += "&" + kvp.Key + "=" + kvp.Value;
					counter++;
				}

				this.www = new WWW(this.url, null, postHeader);
			}
		}

		IEnumerator ExecuteRequest() {
			if(Application.internetReachability != NetworkReachability.NotReachable) {
				DataspinManager.Instance.LogInfo("Executing connection: "+this.ToString());
				yield return this.www;
				if(this.www.error != null) {
					DataspinManager.Instance.ParseError(this);
					if(taskPid != 0) {
						DataspinBacklog.Instance.ReportTaskCompletion(this, false);
					}
					else {
						Log("TaskPid != 0");
						if(dataspinMethod == DataspinRequestMethod.Dataspin_StartSession) {
							DataspinBacklog.Instance.CreateOfflineSession();
						}
						else if(DataspinBacklog.Instance.ShouldPutMethodOnBacklog(this.dataspinMethod)) {
							Log("Server error - Putting request on tape: "+this.ToString());
							DataspinBacklog.Instance.PutRequestOnBacklog(this);
						}
						else {
							Log("Method not elgible for backlog, aborting.");
						}
					} 
				}
				else {
					DataspinManager.Instance.LogInfo("Request "+dataspinMethod.ToString()+" success! Response: "+www.text);
					DataspinManager.Instance.OnRequestSuccessfullyExecuted(this);
					if(taskPid != 0) DataspinBacklog.Instance.ReportTaskCompletion(this, true);
				}
			}
			else {
				DataspinManager.Instance.dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.INTERNET_NOTREACHABLE, 
					"Internet not reachable", "-", this));

				if(taskPid != 0) DataspinBacklog.Instance.ReportTaskCompletion(this, false);
				else {
					if(DataspinBacklog.Instance.ShouldPutMethodOnBacklog(this.dataspinMethod)) {
						Log("Internet unreachable - Putting request on tape: "+this.ToString());
						DataspinBacklog.Instance.PutRequestOnBacklog(this);
					}
				}
			}

			//TODO: Notify backlog if task was executed, if yes, remove fom backlog
		}

		private string HttpMethodToString(HttpRequestMethod httpMethod) {
			switch(httpMethod) {
				case HttpRequestMethod.HttpMethod_Post:
					return "POST";
				case HttpRequestMethod.HttpMethod_Get:
					return "GET";
				default:
					return "POST";
			}
		}

		private void Log(string msg) {
			if(DataspinManager.Instance.CurrentConfiguration.logDebug) Debug.Log("[DataspinWebRequest] "+msg);
		}

		public override string ToString() {
			return "Request Type: "+dataspinMethod.ToString() + ", URL: "+ this.url +", HTTP: "+httpMethod.ToString() + 
			", PostData: "+ this.stringPostData + ", header: " + 
			((this.dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) ? DataspinManager.Instance.GetAuthHeader().ToString() : "not applicable");
		}
	}
}
