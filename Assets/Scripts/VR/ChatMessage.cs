using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour {


	public List<string> m_Msg;
	public Text m_Text;
	private int cnt = 0;

	void Awake()
	{

		m_Msg = new List<string> ();
		m_Text = GetComponent<Text> ();

	}
		
	public void Add(string msg)
	{
		
		string tmp = string.Empty;

		if (m_Msg.Count > 3) {
			m_Msg.RemoveAt (0);
			//cnt++;
		}

		m_Msg.Add (msg);

		for(int i = 0; i< m_Msg.Count; i++)
		{
			tmp += (m_Msg[i]+"\n");	
		}

		m_Text.text = tmp;
	}

	public void Enable()
	{
		string tmp = string.Empty;

		for(int i = 0; i< m_Msg.Count; i++)
		{
			tmp+= (m_Msg[i]+"\n");	
		}

		m_Text.text = tmp;		
	}
}
