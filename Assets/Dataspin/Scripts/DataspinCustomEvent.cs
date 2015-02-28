using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dataspin {
	[System.Serializable]
	public class DataspinCustomEvent {
		private string name;
		private string id;

		public string Name {
			get {
				return name;
			}
		}

		public string Id {
			get {
				return id;
			}
		}

		public DataspinCustomEvent(Dictionary<string, object> eventDict) {
			try {
				this.id = (string) eventDict["slug"];
				this.name = (string) eventDict["name"];
			}
			catch(System.Exception e) {
				DataspinManager.Instance.AddError(DataspinError.ErrorTypeEnum.JSON_PROCESSING_ERROR, "Failed to create new DataspinCustomEvent. ", e.StackTrace);
			}
		}

		public override string ToString() {
			return "ID: "+id+", Name: "+name;
		}
	}
}
