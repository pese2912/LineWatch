using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetHP : MonoBehaviour {

	private GameObject Canvas;
	private GameObject Panel;
	private Image m_imgHpbar;
	private Transform camera;
	private int initHp;
	private bool isDie = false;


	// Use this for initialization
	void Start () {
		Canvas = gameObject.transform.FindChild ("Canvas").gameObject;
		Panel = Canvas.transform.FindChild ("Panel").gameObject;
		m_imgHpbar = Panel.transform.FindChild("Image").GetComponent<Image>();	
		initHp = 100;
	}
	
	// Update is called once per frame
	void Update () {
		//if(!isDie)
			//Canvas.transform.LookAt (Camera.main.transform.position);	
	}

	public void targetAche(int hp)
	{

		m_imgHpbar.fillAmount = ((float)hp / (float)initHp);
	}

	public void targetDie()
	{
		isDie = true;
		Canvas.SetActive (false);

	}
	public void targetRespawn(int hp)
	{
		
		Canvas.SetActive (true);
		initHp = 100;
		m_imgHpbar.fillAmount = 1;
		isDie = false;
	}
}
