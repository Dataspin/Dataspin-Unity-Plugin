using UnityEngine;
using System.Collections;

namespace Dataspin {
	[System.Serializable]
	public class DataspinError {

		public ErrorTypeEnum errorType;
		public string message;
		public string stackTrace;
		public DataspinWebRequest request;

		public enum ErrorTypeEnum {
			UNKNOWN_ERROR = 0,
			JSON_PROCESSING_ERROR = 1,
			CONNECTION_ERROR = 2,
			API_KEY_NOT_PROVIDED = 3,
			UNRECOGNIZED_PLATFORM = 4,
			CORRESPONDING_URL_MISSING = 5,
			USER_NOT_REGISTERED = 6,
			INTERNET_NOTREACHABLE = 7,
			SESSION_NOT_STARTED = 8,
			BACKLOG_CORRUPTED = 9,
			INTERNAL_PLUGIN_ERROR = 10,
			BACKLOG_FLUSH_ERROR = 11,
			QUANTITY_ERROR = 410,
		}

		public ErrorTypeEnum ErrorType {
			get {
				return errorType;
			}
		}

		public string Message {
			get {
				return message;
			}
		}

		public string StackTrace {
			get {
				return stackTrace;
			}
		}

		public DataspinWebRequest Request {
			get {
				return request;
			}
		}

		public DataspinError(ErrorTypeEnum errorType, string message, string stackTrace = null, DataspinWebRequest request = null) {
			this.errorType = errorType;
			this.message = message;
			this.stackTrace = stackTrace;
			this.request = request;

			DataspinManager.Instance.LogError(this.ToString());
			DataspinManager.Instance.FireErrorEvent(this);
		}

		public override string ToString() {
			return "[DataspinError] while executing "+ ((request != null) ? request.DataspinMethod.ToString() : "NO_METHOD") + ", Error type: " + errorType.ToString() + 
			" - " + message + ((stackTrace == null) ? "" : ", Stack Trace: "+stackTrace);
		}
	}
}
