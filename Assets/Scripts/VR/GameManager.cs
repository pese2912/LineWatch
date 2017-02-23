using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Protocol;


[System.Serializable]
public class SelectData{

	public string Id;
	public int Kind;
	public enum CharacterType{

		RIFLER = 1,
		YUTANER = 2,
		SAMURAI = 3,

	};
	public CharacterType Type;
}
	
[System.Serializable]
public class Weapons{

	public GameObject m_weapon;
	public int m_damage;
	public enum WeaponType{

		RIFLE = 1,
		YUTAN = 2,
		SAMURAI = 3,
		RIFLE_UP = 4,
		YUTAN_UP = 5,
		SAMURAI_UP = 6,

	};
	public WeaponType m_Type;

	public int m_speed;
	public int m_distance;

	public Transform m_firePos;
	public Transform m_fireEffect;
	public Transform m_skillEffect;
	public Transform m_portalEffect;
	public Transform m_ExplosionEffect;
	public AudioSource m_SkillAudio;
}
	
[System.Serializable]
public class UIOjbects{

	public GameObject CANVAS;
	public GameObject PanelHP;
	public GameObject TextName;
	public  GameObject PanelReady;
	public GameObject TextReady;
	public GameObject PanelTime;
	public GameObject TextTime;

	public GameObject TeamRed;
	public GameObject RedResultList;

	public GameObject TeamBlue;
	public GameObject BlueResultList;

	public GameObject PanelMVP;
	public GameObject TextMVP;

	public GameObject PanelExit;
	public GameObject BtnWebCam;
	public GameObject BtnWebExit;
	public GameObject PanelKillList;
	public GameObject LoopImg;
	public GameObject TopCam;
	public GameObject PanelPlayer;


	public GameObject PanelChattingList;
	public GameObject TextChatting;

}
	
public class GameManager : MonoBehaviour, IListener {


	#region
	public static GameManager Instance
	{
		get{ return instance; }
		set{ }
	}

	#endregion

	#region
	private static GameManager instance = null;

	[Header("UI Settings")]
	public UIOjbects Ui;

	[Header("Objects Pool")]
	public List<GameObject> ShotGun_bulletPool = new List<GameObject>();
	public List<GameObject> ShotGun_FlarePool = new List<GameObject> ();
	public List<GameObject> LaserGun_bulletPool = new List<GameObject> ();
	public List<GameObject> LaserGun_FlarePool = new List<GameObject> ();
	public List<GameObject> Yutan_bulletPool = new List<GameObject> ();
	public List<GameObject> Yutan_FlarePool = new List<GameObject> ();
	public List<GameObject> PlasmaGun_bulletPool = new List<GameObject> ();
	public List<GameObject> PlasmaGun_FlarePool = new List<GameObject> ();

	public List<GameObject> CameraPool = new List<GameObject> ();
	public List<GameObject> KillListPool = new List<GameObject> ();

	public int b_maxPool = 20;
	public int s_maxPool = 3;
	public int k_maxPool = 7;

	[Header("Bullet Prefabs")]
	public GameObject shotgun_bullet;
	public GameObject shotgun_flare;
	public GameObject lasergun_bullet;
	public GameObject lasergun_flare;
	public GameObject yutan_bullet;
	public GameObject yutan_flare;
	public GameObject plasma_bullet;
	public GameObject plasma_flare;

	[Header("RemotePlayer Prefabs")]
	public GameObject Red_Rifler;
	public GameObject Blue_Rifler;
	public GameObject Red_Yutaner;
	public GameObject Blue_Yutaner;
	public GameObject Red_Samurai;
	public GameObject Blue_Samurai;
	public GameObject Target;

	[Header("Player Prefabs")]
	public GameObject Player_Rifler;
	public GameObject Player_Yutaner;
	public GameObject Player_Samurai;
	private GameObject m_player;
	public string userName=string.Empty;
	public string userToken= string.Empty;

	[Header("Game Settings")]
	public bool isGame = false;
	private GameObject myEventSystem;
	public bool isWebNetwork;
	public System.Diagnostics.Stopwatch sw;

	public GameObject m_WebCam;
	public int Idx = 0;
	public enum GameState {Idle, Ready, Start, End};
	public GameState m_state = GameState.Idle;


	//float deltaTime;
	#endregion

