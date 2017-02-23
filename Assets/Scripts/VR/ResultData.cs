using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultData : MonoBehaviour {


	[HideInInspector]
	public string name = "";
	[HideInInspector]
	public int killCnt = 0;
	[HideInInspector]
	public int DeathCnt = 0;

	public Text textPlayerName;
	public Text textResultInfo;


	public void DisResultData()
	{
		
		textPlayerName.text = name;
		textResultInfo.text ="(" + killCnt.ToString() +
			"/" + DeathCnt.ToString() + ")";		
	}
}
