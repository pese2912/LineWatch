using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using FlatBuffers;
using Protocol;
using System;
using BestHTTP;
using BestHTTP.SocketIO;
using Newtonsoft.Json;

public class NetworkManager : MonoBehaviour, IListener {
	
	#region
	public static NetworkManager Instance {
		get{ return instance; }
		set{ }
	}

	#endregion

	#region
	private static NetworkManager instance = null;
	// 통신 모듈.
	public GameObject	transportTcpPrefab;
	public GameObject	transportUdpPrefab;
	public HTTPRequest request;

	// 통신용 변수
	public TransportTCP		t_transport = null;
	public TransportUDP		u_transport = null;

	private SocketManager manager;
	private Socket socket;

	// 접속할 IP 주소.
	private string		m_IP = "10.113.189.193";
	//private string		m_IP = "10.70.22.70";
	// 접속할 포트 번호.
	private int 	m_port = 3000;

	private const int 	m_mtu = 1400;

	private enum ProtocolHeader {EXCEPTE=0, PLAYER_REQ=1, PLAYER_RES=2, GAME_STATE=3,
		MOVE=4,ROTATE = 5,SHOOT=6, ACHE=7, RECALL=8, DIE=9, RESPAWN=10, RESULT=11 , HEART_BEAT = 12, PLAYER_OUT = 14};


	[SerializeField]
	private ProtocolHeader p_Header;
	public SelectData m_data;


	#endregion


	#region
	void Awake()
	{
		
		if (instance == null) {
			instance = this;
			DontDestroyOnLoad (gameObject);
		} else
			DestroyImmediate (this);

	}