	#region
	void Awake()
	{
		
		SceneManager.sceneLoaded += OnSceneLoaded;

		Application.targetFrameRate = 50;



		if (instance == null) {
			instance 	= this;
			DontDestroyOnLoad (gameObject);
		} else
			DestroyImmediate (this);
	}

	void Start()
	{
		if (NetworkManager.Instance == null)
			isWebNetwork = true;
		else if (W_NetworkManager.Instance == null)
			isWebNetwork = false;


		sw = new System.Diagnostics.Stopwatch();

		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_DEAD, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAMEMANAGER_CONNECT, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAME_START, this);
		EventManager.Instance.AddListener (EVENT_TYPE.PLAYER_CONNECT, this);
		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_GAME_END, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAME_RESULT, this);
		EventManager.Instance.AddListener (EVENT_TYPE.CHAT_MSG, this);


		#if !UNITY_EDITOR && UNITY_WEBGL

		WebGLInput.captureAllKeyboardInput = false;

		#endif

	}

	void Update()
	{
	//	deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
		if (!isWebNetwork  && SceneManager.GetActiveScene ().name.Equals ("MainScene")) {
			
			if (Input.GetButtonDown ("B")) {
				if (Ui.PanelReady != null && Ui.PanelReady.activeSelf)
					myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Ui.PanelReady.gameObject);
				if (Ui.PanelExit != null && Ui.PanelExit.activeSelf)
					myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Ui.PanelExit.gameObject);
		
			}

			if (Input.GetButtonDown ("R1")) {

				if(Ui.PanelExit != null )
					ExitShow ();				
				
			}
		}
	}

	public void ExitShow()
	{
		

		if (Ui.PanelExit.activeSelf) {
			switch (m_state) {

			case GameState.Ready:
				Ui.PanelHP.SetActive (true);
				Ui.PanelReady.SetActive (true);
				Ui.PanelTime.SetActive (true);
				Ui.PanelExit.SetActive (false);
				Ui.PanelChattingList.SetActive (false);
				break;
			case GameState.Start:
				Ui.PanelHP.SetActive (true);
				Ui.PanelTime.SetActive (true);
				Ui.PanelExit.SetActive (false);
				Ui.PanelChattingList.SetActive (true);

				break;
			case GameState.End:

				Ui.TeamRed.SetActive (true);
				Ui.RedResultList.SetActive (true);
				Ui.TeamBlue.SetActive (true);
				Ui.BlueResultList.SetActive (true);
				Ui.PanelMVP.SetActive (true);
				Ui.PanelExit.SetActive (false);
				Ui.PanelChattingList.SetActive (false);
				break;

			default :
				break;
			};

		
		} else {
			
			switch (m_state) {

			case GameState.Ready:
				Ui.PanelReady.SetActive (false);
				Ui.PanelHP.SetActive (false);
				Ui.PanelTime.SetActive (false);
				Ui.PanelExit.SetActive (true);
				Ui.PanelChattingList.SetActive (false);

				break;

			case GameState.Start:
				Ui.PanelHP.SetActive (false);
				Ui.PanelTime.SetActive (false);
				Ui.PanelExit.SetActive (true);
				Ui.PanelChattingList.SetActive (false);
				break;

			case GameState.End:
				Ui.PanelHP.SetActive (false);
				Ui.PanelTime.SetActive (false);
				Ui.TeamRed.SetActive (false);
				Ui.RedResultList.SetActive (false);
				Ui.TeamBlue.SetActive (false);
				Ui.BlueResultList.SetActive (false);
				Ui.PanelMVP.SetActive (false);
				Ui.PanelExit.SetActive (true);
				Ui.PanelChattingList.SetActive (false);

				break;

			default :
				break;
			};
				
			myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Ui.PanelExit.gameObject);
		}
	}

	public IEnumerator Reset()
	{
		yield return null;
		if (SceneManager.GetActiveScene ().name.Equals ("MainScene")) {

			if (!isWebNetwork) {
				EventManager.Instance.PostNotification (EVENT_TYPE.GAME_END, this, userName);

				yield return new WaitForSeconds (1f);

				SceneManager.sceneLoaded -= OnSceneLoaded;


				GameManager.Instance = null;
				NetworkManager.Instance = null;
				EventManager.Instance = null;

				DestroyImmediate (GameManager.Instance.gameObject);
				DestroyImmediate (NetworkManager.Instance.gameObject);
				DestroyImmediate (EventManager.Instance.gameObject);
				DestroyImmediate (NetworkManager.Instance.t_transport.gameObject);
				DestroyImmediate (NetworkManager.Instance.u_transport.gameObject);

				SceneManager.LoadScene ("LobbyScene");

			} else {
				
				#if !UNITY_EDITOR && UNITY_WEBGL
				Application.ExternalCall( "GameExit", "The game says bye!" );
				#endif
				EventManager.Instance.PostNotification (EVENT_TYPE.GAME_END, this, userName);

				yield return new WaitForSeconds (1f);

				GameManager.Instance = null;
				W_NetworkManager.Instance = null;
				EventManager.Instance = null;

				SceneManager.sceneLoaded -= OnSceneLoaded;

				DestroyImmediate (GameManager.Instance.gameObject);
				DestroyImmediate (W_NetworkManager.Instance.gameObject);
				DestroyImmediate (EventManager.Instance.gameObject);
				SceneManager.LoadScene ("Web_LobbyScene");
			}
		}
	}

	public void JoinGame()
	{
		
		for (int i = 0; i < b_maxPool; i++)
		{
			
			GameObject _shotbullet = Instantiate(shotgun_bullet) as GameObject;
			_shotbullet.name = string.Format ("ShotBullet_{0}", i.ToString ("00"));
			_shotbullet.SetActive(false);
			ShotGun_bulletPool.Add (_shotbullet);

			GameObject _shotFlare = Instantiate(shotgun_flare) as GameObject;
			_shotFlare.name = string.Format ("ShotFlare_{0}", i.ToString ("00"));
			_shotFlare.SetActive(false);
			ShotGun_FlarePool.Add (_shotFlare);

			GameObject _yutanbullet = Instantiate(yutan_bullet) as GameObject;
			_yutanbullet.name = string.Format ("YutanBullet_{0}", i.ToString ("00"));
			_yutanbullet.SetActive(false);
			Yutan_bulletPool.Add (_yutanbullet);

			GameObject _yutanFlare = Instantiate(yutan_flare) as GameObject;
			_yutanFlare.name = string.Format ("YutanFlare_{0}", i.ToString ("00"));
			_yutanFlare.SetActive(false);
			Yutan_FlarePool.Add (_yutanFlare);
		}

		for (int i = 0; i < s_maxPool; i++) {
			
			GameObject _laserbullet = Instantiate(lasergun_bullet) as GameObject;
			_laserbullet.name = string.Format ("LaserBullet_{0}", i.ToString ("00"));
			_laserbullet.SetActive(false);
			LaserGun_bulletPool.Add (_laserbullet);

			GameObject _laserFlare = Instantiate(lasergun_flare) as GameObject;
			_laserFlare.name = string.Format ("LaserFlare_{0}", i.ToString ("00"));
			_laserFlare.SetActive(false);
			LaserGun_FlarePool.Add (_laserFlare);

			GameObject _plasmabullet = Instantiate(plasma_bullet) as GameObject;
			_plasmabullet.name = string.Format ("PlasmaBullet_{0}", i.ToString ("00"));
			_plasmabullet.SetActive(false);
			PlasmaGun_bulletPool.Add (_plasmabullet);

			GameObject _plasmaFlare = Instantiate(plasma_flare) as GameObject;
			_plasmaFlare.name = string.Format ("PlasmaFlare_{0}", i.ToString ("00"));
			_plasmaFlare.SetActive(false);
			PlasmaGun_FlarePool.Add (_plasmaFlare);

		}

		if (isWebNetwork) {
			
			StartCoroutine( GameInit ());
			isGame = true;

		} else {
			GameObject canvas = GameObject.Find ("WebCanvas");
			GameObject TopCam = GameObject.FindGameObjectWithTag ("TOP_CAM");//.SetActive (false);
			if (TopCam != null) {

				TopCam.SetActive (false);
			}
			if(canvas != null)
				canvas.SetActive (false);
		}
	}

	public void UiInit()
	{

		myEventSystem = GameObject.Find ("EventSystem");

		if (!isWebNetwork) {

			Ui.CANVAS = GameObject.FindGameObjectWithTag ("CANVAS");
			Ui.PanelHP = GameObject.FindGameObjectWithTag ("PANEL_HP");
			Ui.TextName = GameObject.FindGameObjectWithTag ("TEXT_NAME");
			Ui.PanelReady = GameObject.FindGameObjectWithTag ("PANEL_READY");

			Ui.PanelTime = GameObject.FindGameObjectWithTag ("PANEL_TIME");
			Ui.TextTime = GameObject.FindGameObjectWithTag ("TEXT_TIME");
			Ui.PanelExit = GameObject.FindGameObjectWithTag ("PANEL_EXIT");

			Ui.PanelChattingList = GameObject.FindGameObjectWithTag ("PANEL_CHATTINGLIST");
			Ui.TextChatting = GameObject.FindGameObjectWithTag ("TEXT_CHATTING");


		} else if (isWebNetwork) {

			Ui.PanelPlayer = GameObject.FindGameObjectWithTag ("PANEL_PLAYER");
			Ui.TopCam = GameObject.FindGameObjectWithTag ("TOP_CAM");//.SetActive (false);
			Ui.PanelKillList = GameObject.FindGameObjectWithTag ("PANEL_KILLLIST");
			Ui.BtnWebCam = GameObject.FindGameObjectWithTag ("BTN_WEBCAM");
			Ui.BtnWebExit = GameObject.FindGameObjectWithTag ("WEB_BTN_EXIT");
		}
		Ui.TeamRed = GameObject.FindGameObjectWithTag ("TEAM_RED");
		Ui.RedResultList = GameObject.FindGameObjectWithTag ("RED_RESULTLIST");

		Ui.TeamBlue = GameObject.FindGameObjectWithTag ("TEAM_BLUE");
		Ui.BlueResultList = GameObject.FindGameObjectWithTag ("BLUE_RESULTLIST");

		Ui.PanelMVP = GameObject.FindGameObjectWithTag ("PANEL_MVP");
		Ui.TextMVP = GameObject.FindGameObjectWithTag ("TEXT_MVP");

	}
		
	public IEnumerator GameInit()
	{
	 
		while (true) {
		 	UiInit ();
			if (!isWebNetwork) {
				if (Ui.CANVAS != null && Ui.PanelHP != null && Ui.TextName != null && Ui.PanelReady != null 
					&& Ui.TeamRed != null && Ui.RedResultList != null && Ui.TeamBlue != null && Ui.PanelChattingList != null &&
					Ui.PanelTime != null && Ui.TextTime != null && Ui.BlueResultList != null 
					&& Ui.PanelExit != null && Ui.TextMVP != null && Ui.PanelMVP != null && Ui.TextChatting != null)
					break;
				
			} else {
				if (Ui.TopCam != null && Ui.TeamRed != null && Ui.RedResultList != null && Ui.TeamBlue != null && Ui.BtnWebCam != null && Ui.BtnWebExit != null &&
					Ui.PanelKillList != null &&	Ui.BlueResultList != null && Ui.TextMVP != null && Ui.PanelMVP != null && Ui.PanelPlayer != null)
					break;
			}
			yield return null;
			
		}

		Ui.TeamRed.SetActive (false);
		Ui.RedResultList.SetActive (false);
		Ui.TeamBlue.SetActive (false);
		Ui.BlueResultList.SetActive (false);
		Ui.PanelMVP.SetActive (false);


		if (!isWebNetwork) {

			Ui.PanelExit.GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
				OnClickExit ();
			});

			Ui.PanelChattingList.SetActive (false);
			Ui.PanelExit.SetActive (false);

			Ui.TextName.GetComponent<Text> ().text = m_player.GetComponent<PlayerMoveCtrl> ().Name;

			Ui.PanelReady.GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
				OnClickReady ();
			});

			myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (Ui.PanelReady.gameObject);
		} else if (isWebNetwork) {

			CameraPool.Add (Ui.TopCam);
			m_WebCam = Ui.TopCam;
			Ui.PanelPlayer.GetComponent<Text> ().text = Ui.TopCam.name;


			Ui.BtnWebCam.GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
				OnClickCamTranslate ();
			});

			Ui.BtnWebExit.GetComponent<UnityEngine.UI.Button> ().onClick.AddListener (delegate {
				OnClickWebExit ();
			});

		}

		m_state = GameState.Ready;

		if (!isWebNetwork) {
			Ui.CANVAS.GetComponent<CanvasGroup> ().alpha = 1;
		} else {

			Transform[] child = Ui.PanelKillList.GetComponentsInChildren<Transform> ();

			for (int i = 0; i < child.Length; i++) {

				if (child [i].name.Contains ("KillItem")) {
					child [i].gameObject.SetActive (false);
					KillListPool.Add (child [i].gameObject);
				}
			}

			GameObject canvas = GameObject.Find ("WebCanvas");	
			canvas.GetComponent<CanvasGroup> ().alpha = 1;

		}

	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {

	
		if(scene.name.Equals("MainScene"))
			JoinGame ();


		#if !UNITY_EDITOR && UNITY_WEBGL
	
		if(scene.name.Equals("Web_LobbyScene"))
			Application.ExternalCall( "GameInit", "The game says hello" );
		#endif

	}

	public void OnClickExit()
	{

		StartCoroutine( Reset ());		
	}	

	public void OnClickWebExit()
	{
		
		StartCoroutine( Reset ());		

	}

	public void OnClickReady()
	{

		EventManager.Instance.PostNotification (EVENT_TYPE.GAME_READY, this , userName);
	}


	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{

		switch (Event_Type) {

		case EVENT_TYPE.GAMEMANAGER_CONNECT:
			SceneManager.LoadScene (1);

			//StartCoroutine (LoadImg());
			break;

		case EVENT_TYPE.GAME_START:

			StartCoroutine (GameStart ());
			break; 

		case EVENT_TYPE.PLAYER_CONNECT:
			PlayerConnect (Param);
	
			break;
		case EVENT_TYPE.NETWORK_GAME_END:
			NetworkGameEnd (Param);
			break;

		case EVENT_TYPE.GAME_RESULT:
			StartCoroutine( "GameResult" ,Param);
			break;
		case EVENT_TYPE.R_PLAYER_DEAD:
			StartCoroutine ("ShowKillList", Param);
			break;

		case EVENT_TYPE.CHAT_MSG:
			ChatMsg (Param);
			break;

		};
	}

	public void ChatMsg(object Param)
	{
		if (Param != null) {
			string msg = (string)Param;

			Ui.TextChatting.GetComponent<ChatMessage> ().Add (msg);
		}

	}

	public IEnumerator LoadImg()
	{

		float startTime = 0.5f;
		AsyncOperation async = SceneManager.LoadSceneAsync(1);
		Ui.LoopImg.SetActive (true);
		while (async.isDone == false) {
			

			startTime = 0.5f;
			while (true)
			{
				
				startTime -= Time.deltaTime;
				if(!SceneManager.GetActiveScene().name.Equals("MainScene"))
					Ui.LoopImg.GetComponent<CanvasGroup> ().alpha -= Time.deltaTime;
		

				if (startTime <= 0)
					break;
				yield return null;
			}

			startTime = 0.5f;
			while (true)
			{
				startTime -= Time.deltaTime;
				if(!SceneManager.GetActiveScene().name.Equals("MainScene"))
					Ui.LoopImg.GetComponent<CanvasGroup> ().alpha += Time.deltaTime;

				if (startTime <= 0)
					break;
				yield return null;
			}
			yield return true;
		}
			
	}


	public IEnumerator ShowKillList(object Param)
	{
		yield return null;
		Player player = (Player)Param;

		for (int i = 0; i < KillListPool.Count; i++) {

			if (KillListPool [i] != null && !KillListPool [i].activeSelf) {

				KillListPool [i].GetComponent<KillListItem> ().Killer = player.Killer;
				KillListPool [i].GetComponent<KillListItem> ().deather = player.Name;
				KillListPool [i].SetActive (true);
				break;
			}

		}
	}


	public void PlayerConnect(object Param)
	{
		if (Param != null) {
			Player player = (Player)Param;

			if (!isWebNetwork) {

				if (userName.Equals (player.Name)) {

					StartCoroutine ("PlayerCreate", player);

				} else {
			
					StartCoroutine ("RemotePlayerCreate", player);
				}
			} else
				StartCoroutine ("RemotePlayerCreate", player);
		}
		
	}

	public IEnumerator PlayerCreate(Player player)
	{
		yield return new WaitForSeconds (2f);

		Vector3 RandomDest = new Vector3 (player.Pos.Value.X, player.Pos.Value.Y+1f, player.Pos.Value.Z);
		float r = player.Pos.Value.R;

		if (player.Jobkind == Job.Rifler) {
			m_player = Instantiate (Player_Rifler) as GameObject;

		}

		else if (player.Jobkind == Job.Yutaner) {
			m_player = Instantiate (Player_Yutaner) as GameObject;

		}

		else if (player.Jobkind == Job.Samurai) {
			
			m_player = Instantiate (Player_Samurai) as GameObject;

		}

		if (player.Team == Protocol.Color.Blue) {

			m_player.GetComponent<PlayerMoveCtrl> ().Player (player.Name, PlayerMoveCtrl.TeamState.BLUE, RandomDest, r);

		} else if (player.Team == Protocol.Color.Red) {

			m_player.GetComponent<PlayerMoveCtrl> ().Player (player.Name, PlayerMoveCtrl.TeamState.RED, RandomDest, r);
		} 

		yield return new WaitForSeconds (1f);
		if(!isWebNetwork)
			StartCoroutine(GameInit ());
	}

	public IEnumerator RemotePlayerCreate(Player player)
	{
		yield return new WaitForSeconds(2f);

		Vector3 RandomDest = new Vector3 (player.Pos.Value.X, player.Pos.Value.Y, player.Pos.Value.Z);
		float r = player.Pos.Value.R;

		if (player.Jobkind == Job.Rifler) {
			
			if (player.Team == Protocol.Color.Blue) {
			
				GameObject _BluePlayer = Instantiate (Blue_Rifler) as GameObject;
				_BluePlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.BLUE, RandomDest, r);

				CameraPool.Add (_BluePlayer.GetComponent<RemotePlayerCtrl> ().r_camera);

			} else if (player.Team == Protocol.Color.Red) {

				GameObject _RedPlayer = Instantiate (Red_Rifler) as GameObject;
				_RedPlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.RED, RandomDest, r);
				CameraPool.Add (_RedPlayer.GetComponent<RemotePlayerCtrl> ().r_camera);
		

			} else if (player.Team == Protocol.Color.Gray) {

				GameObject _Target = Instantiate (Target) as GameObject;
				_Target.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.GRAY, RandomDest, r);
				CameraPool.Add (_Target.GetComponent<RemotePlayerCtrl> ().r_camera);
	
			}
		}


		else if (player.Jobkind == Job.Yutaner) {

			if (player.Team == Protocol.Color.Blue) {

				GameObject _BluePlayer = Instantiate (Blue_Yutaner) as GameObject;
				_BluePlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.BLUE, RandomDest, r);

				CameraPool.Add (_BluePlayer.GetComponent<RemotePlayerCtrl> ().r_camera);


			} else if (player.Team == Protocol.Color.Red) {

				GameObject _RedPlayer = Instantiate (Red_Yutaner) as GameObject;
				_RedPlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.RED, RandomDest, r);
				CameraPool.Add (_RedPlayer.GetComponent<RemotePlayerCtrl> ().r_camera);

			} 
		}

		else if (player.Jobkind == Job.Samurai) {

			if (player.Team == Protocol.Color.Blue) {

				GameObject _BluePlayer = Instantiate (Blue_Samurai) as GameObject;
				_BluePlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.BLUE, RandomDest, r);

				CameraPool.Add (_BluePlayer.GetComponent<RemotePlayerCtrl> ().r_camera);


			} else if (player.Team == Protocol.Color.Red) {

				GameObject _RedPlayer = Instantiate (Red_Samurai) as GameObject;
				_RedPlayer.GetComponent<RemotePlayerCtrl> ().RemotePlayer (player.Name, RemotePlayerCtrl.TeamState.RED, RandomDest, r);
				CameraPool.Add (_RedPlayer.GetComponent<RemotePlayerCtrl> ().r_camera);


			} 
		}
	}

	public void OnClickCamTranslate()
	{
		Idx = Idx%CameraPool.Count;

		if (!CameraPool [Idx].activeSelf && CameraPool[Idx] != null) {
			m_WebCam.SetActive (false);
			m_WebCam = CameraPool [Idx];
			Ui.PanelPlayer.GetComponent<Text> ().text = CameraPool [Idx].name;
			m_WebCam.SetActive (true);

		}
		Idx++;
	}
		
	public IEnumerator GameStart()
	{
		yield return null;

		sw.Start ();
		if (!isWebNetwork) {
			Ui.TextReady = Ui.PanelReady.transform.FindChild ("TextReady").gameObject;
			Ui.TextReady.GetComponent<Text> ().text = "START!";

			Ui.PanelReady.GetComponent<Button> ().enabled = false;
		
			StartCoroutine (TimeCheck ());
		
			yield return new WaitForSeconds (1f);
			Ui.PanelReady.SetActive (false);
			Ui.PanelChattingList.SetActive (true);
		}
		isGame = true;
		m_state = GameState.Start;
	}

	public IEnumerator GameResult(object Param)
	{
		yield return null;

		if (Param != null) {

			if(!isWebNetwork)
				Ui.TextReady = Ui.PanelReady.transform.FindChild ("TextReady").gameObject;
			
			yield return new WaitForSeconds(2f);

			Result result = (Result)Param;

			if (!isWebNetwork) {
				if ((int)m_player.GetComponent<PlayerMoveCtrl> ().m_team == (int)result.Win)
					Ui.TextReady.GetComponent<Text> ().text = "WIN!";
				else if (result.Win == Protocol.Color.Gray)
					Ui.TextReady.GetComponent<Text> ().text = "DRAW!";
				else
					Ui.TextReady.GetComponent<Text> ().text = "LOSE!";
		
				yield return new WaitForSeconds (2f);
				Ui.PanelReady.SetActive (false);
			}
		
			Ui.TeamRed.SetActive (true);
			Ui.RedResultList.SetActive (true);
			Ui.TeamBlue.SetActive (true);
			Ui.BlueResultList.SetActive (true);
			Ui.PanelMVP.SetActive (true);

			Ui.TextMVP.GetComponent<Text> ().text = result.Mvp;

			for (int i = 0; i < result.RedteamLength; i++) {

				GameObject Item = Ui.RedResultList.transform.FindChild ("ResultItem" + i).gameObject;
				
				ResultData resultData = Item.GetComponent<ResultData> ();

				resultData.name = result.Redteam (i).Value.Name;
				resultData.killCnt = result.Redteam (i).Value.Kill;
				resultData.DeathCnt = result.Redteam (i).Value.Death;
				resultData.DisResultData ();
			}


			for (int i = 0; i < result.BlueteamLength; i++) {

				GameObject Item = Ui.BlueResultList.transform.FindChild ("ResultItem" + i).gameObject;

				ResultData resultData = Item.GetComponent<ResultData> ();

				resultData.name = result.Blueteam (i).Value.Name;
				resultData.killCnt = result.Blueteam (i).Value.Kill;
				resultData.DeathCnt = result.Blueteam (i).Value.Death;
				resultData.DisResultData ();

			}

			m_state = GameState.End;

		}
	}

	public void NetworkGameEnd(object Param)
	{
		sw.Stop ();
		isGame = false;

		if (!isWebNetwork) {
			if (Ui.PanelExit.activeSelf)
				Ui.PanelExit.SetActive (false);
			Ui.PanelTime.SetActive (false);

			Ui.PanelHP.SetActive (false);
			Ui.PanelChattingList.SetActive (false);
			Ui.TextReady = Ui.PanelReady.transform.FindChild ("TextReady").gameObject;
			Ui.TextReady.GetComponent<Text> ().text = "GAME END!";
			Ui.PanelReady.SetActive (true);
		}
	}
		
	public IEnumerator TimeCheck()
	{
		
		string text;
		System.TimeSpan ts;

		while (true) {
			
			ts = sw.Elapsed;

			text = string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

			/*
			float msec = deltaTime * 1000.0f;
			float fps = 1.0f / deltaTime;
			text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
			*/

			Ui.TextTime.GetComponent<Text> ().text = text;

			yield return null;

		}
	}

	public void GameServerConnect (string data) {
		#if !UNITY_EDITOR && UNITY_WEBGL

		string[] result = data.Split('/');

		GameServerInfo info = new GameServerInfo();
		info.game_ip = result[0];
		info.game_port = result[1];
		info.roomid = result[2];

		EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_ROOM_JOIN, this, info); // param roomInfo, id

		#endif
	}

	#endregion
}
