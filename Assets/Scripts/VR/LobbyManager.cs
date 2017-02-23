using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class UIComponents{

	public Button m_LoginBtn;
	public GameObject m_RoomList;
	public GameObject m_Image;
	public GameObject m_Light;
	public GameObject m_ChangeBtn;
	public GameObject m_SelectBtn;

}

public class LobbyManager : MonoBehaviour, IListener{

	public float m_Time;
	public UIComponents UI;

	public GameObject myEventSystem;
	public GameObject FirstBtn;
	public GameObject[] Chracters;
	public int Idx = 0;

	void Awake()
	{
		
		PlayerPrefs.SetString ("USER_TOKEN", "");	
		GameManager.Instance.userToken = PlayerPrefs.GetString ("USER_TOKEN");	

	}

	// Use this for initialization
	void Start () {


		EventManager.Instance.AddListener (EVENT_TYPE.LOBBY_LOGIN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.LOBBY_ROOMLIST, this);
		StartCoroutine ("LoginAccess",m_Time);	
	}

	void Update()
	{
		if (Input.GetButtonDown ("B")) {
			if(UI.m_LoginBtn.gameObject.activeSelf)
				myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(UI.m_LoginBtn.gameObject);	
			if(FirstBtn != null && FirstBtn.activeSelf)
				myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(FirstBtn);	
			if(UI.m_SelectBtn != null && UI.m_SelectBtn.activeSelf)
				myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(UI.m_SelectBtn);	
			
		}
	}
		
	IEnumerator LoginAccess(float time)
	{
		
		yield return new WaitForSeconds (time);

		if (string.IsNullOrEmpty (GameManager.Instance.userToken)) { // no id
			
			UI.m_LoginBtn.gameObject.SetActive (true);
		
			myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(UI.m_LoginBtn.gameObject);

			// try btn connect

		} else { // id
			//connect
			EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_ROOMLIST, this, GameManager.Instance.userToken); // param id
		}
	}

	public void OnClickLoginBtn()
	{
		//login btn
		UI.m_LoginBtn.gameObject.SetActive (false);
		EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_LOGIN, this); // param ?

	}

	public void OnClickChangeBtn()
	{

		Chracters [Idx].SetActive (false);
		Idx++;
		Idx = Idx % Chracters.Length;
		Chracters [Idx].SetActive (true);

	}

	public void OnClickSelectBtn()
	{
		
		NetworkManager.Instance.m_data.Type = Chracters[Idx].GetComponent<CharacterChoice>().Type;
		EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_ROOM_JOIN, this, NetworkManager.Instance.m_data.Kind); // param roomInfo, id
		Chracters[Idx].SetActive(false);
		UI.m_ChangeBtn.SetActive (false);
		UI.m_SelectBtn.SetActive (false);
	}



	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{
		
		switch (Event_Type) {

		case EVENT_TYPE.LOBBY_LOGIN:
			
			RoomList ();
			//userId = "sangsu";
			//PlayerPrefs.SetString (USER_ID, userId);
			//

			break;
		case EVENT_TYPE.LOBBY_ROOMLIST:
			RoomList ();

			break;

		};
	}

	public void RoomList() //  param roomInfo : roomid, name, connectplayer, maxplayer
	{
		UI.m_LoginBtn.gameObject.SetActive (false);

		UI.m_RoomList.SetActive (true);
		Transform[] Child = UI.m_RoomList.transform.GetComponentsInChildren<Transform> ();
		int j = 0;
		for (int i = 0; i < Child.Length; i++) {

			if (Child [i].name.Contains ("RoomItem")) {
				j++;
				RoomData roomData = Child[i].GetComponent<RoomData> ();
				Child [i].GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
					OnClickRoomItem (roomData.roomKind);});
				
				if (j == 1) {
					myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Child [i].gameObject);
					FirstBtn = Child [i].gameObject;
				}
			}
		}				
	}

	public void OnClickRoomItem(int kind)
	{
		//room join
		UI.m_Image.SetActive(false);
		UI.m_RoomList.SetActive (false);
		UI.m_Light.SetActive (true);
		UI.m_ChangeBtn.SetActive (true);
		UI.m_SelectBtn.SetActive (true);
		Chracters [Idx].SetActive (true);

		myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(UI.m_SelectBtn);	

		NetworkManager.Instance.m_data.Kind = kind;


	}
		


}