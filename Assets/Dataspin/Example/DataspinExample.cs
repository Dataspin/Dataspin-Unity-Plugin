using UnityEngine;
using System.Collections;
using Dataspin;

public class DataspinExample : MonoBehaviour {

	void Start () {
		DataspinManager.Instance.RegisterUser();
	}
}
