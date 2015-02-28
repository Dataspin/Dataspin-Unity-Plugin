using UnityEngine;

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

namespace Dataspin {

	[System.Serializable]
	public class DataspinItem {
		private int id;
		private string internal_id;
		private string long_name;
		private string price;
		private bool isCoinpack;
		private string parameters;
		private DateTime created;

		public String InternalId {
			get {
				return internal_id;
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

		public DataspinItem(Dictionary<string, object> dict) {
			try {
				this.id = (int)(long) dict["id"];
				this.internal_id = (string) dict["internal_id"];
				this.long_name = (string) dict["long_name"];
				this.price = (string) dict["price"];
				this.isCoinpack = (bool) dict["is_coinpack"];
				this.parameters = (string) dict["parameters"];
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


