using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCtrl : MonoBehaviour {

	public float time=0.2f;
	// Use this for initialization
	public AudioSource audioSource;

	void OnEnable () {
		if(audioSource != null)
			audioSource.Play ();
		StartCoroutine ("Disable", time);
	}

	public IEnumerator Disable(float time)
	{
		yield return new WaitForSeconds (time);
		gameObject.SetActive (false);
	}


}
