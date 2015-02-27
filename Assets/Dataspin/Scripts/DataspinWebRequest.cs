using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dataspin {
	public class DataspinWebRequest {
		private string url;
		private Dictionary<string,object> postData;
		private DataspinRequestMethod dataspinMethod;
		private HttpRequestMethod httpMethod;

		private WWW www;
		private WWWForm form;

		public WWW WWW {
			get {
				return www;
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

			if(postData != null && httpMethod == HttpRequestMethod.HttpMethod_Post) {
				this.form = new WWWForm();
				foreach(KeyValuePair<string, object> kvp in this.postData) {
					this.form.AddField(kvp.Key, (string) kvp.Value);
				}
			}

			if(dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken)
				this.www = new WWW(this.url, this.form.data, DataspinManager.Instance.GetAuthHeader());
			else 
				this.www = new WWW(this.url, form);

			DataspinManager.Instance.StartChildCoroutine(ExecuteRequest());
		}

		IEnumerator ExecuteRequest() {
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

		public override string ToString() {
			string extraData = "";
			foreach(KeyValuePair<string, object> kvp in postData) {
				extraData += kvp.Key + ":" + kvp.Value + ", ";
			}
			return "Request Type: "+dataspinMethod.ToString() + ", URL: "+ this.url +", HTTP: "+httpMethod.ToString() + 
			", PostData: "+extraData + ", header: " + 
			((this.dataspinMethod != DataspinRequestMethod.Dataspin_GetAuthToken) ? DataspinManager.Instance.GetAuthHeader().ToString() : "not applicable");
		}
	}
}
