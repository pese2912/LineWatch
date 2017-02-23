using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerAudio : MonoBehaviour {

	public AudioSource m_audio;
	public AudioClip impactAudio;
	public AudioClip explosionAudio;

	public bool isDie = false;

	// Use this for initialization
	void Start () {
		m_audio = GetComponent<AudioSource> ();
	}
	
	public void Audio()
	{
		if (!isDie) {
			m_audio.clip = impactAudio;
			m_audio.PlayOneShot (impactAudio);
		} else {
			m_audio.clip = explosionAudio;
			m_audio.PlayOneShot (explosionAudio);
		}
	}
}
