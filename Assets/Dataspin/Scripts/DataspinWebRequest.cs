using UnityEngine;
using System.Collections;

namespace Dataspin {
	public class DataspinWebRequest {
		private string url;
		private Dictionary<string,object> postData;
		private DataspinRequestMethod dataspinMethod;
		private HttpRequestMethod httpMethod;

		private WWW www;
		private WWWForm form;

		public DataspinWebRequest (string url, Dictionary<string,object> postData, DataspinRequestMethod dataspinMethod, HttpRequestMethod httpMethod) {
			this.postData = postData;
			this.dataspinMethod = dataspinMethod;
			this.httpMethod = httpMethod;
			this.url = DataspinManager.Instance.CurrentConfiguration.GetMethodCorrespondingURL(dataspinMethod);

			this.form = new WWWForm();
			foreach(KeyValuePair kvp in postData) {
				form.AddField(kvp.key, kvp.value);
			}

			this.www = new WWW(this.url);


		}
	}
}
