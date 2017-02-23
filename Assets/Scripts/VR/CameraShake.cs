using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour {

	// Transform of the camera to shake. Grabs the gameObject's transform
	// if null.
	public Transform camTransform;

	// How long the object should shake for.
	public float shake = 1f;
	private float _shake;
	// Amplitude of the shake. A larger value shakes the camera harder.
	public float shakeAmount = 0.3f;
	public float decreaseFactor = 1.0f;

	public bool isDie = false;

	public AudioSource m_audio;
	public AudioClip explosionAudio;
	public AudioClip impactAudio;

	Vector3 originalPos;

	void OnEnable()
	{
		_shake = shake;
		if (camTransform == null)
		{
			camTransform = GetComponent(typeof(Transform)) as Transform;
		}
		originalPos = camTransform.localPosition;
		m_audio = camTransform.GetComponent<AudioSource> ();

		StartCoroutine (Shake ());
	}

	public IEnumerator Shake(){

		if (!isDie) {
			m_audio.clip = impactAudio;
			m_audio.PlayOneShot (impactAudio);
		} else if (isDie) {
			m_audio.clip = explosionAudio;
			m_audio.PlayOneShot (explosionAudio);
		}
		while (_shake > 0)
		{
			camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
			_shake -= Time.deltaTime * decreaseFactor;
			yield return new WaitForSeconds (0.01f);
		}

		_shake = 0f;
		camTransform.localPosition = originalPos;
		yield return null;

	}

}
