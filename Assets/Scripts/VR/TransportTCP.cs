using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;


public class TransportTCP : MonoBehaviour {


	//
	// socket connect
	//

	//Listening Socket
	private Socket	m_listener = null;

	//Connect socket for Client
	private Socket	m_socket = null;

	//Send Buffer
	private PacketQueue	m_sendQueue;

	//Receive Buffer
	private PacketQueue	m_recvQueue;

	//Server flag
	private bool	m_isServer = false;

	//Connect flag
	private bool	m_isConnected = false;

	//
	// Event Member
	//

	//Event Delegate
	public delegate void EventHandler(NetEventState state);

	private EventHandler	m_handler;

	//
	//	Thread Member 
	//

	//Thread flag
	protected bool	m_threadLoop = false;

	protected Thread	m_thread = null;

	private static int s_mtu = 1400;



	// Use this for initialization
	void Start () {


		//send,recv buffer
		m_sendQueue = new PacketQueue();
		m_recvQueue = new PacketQueue();

	}
	



	//wait start

	public bool StartServer(int port, int connectionNum)
	{
		Debug.Log ("StartServer Called!");

		//Listening socket create

		try{
			
			//socekt create
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//use port bind
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			//wait start
			m_listener.Listen(connectionNum);
		}
		catch{
			Debug.Log ("StartServer fail");
			return false;
		}

		m_isServer = true;

		return LaunchThread ();

	}

	//wait stop
	public void StopServer(){

		m_threadLoop = false;
		if (m_thread != null) {
			m_thread.Join ();
			m_thread = null;
		}

		Disconnect ();

		if (m_listener != null) {
			m_listener.Close ();
			m_listener = null;
		}

		m_isServer = false;

		Debug.Log ("Server stopped.");

	}


	//Connect
	public bool Connect(string address, int port)
	{
		Debug.Log ("TransportTCP connect called.");

		if (m_listener != null) {
			return false;
		}

		bool ret = false;

		try{
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//m_socket.Bind(new IPEndPoint(IPAddress.Any, port));
			m_socket.NoDelay =true;
			//m_socket.Connect("10.70.24.188", 8992);
			m_socket.Connect(address, port);
			ret = LaunchThread();

		}
		catch{
			m_socket = null;
		}

		if (ret == true) {
			m_isConnected = true;
			Debug.Log ("Connection success");
		} else {
			m_isConnected = false;
			Debug.Log ("Connect fail");
		}

		if (m_handler != null) {
			//connection notification
		
			NetEventState state = new NetEventState ();
			state.type = NetEventType.Connect;
			state.result = (m_isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
			m_handler (state);
			Debug.Log ("evnet handler called");

		}

		return m_isConnected;

	}


	//disconnected
	public void Disconnect(){

		m_isConnected = false;

		if (m_socket != null) {
			//socket close
			m_socket.Shutdown (SocketShutdown.Both);
			m_socket.Close ();
			m_socket = null;
		}

		//disconnect Notification
		if (m_handler != null) {
			NetEventState state = new NetEventState ();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			m_handler (state);
		}

		Debug.Log("TransportTCP::Disconnect called.");
	}

	//send
	public int Send(byte[] data, int size)
	{
		if (m_sendQueue == null) {
			return 0;
		}
	
		string msg = System.Text.Encoding.UTF8.GetString(data);
	

		return m_sendQueue.Enqueue (data, size);

	}

	//receive
	public int Receive(ref byte[] buffer, int size)
	{
		
		if (m_recvQueue == null) {
			return 0;
		}

		int q = m_recvQueue.Dequeue (ref buffer, size);

		return q;
	
	}

	//event notification register
	public void RegisterEventHandler(EventHandler handler)
	{
		m_handler += handler;
	}

	//event notification delete
	public void UnregisterEventHandler(EventHandler handler)
	{
		m_handler -= handler;
	}


	//Thread Launch function
	bool LaunchThread()
	{
		try {
			//Dispatch Thread
			m_threadLoop = true;
			m_thread = new Thread (new ThreadStart (Dispatch));
			m_thread.Start ();
		} catch {
			Debug.Log ("Cannot launch thread.");
			return false;
		}

		return true;
	}

	//Thread send/recv
	public void Dispatch(){

		Debug.Log ("Dispatch thread started.");

		while (m_threadLoop) {
			// wait for client Connection
			AcceptCilent ();

			//Send/recv Client
			if (m_socket != null && m_isConnected == true) {

				//Send
				DispatchSend ();

				//Recv
				DispatchReceive ();
			}

			Thread.Sleep (5);
		}
		Debug.Log ("Dispatch thread ended.");
	}


	//Accept Client
	void AcceptCilent()
	{
		if (m_listener != null && m_listener.Poll (0, SelectMode.SelectRead)) {
			//Connection Client
			m_socket = m_listener.Accept ();
			m_isConnected = true;
			Debug.Log ("Connected from client");
		}
	}

	//Thread Send
	void DispatchSend()
	{
		
		try{
			if(m_socket.Poll(0,SelectMode.SelectWrite)){
				byte[] buffer = new byte[s_mtu];
			
				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);


				while(sendSize > 0){
				//	print("tcp send:"+sendSize);
					int r = m_socket.Send(buffer, sendSize, SocketFlags.None);

					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				
				}
			}
		}
		catch{ 
			return;
		}
	}

	//Thread Receive
	void DispatchReceive()
	{
		
		try{
			while(m_socket.Poll(0, SelectMode.SelectRead)){
				byte[] buffer = new byte[s_mtu];

				int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
		
			
				if(recvSize == 0){
					//disconnect
					Debug.Log("Disconnec recv from client");
					Disconnect();
				}

				else if(recvSize > 0){
					
				//	print("tcp recv:"+recvSize);
					m_recvQueue.Enqueue(buffer, recvSize);
			
				}
			}
		} 

		catch{
			return;
		}
	}

	//isServer
	public bool IsServer(){
		return m_isServer;
	}

	//isConnect
	public bool IsConnected(){
		return m_isConnected;
	}

}
