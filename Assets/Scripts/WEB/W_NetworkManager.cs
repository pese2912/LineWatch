using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using FlatBuffers;
using Protocol;
using System;
using BestHTTP;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Networking;
using BestHTTP.WebSocket;
using UnityEngine.UI;

public class W_NetworkManager : MonoBehaviour, IListener {

	#region
	public static W_NetworkManager Instance {
		get{ return instance; }
		set{ }
	}

	#endregion

	#region
	private static W_NetworkManager instance = null;
	// 통신 모듈.

	public UnityWebRequest request = null;
	public WebSocket m_transport = null;


	// 접속할 IP 주소.
	private string		m_IP = string.Empty;

	// 접속할 포트 번호.
	private int 	m_port = 0;

	private const int 	m_mtu = 1400;

	private enum ProtocolHeader {EXCEPTE=0, PLAYER_REQ=1, PLAYER_RES=2, GAME_STATE=3,
		MOVE=4,ROTATE = 5,SHOOT=6, ACHE=7, RECALL=8, DIE=9, RESPAWN=10, RESULT=11 , HEART_BEAT = 12, PLAYER_OUT = 14};


	[SerializeField]
	private ProtocolHeader p_Header;


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

		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_LOGIN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_ROOMLIST, this);
		EventManager.Instance.AddListener (EVENT_TYPE.NETWORK_ROOM_JOIN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAME_END, this);

			
	}



	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{

		switch (Event_Type) {


		case EVENT_TYPE.NETWORK_LOGIN: //http

			StartCoroutine(Login());
			break;

		case EVENT_TYPE.NETWORK_ROOMLIST: // http
			StartCoroutine("RoomList");

			break;

		case EVENT_TYPE.NETWORK_ROOM_JOIN: //http
			StartCoroutine( "NetworkRoomJoin",Param);
			break;

		case EVENT_TYPE.GAME_END: //tcp

			GameEnd (Param);  
			break;
		};
	}

	private void OnBinaryMessageReceived(WebSocket webSocket, byte[] buffer)
	{
		int m_recvSize = buffer.Length;
	//	Debug.Log ("message recvsize" + m_recvSize);

		if (m_recvSize > 0) {

			int header = HeaderExtract (buffer);
			//int length = LengthExtract (u_buffer);
			header = EndianConverter (header);
			//length = EndianConverter (length);
			
			byte[] newPacket = SerializeByteUDP (buffer, buffer.Length-4);

			FlatBuffers.ByteBuffer bb = new FlatBuffers.ByteBuffer (newPacket);
			Player player = Player.GetRootAsPlayer (bb);
			print ("header : " + header + "recv : " + m_recvSize);

			switch (header) {

			case (int)ProtocolHeader.PLAYER_REQ:					
				break;

			case (int)ProtocolHeader.PLAYER_RES: 

				EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_CONNECT, this, player);
				break;

			case (int)ProtocolHeader.GAME_STATE:

				if (player.Game == Ingame.Start) {

					EventManager.Instance.PostNotification (EVENT_TYPE.GAME_START, this, player);
				} else if (player.Game == Ingame.End) {

					EventManager.Instance.PostNotification (EVENT_TYPE.NETWORK_GAME_END, this, player);
				}

				break;


			case (int)ProtocolHeader.DIE: 
				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_DEAD, this, player);
				break;

			case (int)ProtocolHeader.RESPAWN: 
				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RESPAWN, this, player);
				break;

			case (int)ProtocolHeader.RESULT: 
				Result result = Result.GetRootAsResult (bb);
				EventManager.Instance.PostNotification (EVENT_TYPE.GAME_RESULT, this, result);
				break;


			case (int)ProtocolHeader.ACHE: 
				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_WOUND, this, player);
				break;

			case (int)ProtocolHeader.RECALL: 
				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_RECALL, this, player);
				break;

			case (int)ProtocolHeader.MOVE: 

				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_MOVE, this, player);
				break;

			case (int)ProtocolHeader.ROTATE: 

				EventManager.Instance.PostNotification (EVENT_TYPE.R_PLAYER_ROTATE, this, player);
				break;
			case (int)ProtocolHeader.SHOOT: 
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
		
	private int EndianConverter(int param) {

		byte[] data = BitConverter.GetBytes(param);
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(data);
		}
		return BitConverter.ToInt32(data, 0);
	}

	public IEnumerator Login()
	{
		
		string data = string.Empty;

		WWWForm form = new WWWForm ();
		form.AddField ("token", "");

		request = UnityWebRequest.Post ("http://10.113.189.193:3000/auth/register", form);
		yield return request.Send ();

		if (request.isError)
			Debug.Log (request.error);

		string result = System.Text.Encoding.UTF8.GetString (request.downloadHandler.data);
		PlayerInfo playerInfo = JsonConvert.DeserializeObject<PlayerInfo> (result);

		if (playerInfo.message.Equals ("success")) {
			GameManager.Instance.userName = playerInfo.name;
			GameManager.Instance.userToken = playerInfo.token;

			PlayerPrefs.SetString ("USER_NAME", GameManager.Instance.userName);
			PlayerPrefs.SetString ("USER_TOKEN", GameManager.Instance.userToken);
			EventManager.Instance.PostNotification (EVENT_TYPE.LOBBY_LOGIN, this);
		}

	}

	public IEnumerator RoomList()
	{
		
		request = UnityWebRequest.Get ("http://10.113.189.193:3000/room/list");
		yield return request.Send ();
	
		if (request.isError)
			Debug.Log (request.error);

		string result = System.Text.Encoding.UTF8.GetString (request.downloadHandler.data);

		W_RoomList roomList = JsonConvert.DeserializeObject<W_RoomList> (result);

		EventManager.Instance.PostNotification (EVENT_TYPE.LOBBY_ROOMLIST, this, roomList);
		
	}
		
	public IEnumerator NetworkRoomJoin(object Param)
	{
		
		if (Param != null) {
			
			GameServerInfo gameServerInfo  = (GameServerInfo)Param;


			m_IP = gameServerInfo.game_ip;
			m_port = int.Parse(gameServerInfo.game_port);

			string url = "WS://" + m_IP+ ":" + m_port+"/websocket";
			m_transport = new WebSocket(new Uri(url));
			m_transport.OnOpen += OnWebSocketOpen;
			m_transport.OnBinary += OnBinaryMessageReceived;
			m_transport.OnError += OnError;
			m_transport.OnClosed += OnWebSocketClosed;

			m_transport.Open ();

			yield return new WaitForSeconds (1f);

			string name = GameManager.Instance.userName;
			int roomNo = int.Parse (gameServerInfo.roomid);

			FlatBufferBuilder builder = new FlatBufferBuilder (1);

			var offset = builder.CreateString (name);
			Player.StartPlayer (builder);
			Player.AddName (builder, offset);
			Player.AddRoomid (builder, roomNo);
			var endoffset = Player.EndPlayer (builder);
			builder.Finish (endoffset.Value);

			int header = EndianConverter((int)ProtocolHeader.PLAYER_REQ);
			byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
			if (m_transport != null && m_transport.IsOpen) {
				
				m_transport.Send (newPacket);
			}

			EventManager.Instance.PostNotification (EVENT_TYPE.GAMEMANAGER_CONNECT, this, null); // param playerInfo(id,team,), RemotePlayer(id,team)

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
		#if !UNITY_EDITOR && UNITY_WEBGL
		Application.ExternalCall( "GameExit", "The game says bye!" );	
		#endif

		FlatBufferBuilder builder = new FlatBufferBuilder (1);

		var offset = builder.CreateString (GameManager.Instance.userName);
		Player.StartPlayer (builder);
		Player.AddName (builder, offset);
		Player.AddGame (builder, Ingame.Out);
		var endOffset = Player.EndPlayer (builder);
		builder.Finish (endOffset.Value);
	
		int header = EndianConverter((int)ProtocolHeader.GAME_STATE);

		byte[] newPacket = HeaderAddUDP(builder.SizedByteArray (), header);

		if (m_transport != null && m_transport.IsOpen) {
			m_transport.Send(newPacket);

			m_transport.Close ();
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

		byte[] newPacket = HeaderAddUDP (builder.SizedByteArray (), header);
		//byte[] newPacket = builder.SizedByteArray();

		if (m_transport != null && m_transport.IsOpen) {
			m_transport.Send(newPacket);

			m_transport.Close ();
		}
	
	}
	private void OnWebSocketOpen(WebSocket webSocket)
	{
		print("WebSocket Open!");
	}

	private void OnWebSocketClosed(WebSocket webSocket, UInt16 code, string message)
	{
		print ("WebSocket Closed!");
	}

	private void OnError(WebSocket ws, Exception ex) {
		//string errorMsg = string .Empty;
		//if (ws.InternalRequest.Response != null)
		//	errorMsg = string.Format("Status Code from Server: {0} and Message: {1}", ws.InternalRequest.Response.StatusCode, ws.InternalRequest.Response.Message);
		//Debug.Log("An error occured: " + (ex != null ? ex.Message : "Unknown: " + errorMsg));
	}

	#endregion
}