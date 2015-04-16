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
		private Dictionary<string,object> postData;
		private DataspinRequestMethod dataspinMethod;
		private HttpRequestMethod httpMethod;
		private WWW www;
		private string responseBody;
		private string responseError;

		//Unique ID used for searching tasks after being executed on native layer
		//Task is invoked in Unity layer ---> Task is sent to Android layer and there executed --> Sent back to Unity, searching for this particular DataspinWebRequest

		private string externalTaskPid;

		public string ExternalTaskPid {
			get { return externalTaskPid; }
		}

		public int TaskPid {
			get { return taskPid; }
		}

		public string Error {
			get { return responseError; }
		}

		public string URL {
			get { return url; }
		}

		public string Response {
			get {
				if(responseBody.Length < 2) return "{}";
				return responseBody;
			}
		}

		public Dictionary<string,object> PostData {
			get { return postData; }
			set {
				UpdateWWW();
				postData = value;
			}
		}

		public DataspinRequestMethod DataspinMethod {
			get { return dataspinMethod; }
		}

		public HttpRequestMethod HttpMethod {
			get { return httpMethod; }
		}

		public string Body {
			get { return responseBody; }
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
				Fire();
			}
		}

		public void Fire() {
			DataspinManager.Instance.StartChildCoroutine(ExecuteRequest());
		}

		public void UpdateWWW() {
			var encoding = new System.Text.UTF8Encoding();

			if(httpMethod == HttpRequestMethod.HttpMethod_Post) {
				if(dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) {
					this.stringPostData = Json.Serialize(this.postData);

					Dictionary<string, string> postHeader = new Dictionary<string, string>();
					postHeader["Content-Type"] = "application/json";
					postHeader["Content-Length"] = stringPostData.Length.ToString();
					postHeader["Authorization"] = DataspinManager.Instance.GetStringAuthHeader();

					#if !UNITY_ANDROID || UNITY_EDITOR
						this.www = new WWW(this.url, encoding.GetBytes(stringPostData), postHeader);
					#endif
				}
				else {
					#if !UNITY_ANDROID || UNITY_EDITOR
						this.www = new WWW(this.url, new WWWForm());
					#endif
				}
			}
			else if(httpMethod == HttpRequestMethod.HttpMethod_Get) {
				Dictionary<string, string> postHeader = new Dictionary<string, string>();
				postHeader["Authorization"] =  DataspinManager.Instance.GetStringAuthHeader();

				int counter = 0;
				foreach(KeyValuePair<string, object> kvp in postData) {
					if(counter == 0) url += "?" + kvp.Key + "=" + WWW.EscapeURL((string)kvp.Value.ToString());
					else url += "&" + kvp.Key + "=" + kvp.Value;
					counter++;
				}

				#if !UNITY_ANDROID || UNITY_EDITOR
					this.www = new WWW(this.url, null, postHeader);
				#endif
			}
		}

		IEnumerator ExecuteRequest() {

			DataspinManager.Instance.LogInfo("Executing connection: "+this.ToString());

			//If session has been just invalidated, wait until starting a new one. 
			if( this.dataspinMethod == DataspinRequestMethod.Dataspin_StartSession || 
				this.dataspinMethod == DataspinRequestMethod.Dataspin_RegisterUser || 
				this.dataspinMethod == DataspinRequestMethod.Dataspin_RegisterUserDevice || 
				this.dataspinMethod == DataspinRequestMethod.Dataspin_RegisterOldSession ) {
				//Okay
			}
			else {
				//Debug.Log("Session has to be verified... " + this.dataspinMethod.ToString());

				if(!DataspinManager.Instance.CheckSessionValidity()) {
					while(!DataspinManager.Instance.isSessionStarted) {
						yield return new WaitForSeconds(0.1f);
					}
				}
			}

			//Re-assign sessionId in case when session has been invalidated and new ID has been assigned
			this.postData["session"] = DataspinManager.Instance.SessionId;

			#if UNITY_ANDROID && !UNITY_EDITOR

				this.externalTaskPid = UnityEngine.Random.Range(0,100000000).ToString() + "-" + UnityEngine.Random.Range(0,100000000).ToString() + "-" + UnityEngine.Random.Range(0,100000000).ToString();
				DataspinManager.Instance.StartExternalTask(this);

			#else

				if(Application.internetReachability != NetworkReachability.NotReachable) {
					yield return this.www;
					#if UNITY_5
						ProcessResponse(this.www.text, this.www.error);
					#else
						if(www.error == null) ProcessResponse(this.www.text, null);
						else ProcessResponse(null, this.www.error);
					#endif
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
			#endif
		}

		public void ProcessResponse(string data, string error) {
			if(error != null) {
				this.responseError = error;
				this.responseBody = data;
				DataspinManager.Instance.ParseError(this);

				if(taskPid != 0) {
					DataspinBacklog.Instance.ReportTaskCompletion(this, false);
				}
				else {
					if(dataspinMethod == DataspinRequestMethod.Dataspin_StartSession) {
						DataspinBacklog.Instance.CreateOfflineSession();
					}
					else if(DataspinBacklog.Instance.ShouldPutMethodOnBacklog(this.dataspinMethod)) {
						Log("Server error - Putting request on tape: "+this.ToString());
						DataspinBacklog.Instance.PutRequestOnBacklog(this);
					}
					//Else method should be not put on backlog
				}
			}
			else {
				this.responseBody = data;
				//DataspinManager.Instance.LogInfo("Request "+dataspinMethod.ToString()+" success! Response: "+this.responseBody);
				DataspinManager.Instance.OnRequestSuccessfullyExecuted(this);

				if(taskPid != 0) DataspinBacklog.Instance.ReportTaskCompletion(this, true); //If its BackLog task
			}
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
