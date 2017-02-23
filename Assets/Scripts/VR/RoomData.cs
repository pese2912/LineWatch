using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomData : MonoBehaviour {

	public string roomName = "";
	public int roomKind = 0;
	public int InnerUser=0;

	public Text textRoomName;
	public Text textConnectInfo;

	public void Show()
	{
		textRoomName.text = roomName;
		textConnectInfo.text = InnerUser.ToString();
	}

}
