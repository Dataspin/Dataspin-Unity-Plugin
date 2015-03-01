using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;
using System.Text;
using MiniJSON;

namespace Dataspin {
	public class DataspinWebRequest {
		private string url;
		private string stringPostData;
		private WebRequest webRequest;
		private Dictionary<string,object> postData;
		private DataspinRequestMethod dataspinMethod;
		private HttpRequestMethod httpMethod;


		private WWW www;

		public WWW WWW {
			get {
				return www;
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
		}

		public DataspinRequestMethod DataspinMethod {
			get {
				return dataspinMethod;
			}
		}

		public DataspinWebRequest (DataspinRequestMethod dataspinMethod, HttpRequestMethod httpMethod, Dictionary<string,object> postData = null) {
			this.postData = postData;
			this.dataspinMethod = dataspinMethod;
			this.httpMethod = httpMethod;
			this.url = DataspinManager.Instance.CurrentConfiguration.GetMethodCorrespondingURL(dataspinMethod);

			var encoding = new System.Text.UTF8Encoding();

			if(httpMethod == HttpRequestMethod.HttpMethod_Post) {
				if(dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) {
					this.stringPostData = Json.Serialize(this.postData);

					Hashtable postHeader = new Hashtable();
					postHeader.Add("Content-Type", "application/json");
					postHeader.Add("Content-Length", stringPostData.Length);
					postHeader.Add("Authorization", DataspinManager.Instance.GetStringAuthHeader());

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

			DataspinManager.Instance.StartChildCoroutine(ExecuteRequest());
		}

		IEnumerator ExecuteRequest() {
			if(Application.internetReachability != NetworkReachability.NotReachable) {
				DataspinManager.Instance.LogInfo("Executing connection: "+this.ToString());
				yield return this.www;
				if(this.www.error != null) {
					DataspinManager.Instance.dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.CONNECTION_ERROR, www.error, null, this.dataspinMethod));
				}
				else {
					DataspinManager.Instance.LogInfo("Request "+dataspinMethod.ToString()+" success! Response: "+www.text);
					DataspinManager.Instance.OnRequestSuccessfullyExecuted(this);
				}
			}
			else {
				DataspinManager.Instance.dataspinErrors.Add(new DataspinError(DataspinError.ErrorTypeEnum.INTERNET_NOTREACHABLE, 
					"Internet not reachable", "-", this.dataspinMethod));
			}
		}

		private string HttpMethodToString(HttpRequestMethod httpMethod) {
			switch(httpMethod) {
				case HttpRequestMethod.HttpMethod_Post:
					return "POST";
				case HttpRequestMethod.HttpMethod_Get:
					return "GET";
				default:
					return "GET";
			}
		}

		public override string ToString() {
			return "Request Type: "+dataspinMethod.ToString() + ", URL: "+ this.url +", HTTP: "+httpMethod.ToString() + 
			", PostData: "+ this.stringPostData + ", header: " + 
			((this.dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) ? DataspinManager.Instance.GetAuthHeader().ToString() : "not applicable");
		}
	}
}
