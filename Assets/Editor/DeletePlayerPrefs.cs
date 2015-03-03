using UnityEngine;
using System.Collections;
using UnityEditor;

public class DeletePlayerPrefs : EditorWindow {

	[MenuItem ("Window/Delete PlayerPrefs")]
	public static void Delete () { 
		PlayerPrefs.DeleteAll();
	}
}
