using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRaycast : MonoBehaviour {

	public bool isAim = false;
	public string killer = string.Empty;


	public Ray ray;
	public RaycastHit hit;
	public Transform m_firePos;


	void Start()
	{
	//	m_firePos=gameObject.GetComponent<PlayerMoveCtrl>().weapon.m_weapon.gameObject.transform.FindChild ("FirePos").GetComponent<Transform>();
	//	camera = Camera.main.GetComponent<Transform> ();

	}

	/*
	// Update is called once per frame
	void Update () {
		
		ray = new Ray(m_firePos.position, m_firePos.rotation * Vector3.forward*15); //레이
		Debug.DrawRay(m_firePos.position, m_firePos.rotation * Vector3.forward * 15); // 카메라가 보는 광선을 그려줌


		if (Physics.Raycast (ray, out hit, 1 << LayerMask.NameToLayer ("R_PLAYER"))) { // 문 레이어 지정

			if (hit.collider.CompareTag ("R_PLAYER")) {

				if ((int)gameObject.GetComponent<PlayerMoveCtrl> ().m_team != (int)hit.collider.GetComponent<RemotePlayerCtrl> ().m_team) {
					
					killer = hit.collider.name;
					isAim = true;
				} else {
					isAim = false;
					killer = string.Empty;
				}
			} else {
				isAim = false;
				killer = string.Empty;
			}	
		}

		else{
			killer = string.Empty;
			isAim = false;
		}


	}
	*/
}
