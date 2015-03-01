using UnityEngine;
using System.Collections;

namespace Dataspin {
	[System.Serializable]
	public class DataspinError {

		public ErrorTypeEnum errorType;
		public string message;
		public string stackTrace;
		public DataspinRequestMethod requestMethod;

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

		public DataspinError(ErrorTypeEnum errorType, string message, string stackTrace = null, DataspinRequestMethod requestMethod = DataspinRequestMethod.Unknown) {
			this.errorType = errorType;
			this.message = message;
			this.stackTrace = stackTrace;
			this.requestMethod = requestMethod;

			DataspinManager.Instance.LogError(this.ToString());
			DataspinManager.Instance.FireErrorEvent(this);
		}

		public override string ToString() {
			return "[DataspinError] while executing "+requestMethod.ToString() +"Error type: " + errorType.ToString() + 
			" - " + message + ", Stack Trace: "+stackTrace;
		}
	}
}
