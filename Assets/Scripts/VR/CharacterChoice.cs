using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterChoice : MonoBehaviour {

	public SelectData.CharacterType Type;


	void OnEnable()
	{
		StartCoroutine (Rotation());
	}

	void OnDisable()
	{
		StopCoroutine (Rotation());

	}

	public IEnumerator Rotation()
	{
		while (true) {
			transform.Rotate (Vector3.up);

			yield return null;
		}
	}

}
