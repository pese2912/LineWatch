using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_PlayerAdapter {

	private static R_PlayerAdapter instance;

	public List<GameObject> m_list = null;

	public static R_PlayerAdapter getInstance(){

		if (instance == null) {
			instance = new R_PlayerAdapter ();
		}
		return instance;
	}

	private R_PlayerAdapter()
	{
		m_list = new List<GameObject> ();
	}
		
	public void Add(GameObject r_Player)
	{
		m_list.Add(r_Player);

	} 
		
	public List<GameObject> getList(){
		return m_list;
	}

	public int getSize()
	{
		return m_list.Count;
	}

	public void Clear(){
		m_list.Clear ();

	}

	public void RemoveAt(GameObject r_Player)
	{
		m_list.Remove (r_Player);
	
	}

}

