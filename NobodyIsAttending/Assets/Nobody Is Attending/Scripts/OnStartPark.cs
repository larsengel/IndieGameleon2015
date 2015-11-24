using UnityEngine;
using System.Collections;

public class OnStartPark : MonoBehaviour {

	// Use this for initialization
	void Start () {
		this.GetComponent<AC.Interaction> ().Interact ();
        int blind_guy_location = AC.GlobalVariables.GetVariable(1).val;
        if (blind_guy_location != 0) {
            GameObject.Find("BlindGuy").SetActive(false);
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
