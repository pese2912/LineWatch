using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillListItem : MonoBehaviour {

	public Text KillText;
	public Text DeathText;

	public string Killer;
	public string deather;

	public float time;

	void OnEnable () {
		KillText.text = Killer;
		DeathText.text = deather;
		StartCoroutine ("Disable", time);
	}

	public IEnumerator Disable(float time)
	{
		yield return new WaitForSeconds (time);
		gameObject.SetActive (false);
	}

}
