using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {
	
	GameObject R;


	// Use this for initialization
	void Start () {
		
		R = Instantiate (Resources.Load ("RemotePlayer")) as GameObject;
		R.name = "1RemotePlayer";
		R_PlayerAdapter.getInstance ().Add (R);

	}

	// Update is called once per frame
	void Update () {
		
	}
}
