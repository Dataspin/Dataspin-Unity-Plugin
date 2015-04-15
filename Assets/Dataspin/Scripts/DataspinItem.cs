using UnityEngine;

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using MiniJSON;

namespace Dataspin {

	[System.Serializable]
	public class DataspinItem {
		private int id;
		public string internal_id;
		public string long_name;
		private string price;
		private bool isCoinpack;
		private string parameters;
		private DateTime created;
		private Dictionary<string,object> baseDict;
		private string itemJson;

		public String InternalId {
			get {
				return internal_id;
			}
		}

		public Dictionary<string,object> Dict {
			get {
				return baseDict;
			}
		}

		public String JsonItem {
			get {
				return itemJson;
			}
		}

		public String FullName {
			get {
				return long_name;
			}
		}

		public float Price {
			get {
				float f = -1f;
				float.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out f);
				return f; 
			}
		}

		public bool IsCoinpack {
			get {
				return isCoinpack;
			}
		}

		public string Parameters {
			get { 
				return parameters; 
			}
		}

		public DataspinItem(string itemName) {
			this.internal_id = itemName;
			this.long_name = itemName;
		}

		public DataspinItem(Dictionary<string, object> dict) {
			try {
				this.baseDict = dict;
				this.id = (int)(long) dict["id"];
				this.internal_id = (string) dict["internal_id"];
				this.long_name = (string) dict["long_name"];
				this.price = (string) dict["price"];
				this.isCoinpack = (bool) dict["is_coinpack"];
				this.parameters = (string) dict["parameters"];
				this.itemJson = Json.Serialize(dict);
			}
			catch(Exception e) {
				DataspinManager.Instance.AddError(DataspinError.ErrorTypeEnum.JSON_PROCESSING_ERROR, "Failed to create new DataspinItem. ", e.StackTrace);
			}
		}

		public override string ToString() {
			return "Id: "+id+", Internal Id: "+internal_id+", Name: "+long_name+", Price: "+price+", IsCoinpack? "+isCoinpack;
		}

	}
}