	void Start()
	{
		
		// Transport 클래스의 컴포넌트를 가져온다.

		GameObject obj = GameObject.Instantiate(transportTcpPrefab) as GameObject;
		t_transport = obj.GetComponent<TransportTCP>();

		obj = GameObject.Instantiate(transportUdpPrefab) as GameObject;
		u_transport = obj.GetComponent<TransportUDP>();

		DontDestroyOnLoad (t_transport);
		DontDestroyOnLoad (u_transport);



		EventManager.Instance.AddListener (EVENT_TYPE.PLAYER_MOVE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.PLAYER_ROTATE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.PLAYER_FIRE, this);

		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_LOGIN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_ROOMLIST, this);
		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_ROOM_JOIN, this);

		EventManager.Instance.AddListener (EVENT_TYPE.GAME_READY, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAME_END, this);
	
		//StartCoroutine (CheckTimeout ());
	}

	public IEnumerator CheckTimeout()
	{
		yield return new WaitForSeconds (3f);
		while (true) {

			if (u_transport.IsConnected () == true) {
				FlatBufferBuilder builder = new FlatBufferBuilder (1);

				var offset = builder.CreateString (GameManager.Instance.userName);
				HeartBeat.StartHeartBeat (builder);
				HeartBeat.AddName (builder, offset);
				var endoffset = HeartBeat.EndHeartBeat (builder);
				builder.Finish (endoffset.Value);

				int header = EndianConverter ((int)ProtocolHeader.HEART_BEAT);
				byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
				if (u_transport != null && u_transport.IsConnected()) {
					
					u_transport.Send (newPacket, newPacket.Length);
				}
				yield return new WaitForSeconds (3f);
			}

			yield return null;
		}

		yield return null;
	}
		
	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{

		switch (Event_Type) {
		case EVENT_TYPE.PLAYER_MOVE:	// udp		
			Move (Param);
		
			break;
		case EVENT_TYPE.PLAYER_ROTATE:		//udp
			
			Rotate (Param);
			break;



		case EVENT_TYPE.PLAYER_FIRE:   	//udp
			
			Fire(Param);
			break;


		case EVENT_TYPE.NETWORK_LOGIN: //http
			
			Login();
			break;

		case EVENT_TYPE.NETWORK_ROOMLIST: // http
			RoomList (Param);

			break;

		case EVENT_TYPE.NETWORK_ROOM_JOIN: //http
			NetworkRoomJoin (Param);
			break;

		case EVENT_TYPE.GAME_READY: //tcp
			
			GameReady (Param);

			break;

		case EVENT_TYPE.GAME_END: //tcp

			GameEnd (Param);  
			break;
			
		};
	}
		
	void Update()
	{
			
		if (u_transport.IsConnected() == true && t_transport.IsConnected() == true) {
			
			byte[] t_buffer = new byte[m_mtu];
			byte[] u_buffer = new byte[m_mtu];
			int t_recvSize = t_transport.Receive(ref t_buffer, t_buffer.Length);
			int u_recvSize = u_transport.Receive (ref u_buffer, u_buffer.Length);

			if (t_recvSize > 0) {
				
				int TotalSize = t_recvSize;
				int Idx = 0;
				byte[] temp = new byte[m_mtu];
				Buffer.BlockCopy (t_buffer, 0, temp, 0, t_recvSize);

				while (TotalSize > 0) {
		

					int header = HeaderExtract (temp);
					int length = LengthExtract (temp);

					header = EndianConverter (header);
					length = EndianConverter (length);

					byte[] newPacket = SerializeByte (temp, length);
								
					FlatBuffers.ByteBuffer bb = new FlatBuffers.ByteBuffer (newPacket);
					Player player = Player.GetRootAsPlayer (bb);
			
					print ("tcp : " + header);

					switch (header) {

					case (int)ProtocolHeader.PLAYER_REQ:					
						break;

					case (int)ProtocolHeader.PLAYER_RES: //tcp
					
						EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_CONNECT, this, player);
						break;

					case (int)ProtocolHeader.GAME_STATE://tcp

						if (player.Game == Ingame.Start) {
							
							EventManager.Instance.PostNotification (EVENT_TYPE.GAME_START, this, player);
						} else if (player.Game == Ingame.End) {

							EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_GAME_END, this, player);
						}

						break;
	
					case (int)ProtocolHeader.DIE: //tcp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_DEAD, this, player);
						break;

					case (int)ProtocolHeader.RESPAWN: //tcp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RESPAWN, this, player);
						break;

					case (int)ProtocolHeader.RESULT: //tcp
						Result result = Result.GetRootAsResult (bb);
						EventManager.Instance.PostNotification (EVENT_TYPE.GAME_RESULT, this, result);
						break;


					case (int)ProtocolHeader.ACHE: // udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_WOUND, this, player);
						break;

					case (int)ProtocolHeader.RECALL: // udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RECALL, this, player);
						break;

					case (int)ProtocolHeader.MOVE: //udp

						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_MOVE, this, player);
						break;

					case (int)ProtocolHeader.ROTATE: //udp

						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_ROTATE, this, player);
						break;
					case (int)ProtocolHeader.SHOOT: //udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_FIRE, this, player);
						break;

					case (int)ProtocolHeader.PLAYER_OUT: 

						EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_OUT, this, player);
						break;

					default :
						break;
					}
					;

					TotalSize -= (length+8);
					Idx = (length + 8);
					temp = DivisionPacket (temp, Idx, TotalSize);
				}
			}

			if (u_recvSize > 0) {
				
					int header = HeaderExtract (u_buffer);
					//int length = LengthExtract (u_buffer);
					header = EndianConverter (header);
					//length = EndianConverter (length);
					
					
					byte[] newPacket = SerializeByteUDP (u_buffer, u_buffer.Length-4);


					FlatBuffers.ByteBuffer bb = new FlatBuffers.ByteBuffer (newPacket);
					Player player = Player.GetRootAsPlayer (bb);
					print ("udp : " + header);
					switch (header) {
					
									
					case (int)ProtocolHeader.PLAYER_REQ:					
						break;

					case (int)ProtocolHeader.PLAYER_RES: //tcp

						EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_CONNECT, this, player);
						break;

					case (int)ProtocolHeader.GAME_STATE://tcp

						if (player.Game == Ingame.Start) {

							EventManager.Instance.PostNotification (EVENT_TYPE.GAME_START, this, player);
						} else if (player.Game == Ingame.End) {

							EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_GAME_END, this, player);
						}

						break;


					case (int)ProtocolHeader.DIE: //tcp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_DEAD, this, player);
						break;

					case (int)ProtocolHeader.RESPAWN: //tcp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RESPAWN, this, player);
						break;

					case (int)ProtocolHeader.RESULT: //tcp
						Result result = Result.GetRootAsResult (bb);
						EventManager.Instance.PostNotification (EVENT_TYPE.GAME_RESULT, this, result);
						break;


					case (int)ProtocolHeader.ACHE: // udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_WOUND, this, player);
						break;
					case (int)ProtocolHeader.RECALL: // udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RECALL, this, player);
						break;
					case (int)ProtocolHeader.MOVE: //udp

						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_MOVE, this, player);
						break;

					case (int)ProtocolHeader.ROTATE: //udp

						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_ROTATE, this, player);
						break;
					case (int)ProtocolHeader.SHOOT: //udp
						EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_FIRE, this, player);
						break;

					case (int)ProtocolHeader.PLAYER_OUT: 

						EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_OUT, this, player);
						break;

					default :
						break;
					}
					;

			}
		}
	}

	private int EndianConverter(int param) {
		
		byte[] data = BitConverter.GetBytes(param);
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(data);
		}
		return BitConverter.ToInt32(data, 0);
	}

	public void Login()
	{
		//10.113.189.193:3000 
		request = new HTTPRequest(new Uri("http://"+ m_IP + ":" + m_port+"/auth/register"), HTTPMethods.Post, LoginHandleOnRequestFinishedDelegate);
		request.Send ();
	
	}

	void LoginHandleOnRequestFinishedDelegate (HTTPRequest originalRequest, HTTPResponse response)
	{
		switch (request.State) {

		case HTTPRequestStates.Finished:
			string data = System.Text.Encoding.UTF8.GetString (response.Data);
			PlayerInfo playerInfo = JsonConvert.DeserializeObject<PlayerInfo> (data);

			if (playerInfo.message.Equals ("success")) {
				GameManager.Instance.userName = playerInfo.name;
				GameManager.Instance.userToken = playerInfo.token;

				PlayerPrefs.SetString ("USER_NAME", GameManager.Instance.userName);
				PlayerPrefs.SetString ("USER_TOKEN", GameManager.Instance.userToken);

				EventManager.Instance.PostNotification (EVENT_TYPE.LOBBY_LOGIN, this); // param id, roomlist
			}
			break;

		case HTTPRequestStates.Error:
			Debug.Log ("Error");
			break;
		case HTTPRequestStates.Aborted:
			Debug.Log ("Aborted");
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.Log ("ConnectionTimedOut");
			break;
		case HTTPRequestStates.TimedOut:
			Debug.Log ("TimeOut");
			break;
		}	

	}

	public void RoomList(object Param)
	{
		if (Param != null) {

			string user_token = (string)Param;
			request = new HTTPRequest(new Uri("http://"+ m_IP + ":" + m_port+"/auth/login"), HTTPMethods.Post, RoomHandleOnRequestFinishedDelegate);
			request.AddField ("token", user_token);
			request.Send ();

		}
	}

	void RoomHandleOnRequestFinishedDelegate (HTTPRequest originalRequest, HTTPResponse response)
	{
		switch (request.State) {

		case HTTPRequestStates.Finished:
			string data = System.Text.Encoding.UTF8.GetString (response.Data);
			PlayerInfo playerInfo = JsonConvert.DeserializeObject<PlayerInfo> (data);

			if (playerInfo.message.Equals ("success")) {
				GameManager.Instance.userName = playerInfo.name;
				GameManager.Instance.userToken = playerInfo.token;

				PlayerPrefs.SetString ("USER_NAME", GameManager.Instance.userName);
				PlayerPrefs.SetString ("USER_TOKEN", GameManager.Instance.userToken);
				EventManager.Instance.PostNotification (EVENT_TYPE.LOBBY_ROOMLIST, this); // param id, roomlist

			}

			break;

		case HTTPRequestStates.Error:
			Debug.Log ("Error");
			break;
		case HTTPRequestStates.Aborted:
			Debug.Log ("Aborted");
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.Log ("ConnectionTimedOut");
			break;
		case HTTPRequestStates.TimedOut:
			Debug.Log ("TimeOut");
			break;
		}	

	}

	public void NetworkRoomJoin(object Param)
	{

		if (Param != null) {
			
			m_data.Kind = (int)Param;
			request = new HTTPRequest(new Uri("http://"+ m_IP + ":" + m_port+"/room/join/"+m_data.Kind), HTTPMethods.Get, JoinHandleOnRequestFinishedDelegate);
			request.Send ();
			//print ("room join");
		}
	}

	void JoinHandleOnRequestFinishedDelegate (HTTPRequest originalRequest, HTTPResponse response)
	{
		
		switch (request.State) {

		case HTTPRequestStates.Finished:
			string data = System.Text.Encoding.UTF8.GetString (response.Data);
			GameServerInfo gameServerInfo = JsonConvert.DeserializeObject<GameServerInfo> (data);

			//m_IP = gameServerInfo.game_ip;
			//m_port = int.Parse (gameServerInfo.game_port);

			int port = int.Parse (gameServerInfo.game_port);

			m_data.Id = gameServerInfo.roomid;
			GameServerConnect (gameServerInfo.game_ip, port);
			ChatServerConnect (gameServerInfo.chat_ip, gameServerInfo.chat_port, gameServerInfo.roomid);

			break;

		case HTTPRequestStates.Error:
			Debug.Log ("Error");
			break;
		case HTTPRequestStates.Aborted:
			Debug.Log ("Aborted");
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.Log ("ConnectionTimedOut");
			break;
		case HTTPRequestStates.TimedOut:
			Debug.Log ("TimeOut");
			break;
		}	
	}

	public void ChatServerConnect(string Ip, string port, string roomId)
	{
		manager = new SocketManager (new Uri ("http://" + Ip + ":"+ port + "/socket.io/"));
		socket = manager.Socket;

		manager.Socket.On ("connect", OnConnect);
		manager.Socket.Emit ("GamerIn", roomId);

		manager.Socket.On ("fromMsg", OnMessage);
		manager.Socket.On ("disconnect", OnDisconnect);
	}

	void OnConnect(Socket socket, Packet packet, params object[] args) {
		Debug.Log ("chat server connect");

	}
	void OnDisconnect(Socket socket, Packet packet, params object[] args) {
		Debug.Log ("chat server disconnect");

	}
	void OnMessage(Socket socket, Packet packet, params object[] args) {

		EventManager.Instance.PostNotification (EVENT_TYPE.CHAT_MSG, this, (string)args[0]); 
	}

	public void GameServerConnect(string Ip, int port)
	{
		u_transport.Connect(Ip, port);
		t_transport.Connect (Ip, port);
	
		string name = GameManager.Instance.userName;
		int roomId = int.Parse (m_data.Id);

		FlatBufferBuilder builder = new FlatBufferBuilder (1);

		var offset = builder.CreateString (name);
		Player.StartPlayer (builder);
		Player.AddName (builder, offset);
		Player.AddRoomid (builder, roomId);
		Player.AddRoomkind (builder, (short)m_data.Kind);
		if(m_data.Type == SelectData.CharacterType.RIFLER){
			Player.AddJobkind (builder, Job.Rifler);
		}

		else if(m_data.Type == SelectData.CharacterType.YUTANER){
			Player.AddJobkind (builder, Job.Yutaner);
		}
		else if(m_data.Type == SelectData.CharacterType.SAMURAI){
			Player.AddJobkind (builder, Job.Samurai);
		}
		var endoffset = Player.EndPlayer (builder);
		builder.Finish (endoffset.Value);

		int header = EndianConverter((int)ProtocolHeader.PLAYER_REQ);
		byte[] newPacket = HeaderAdd (builder.SizedByteArray (), header);
		//byte[] newPacket = builder.SizedByteArray();
		if (t_transport != null && t_transport.IsConnected()) {
			
			t_transport.Send (newPacket, newPacket.Length);
		}
		EventManager.Instance.PostNotification (EVENT_TYPE.GAMEMANAGER_CONNECT, this, null); // param playerInfo(id,team,), RemotePlayer(id,team)
		StartCoroutine (CheckTimeout ());
	}
		
	public void GameReady(object Param)
	{
		
		if (Param != null) {
			
			string name = (string)Param;

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (name);
			Player.StartPlayer (builder);
			Player.AddName (builder, offset);
			Player.AddGame (builder, Ingame.Ready);
			var endOffset = Player.EndPlayer (builder);
			builder.Finish (endOffset.Value);

			int header = EndianConverter((int)ProtocolHeader.GAME_STATE);

			byte[] newPacket = HeaderAdd (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();
			if (t_transport != null && t_transport.IsConnected()) {
				
				t_transport.Send (newPacket, newPacket.Length);
			}
		
		}
	}

	public void GameEnd(object Param)
	{
		
		FlatBufferBuilder builder = new FlatBufferBuilder (1);

		var offset = builder.CreateString (GameManager.Instance.userName);
		Player.StartPlayer (builder);
		Player.AddName (builder, offset);
		Player.AddGame (builder, Ingame.Out);
		var endOffset = Player.EndPlayer (builder);
		builder.Finish (endOffset.Value);

		int header = EndianConverter((int)ProtocolHeader.GAME_STATE);

		byte[] newPacket = HeaderAdd (builder.SizedByteArray (), header);
		//byte[] newPacket = builder.SizedByteArray();

		if (t_transport != null && t_transport.IsConnected()) {
			t_transport.Send (newPacket, newPacket.Length);
			t_transport.Disconnect ();
		}
		if (u_transport != null && u_transport.IsConnected()) {
			u_transport.Disconnect ();
		}
		if (manager != null && manager.Socket.IsOpen) {
			manager.Socket.Disconnect ();
		}
	}
		
	public void Move(object Param)
	{
		
		if (Param != null) {
			
			GameObject player = (GameObject)Param;
			PlayerMoveCtrl playerCtrl = player.GetComponent<PlayerMoveCtrl> ();

			Vector3 tr = playerCtrl.transform.position;

			float horizontal = playerCtrl.horizontal;
			float vertical = playerCtrl.vertical;
			float r = player.transform.localEulerAngles.y;

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (playerCtrl.Name);
			var pos = Vec5.CreateVec5 (builder, tr.x, tr.y, tr.z, r, horizontal, vertical);
				

			Player.StartPlayer (builder);
			Player.AddPos (builder, pos);
			Player.AddName (builder, offset);
																							
			var endOffset = Player.EndPlayer (builder);

			builder.Finish (endOffset.Value);
			
			int header = EndianConverter((int)ProtocolHeader.MOVE);
			byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();
			if (u_transport != null && u_transport.IsConnected()) {
				
				u_transport.Send (newPacket, newPacket.Length);
			}

		}
	}

	public void Rotate(object Param)
	{

		if (Param != null) {

			GameObject player = (GameObject)Param;
			PlayerMoveCtrl playerCtrl = player.GetComponent<PlayerMoveCtrl> ();
		
			Vector3 tr = playerCtrl.transform.position;

			float r = player.transform.localEulerAngles.y;
			float rotate = playerCtrl.rotate;

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (playerCtrl.Name);
			var rot = Vec2.CreateVec2 (builder, tr.x, tr.z,r,rotate);
	
			Player.StartPlayer (builder);
			Player.AddRot (builder, rot);		
			Player.AddName (builder, offset);

			var endOffset = Player.EndPlayer (builder);

			builder.Finish (endOffset.Value);

			int header = EndianConverter((int)ProtocolHeader.ROTATE);
			byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();
			if (u_transport != null && u_transport.IsConnected()) {
				
				u_transport.Send (newPacket, newPacket.Length);
			}

		}
	}

	public void Fire(object Param)
	{
		
		if (Param != null) {
			
			GameObject player = (GameObject)Param;
			PlayerMoveCtrl playerCtrl = player.GetComponent<PlayerMoveCtrl> ();

			Vector3 tr = new Vector3 (playerCtrl.weapon.m_firePos.transform.position.x,
				playerCtrl.weapon.m_firePos.transform.position.z, playerCtrl.transform.localEulerAngles.y);
			bool isShoot = player.GetComponent<PlayerRaycast> ().isAim;
			string killer = player.GetComponent<PlayerRaycast> ().killer;
			float time = GameManager.Instance.sw.ElapsedMilliseconds / 1000.0f;


			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			int damage = playerCtrl.weapon.m_damage;
			int velocity = playerCtrl.weapon.m_speed;
			int distance = playerCtrl.weapon.m_distance;

			var weapon =  Weapon.CreateWeapon (builder, WepId.rifle, (short)damage, velocity, distance);
			if (playerCtrl.weapon.m_Type == Weapons.WeaponType.RIFLE) {
				weapon = Weapon.CreateWeapon (builder, WepId.rifle, (short)damage, velocity, distance);
			}
		
			else if (playerCtrl.weapon.m_Type == Weapons.WeaponType.YUTAN) {
				weapon = Weapon.CreateWeapon (builder, WepId.yutan, (short)damage, velocity, distance);
			}
			else if (playerCtrl.weapon.m_Type == Weapons.WeaponType.SAMURAI) {
				weapon = Weapon.CreateWeapon (builder, WepId.samurai, (short)damage, velocity, distance);
			}

			else if (playerCtrl.weapon.m_Type == Weapons.WeaponType.RIFLE_UP) {
				weapon = Weapon.CreateWeapon (builder, WepId.rifle_up, (short)damage, velocity, distance);
			}
			else if (playerCtrl.weapon.m_Type == Weapons.WeaponType.YUTAN_UP) {
				weapon = Weapon.CreateWeapon (builder, WepId.yutan_up, (short)damage, velocity, distance);
			}
			else if (playerCtrl.weapon.m_Type == Weapons.WeaponType.SAMURAI_UP) {
				weapon = Weapon.CreateWeapon (builder, WepId.samurai_up, (short)damage, velocity, distance);
			}
		
			var offset = builder.CreateString (playerCtrl.Name);
			var killoffset = builder.CreateString (killer);
			var aim = Vec3.CreateVec3 (builder,tr.x, tr.y, tr.z);
			
			Player.StartPlayer (builder);

			Player.AddAim (builder, aim);
			Player.AddName (builder, offset);
			Player.AddKiller (builder, killoffset);
			Player.AddShoot (builder, isShoot);
			Player.AddTime (builder, time);
			Player.AddWeapon (builder, weapon);

			var endOffset = Player.EndPlayer (builder);
			builder.Finish (endOffset.Value);

			int header = EndianConverter((int)ProtocolHeader.SHOOT);

			byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();
			if (u_transport != null && u_transport.IsConnected()) {
				u_transport.Send (newPacket, newPacket.Length);
			}

		}
	}
		
	public void Wound(object Param)
	{
		if (Param != null) {

			GameObject player = (GameObject)Param;
			PlayerMoveCtrl playerCtrl = player.GetComponent<PlayerMoveCtrl> ();

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (playerCtrl.Name);
			Player.StartPlayer (builder);
			Player.AddName (builder, offset);
			Player.AddHp (builder, (short)playerCtrl.HP);

			var endOffset = Player.EndPlayer (builder);
			builder.Finish (endOffset.Value);

			int header = EndianConverter ((int)ProtocolHeader.ACHE);

			byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();
			if (u_transport != null && u_transport.IsConnected()) {
				u_transport.Send (newPacket, newPacket.Length);
			}

		}
	}

	public void Dead(object Param)
	{
		
		if (Param != null) {

			GameObject player = (GameObject)Param;
			PlayerMoveCtrl playerCtrl = player.GetComponent<PlayerMoveCtrl> ();

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (playerCtrl.Name);
			Player.StartPlayer (builder);
			Player.AddName (builder, offset);



			var endOffset = Player.EndPlayer (builder);
			builder.Finish (endOffset.Value);

			int header = EndianConverter ((int)ProtocolHeader.DIE);

			byte[] newPacket = HeaderAdd (builder.SizedByteArray (), header);
			//byte[] newPacket = builder.SizedByteArray();

			if (t_transport != null && t_transport.IsConnected()) {
				t_transport.Send (newPacket, newPacket.Length);
			}

		}
	}
		
		
	public byte[] SerializeByte(byte[] buffer, int recvSize)
	{

		byte[] newPacket = new byte[recvSize];

		Buffer.BlockCopy (buffer, 8, newPacket, 0, recvSize);



		return newPacket;
	}

	public byte[] SerializeByteUDP(byte[] buffer, int recvSize)
	{

		byte[] newPacket = new byte[recvSize];

		Buffer.BlockCopy (buffer, 4, newPacket, 0, recvSize);



		return newPacket;
	}
	public byte[] HeaderAdd(byte[] buffer, int header)
	{
		/*
		byte[] newPacket = new byte[buffer.Length + 4];
		byte[] byteData = BitConverter.GetBytes (header);

		Buffer.BlockCopy (byteData, 0, newPacket, 0, 4);
		Buffer.BlockCopy (buffer, 0, newPacket, 4, buffer.Length);

		*/

		byte[] newPacket = new byte[buffer.Length + 8];
		byte[] byteHeader = BitConverter.GetBytes (header);
		byte[] byteLength = BitConverter.GetBytes (buffer.Length);

		Buffer.BlockCopy (byteHeader, 0, newPacket, 0, 4);
		Buffer.BlockCopy (byteLength, 0, newPacket, 4, 4);
		Buffer.BlockCopy (buffer, 0, newPacket, 8, buffer.Length);


		return newPacket;
	}

	public byte[] HeaderAddUDP(byte[] buffer, int header)
	{
		
		byte[] newPacket = new byte[buffer.Length + 4];
		byte[] byteData = BitConverter.GetBytes (header);

		Buffer.BlockCopy (byteData, 0, newPacket, 0, 4);
		Buffer.BlockCopy (buffer, 0, newPacket, 4, buffer.Length);


		return newPacket;
	}

	public byte[] DivisionPacket(byte[] buffer, int index, int length)
	{
		byte[] newPacket = new byte[length];

		Buffer.BlockCopy (buffer, index, newPacket, 0, length);

		return newPacket;

	}

	public int HeaderExtract(byte[] buffer)
	{
		byte[] byteHeader = new byte[4];
		
		Buffer.BlockCopy (buffer, 0, byteHeader, 0, 4);

		int header = ((byteHeader[3] << 24) + (byteHeader[2] << 16) + (byteHeader[1] << 8)
			+ (byteHeader[0]));
		
		return header;

	}

	public int LengthExtract(byte[] buffer)
	{

		byte[] byteLength = new byte[4];

		Buffer.BlockCopy (buffer, 4, byteLength, 0, 4);

		int length = ((byteLength[3] << 24) + (byteLength[2] << 16) + (byteLength[1] << 8)
			+ (byteLength[0]));
		
		return length;
	}
		
	void OnApplicationQuit()
	{
		
		FlatBufferBuilder builder = new FlatBufferBuilder (1);

		var offset = builder.CreateString (GameManager.Instance.userName);
		Player.StartPlayer (builder);
		Player.AddName (builder, offset);
		Player.AddGame (builder, Ingame.Out);
		var endOffset = Player.EndPlayer (builder);
		builder.Finish (endOffset.Value);

		int header = EndianConverter((int)ProtocolHeader.GAME_STATE);

		byte[] newPacket = HeaderAdd (builder.SizedByteArray (), header);
		//byte[] newPacket = builder.SizedByteArray();

		if (t_transport != null && t_transport.IsConnected()) {
			t_transport.Send (newPacket, newPacket.Length);
			t_transport.Disconnect ();
		}
		if (u_transport != null && u_transport.IsConnected()) {
			u_transport.Disconnect ();
		}
		if (manager != null && manager.Socket.IsOpen) {
			manager.Socket.Disconnect ();
		}
	}

	#endregion
}
