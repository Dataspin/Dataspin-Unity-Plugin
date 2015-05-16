using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dataspin {
	public class DataspinMenu : EditorWindow {

		[MenuItem ("Window/Delete All PlayerPrefs")]
		public static void Delete () { 
			PlayerPrefs.DeleteAll();
		}

		[MenuItem ("Window/Dataspin SDK/Add Dataspin Prefab")]
		public static void AddDataspinTracker() {
			if (FindObjectOfType (typeof(DataspinManager)) == null) {
				GameObject go = PrefabUtility.InstantiatePrefab(Resources.LoadAssetAtPath<GameObject>("Assets/Dataspin/DataspinManager.prefab")) as GameObject;
				go.name = "DataspinManager";
				Selection.activeObject = go;
			}
			else {
				Debug.LogWarning ("A Dataspin object already exists in this scene - you should never have more than one per scene!");
				Selection.activeObject = GameObject.Find("DataspinManager");
			}
		}

		[MenuItem ("Window/Dataspin SDK/Delete Backlog")]
		public static void DeleteBacklog () { 
			PlayerPrefs.DeleteKey("DATASPIN_BACKLOG");
			PlayerPrefs.DeleteKey("DATASPIN_OFFLINE_SESSIONS");
			PlayerPrefs.DeleteKey("DATASPIN_OFFLINE_REQUESTS");
		}

		[MenuItem ("Window/Dataspin SDK/Send Logs")]
		public static void SendLogs () { 
			if (Application.isPlaying) 
				DataspinManager.Instance.SendMailWithLogs();
			else {
				Debug.LogWarning ("You cannot send logs while being in edit mode! Please start game, send some requests and then try to send logs.");
			}
		}
	}
}