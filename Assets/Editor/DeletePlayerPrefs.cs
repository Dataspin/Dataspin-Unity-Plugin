using UnityEngine;
using System.Collections;
using UnityEditor;

public class DeletePlayerPrefs : EditorWindow {

	[MenuItem ("Window/Delete PlayerPrefs")]
	public static void Delete () { 
		PlayerPrefs.DeleteAll();
	}

	[MenuItem ("Window/Delete Backlog")]
	public static void DeleteBacklog () { 
		PlayerPrefs.DeleteKey("DATASPIN_BACKLOG");
		PlayerPrefs.DeleteKey("DATASPIN_OFFLINE_SESSIONS");
		PlayerPrefs.DeleteKey("DATASPIN_OFFLINE_REQUESTS");
	}
}
