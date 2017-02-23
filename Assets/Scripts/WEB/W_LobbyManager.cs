using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class W_UIComponents{

	public GameObject m_RoomList;
	public GameObject roomData;
	public Transform content;

}

public class W_LobbyManager : MonoBehaviour, IListener{

	public float m_Time;
	public W_UIComponents UI;

	public GameObject myEventSystem;

	void Awake()
	{

		//PlayerPrefs.SetString ("USER_TOKEN", "");	
		//W_GameManager.Instance.userToken = PlayerPrefs.GetString ("USER_TOKEN");	

	}

	// Use this for initialization
	void Start () {

		EventManager.Instance.AddListener (EVENT_TYPE.LOBBY_LOGIN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.LOBBY_ROOMLIST, this);
		//StartCoroutine ("LoginAccess",m_Time);	
	}



	IEnumerator LoginAccess(float time)
	{

		yield return new WaitForSeconds (time);

		EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_ROOMLIST, this); // param id

	}


	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{

		switch (Event_Type) {

		case EVENT_TYPE.LOBBY_ROOMLIST:
			
			RoomList (Param);
		
			break;

		};
	}

	public void RoomList(object Param) //  param roomInfo : roomid, name, connectplayer, maxplayer
	{
		W_RoomList roomList = (W_RoomList)Param;


		UI.m_RoomList.SetActive (true);

		Transform[] Child = UI.content.GetComponentsInChildren<Transform> ();
		//Transform[] RoomItems= null;
		List<GameObject> RoomItems =new List<GameObject>();
		int cnt = 0;
		for (int i = 0; i < Child.Length; i++) {
			if (Child [i].name.Contains ("RoomItem")) {
				RoomItems.Add(Child [i].gameObject);
				Child [i].gameObject.SetActive (false);
			}
		}
			
		for (int i = 0; i < roomList.list.Count; i++) {

			for(int j = 0; j< RoomItems.Count; j++){
				if (!RoomItems[j].gameObject.activeSelf) {

					RoomData data = RoomItems [j].GetComponent<RoomData> ();

					data.roomKind = roomList.list [i]._id;
					data.roomName = "Room# " + data.roomKind;
					data.InnerUser = roomList.list [i].innerUser;
					data.Show ();
					RoomItems [j].gameObject.SetActive (true);

					RoomItems [j].GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
						OnClickRoomItem (data.roomKind);
					});

					break;
				}
			}

			//GameObject room = Instantiate (UI.roomData) as GameObject;

			//room.transform.parent = UI.content;
			//room.GetComponent<RectTransform> ().localScale = new Vector3 (4f, 4f, 4f);
			//room.GetComponent<RectTransform> ().localRotation = Quaternion.;
			//room.GetComponent<RectTransform> ().anchoredPosition3D = Vector3.zero;
			//room.GetComponent<RectTransform> ().anchorMax = new Vector2 (0.5f, 0.5f);
			//room.GetComponent<RectTransform> ().anchorMin = new Vector2 (0.5f, 0.5f);
			//room.GetComponent<RectTransform> ().pivot = new Vector2 (0.5f, 0.5f);
		}

		/*
		Transform[] Child = UI.m_RoomList.transform.GetComponentsInChildren<Transform> ();
		int j = 0;
	
		for (int i = 0; i < Child.Length; i++) {

			if (Child [i].name.Contains ("RoomItem")) {
				j++;
				RoomData roomData = Child[i].GetComponent<RoomData> ();
				Child [i].GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
					OnClickRoomItem (roomData.roomId);});

				if (j == 1) {
					myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Child [i].gameObject);
				
				}
			}
		}	
		*/
	}

	public void OnClickRoomItem(int kind)
	{
		//room join

		EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_ROOM_JOIN, this, kind); // param roomInfo, id

	}

}