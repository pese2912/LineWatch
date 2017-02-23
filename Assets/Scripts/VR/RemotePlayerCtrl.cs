using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocol;

public class RemotePlayerCtrl : MonoBehaviour , IListener{


	[Header("Controller Settings")]
	private Transform r_PlayerTr;
	public Transform Tr { get { return r_PlayerTr; } set { r_PlayerTr = Tr; } } 
	public CharacterController m_controller;
	private Vector3 m_moveDirection = Vector3.zero;

	[SerializeField]
	private float m_speed;
	private float m_horizontal = 0f;
	private float m_vertical = 0f;
	private float m_speedRotation=50;
	private float m_rotate=0f;

	public enum TeamState
	{
		GRAY =0,
		RED = 1,
		BLUE = 2,
	};

	public TeamState m_team;

	[SerializeField]
	private int m_HP;
	public int HP{ get { return m_HP;} set {m_HP = HP;}}
	[SerializeField]
	private string m_name;
	public string Name{ get { return m_name;} set {m_name = Name;}}
	//[SerializeField]
	//private int m_team;
	//public int Team { get { return m_team;} set {m_team = Team;}}
	public SelectData.CharacterType m_Type;



	[Header("Animation Settings")]
	public GameObject r_camera;
	public Animator m_animator;
	private enum R_PlayerState {Idle, run, Die};
	private  Vector3 DestTransform;
	private bool isDest;
	private Vector3 DestRotation;
	private bool isRot;


	[SerializeField]
	private R_PlayerState r_playerState;

	[Header("Shooting Settings")]
	public SkinnedMeshRenderer m_skin;
	[SerializeField]
	private bool isDie;
	public bool IsDie {get { return isDie;} set{ isDie = IsDie;}}
	public Weapons weapon;
	public Transform m_AcheEffect;
	public Transform m_hand;


	[Header("Interpolation Settings")]
	private Vector3 endPosition;
	private Transform startPosition;
	private Transform _target;

	private float Ispeed = 0.3f;
	private float startTime;
	private float journeyLength;

	[Header("Audio Settings")]
	public RemotePlayerAudio m_audio;
	public AudioSource m_AudioSource;
	private float m_StepCycle;
	private float m_NextStep;
	[SerializeField] private AudioClip[] m_FootstepSounds;
	[SerializeField] private float m_StepInterval;



	// Use this for initialization
	void Awake () {
		
		r_PlayerTr = GetComponent<Transform> ();

		r_camera = Instantiate (Resources.Load ("Camera")) as GameObject;
		r_camera.GetComponent<FollowCam> ().targetTr = r_PlayerTr;
		r_camera.SetActive (false);


		DestTransform = Vector3.zero;
		DestRotation = Vector3.zero;
		isDest = false;
		isRot = false;
		r_playerState = R_PlayerState.Idle;
		m_speed = 3f;
		m_HP = 100;
		isDie = false;
		m_StepCycle = 0f;
		m_NextStep = m_StepCycle/2f;
	
	}

	void OnEnable()
	{
		
		StartCoroutine (R_PlayerStateCheck ());
		StartCoroutine (R_PlayerAction());		
	}
		
	void Start()
	{

		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_MOVE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_ROTATE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_FIRE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_WOUND, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_RECALL, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_DEAD, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_RESPAWN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.PLAYER_OUT, this);
	
	}
		
	void FixedUpdate()
	{
		if ((!isDie && GameManager.Instance.isGame)) {

			if (!isDest) {
				/*
				m_moveDirection = new Vector3 (m_horizontal, 0, m_vertical);
				m_moveDirection.y = -9.8f;
				m_moveDirection = transform.TransformDirection (m_moveDirection);
				m_controller.Move (m_moveDirection * Time.deltaTime * m_speed);
				*/

				Vector3 desiredMove = transform.forward*m_vertical + transform.right*m_horizontal;

				// get a normal for the surface that is being touched to move along it
				RaycastHit hitInfo;
				Physics.SphereCast(transform.position, m_controller.radius, Vector3.down, out hitInfo,
					m_controller.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

				desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

				m_moveDirection.x = desiredMove.x*m_speed;
				m_moveDirection.y -= 9.8f;
				m_moveDirection.z = desiredMove.z*m_speed;

				m_controller.Move(m_moveDirection*Time.fixedDeltaTime);

			} 

			if (!isRot)
				transform.Rotate (0, m_rotate * Time.fixedDeltaTime, 0);

			ProgressStepCycle (7f);
		}
		else
			r_playerState = R_PlayerState.Idle;

	}



	private void ProgressStepCycle(float speed)
	{
		if (m_controller.velocity.sqrMagnitude > 0 && (m_vertical != 0 || m_horizontal != 0))
		{
			m_StepCycle += (m_controller.velocity.magnitude + (speed))*
				Time.fixedDeltaTime;
		}

		if (!(m_StepCycle > m_NextStep))
		{
			return;
		}

		m_NextStep = m_StepCycle + m_StepInterval;

		PlayFootStepAudio();
	}

	private void PlayFootStepAudio()
	{
		
		if (!m_controller.isGrounded)
		{
			return;
		}

		// pick & play a random footstep sound from the array,
		// excluding sound at index 0
		int n = Random.Range(1, m_FootstepSounds.Length);
		m_AudioSource.clip = m_FootstepSounds[n];
		m_AudioSource.PlayOneShot(m_AudioSource.clip);
		// move picked sound to index 0 so it's not picked next time
		m_FootstepSounds[n] = m_FootstepSounds[0];
		m_FootstepSounds[0] = m_AudioSource.clip;
	}


	IEnumerator R_PlayerStateCheck()
	{
		
		while (!isDie) {

			if (m_horizontal == 0 && m_vertical == 0 && m_rotate == 0) {
				if (!isDest)
					r_playerState = R_PlayerState.Idle;
			} else {
				r_playerState = R_PlayerState.run;
			}
			//yield return new WaitForSeconds (0.5f);
			yield return null;
		}
	}
		
	IEnumerator R_PlayerAction()
	{
		
		while (!isDie) {

			switch (r_playerState) {
			case R_PlayerState.Idle:	

				m_animator.SetBool ("Idle", true);
				m_animator.SetBool ("Run", false);
				m_animator.SetBool ("Die", false);
				//m_animator.SetBool ("Attack", false);
				break;

			case R_PlayerState.run:

				m_animator.SetBool ("Run", true);
				m_animator.SetBool ("Idle", false);
				m_animator.SetBool ("Die", false);
				//m_animator.SetBool ("Attack", false);
				break;

			
			case R_PlayerState.Die:

				m_animator.SetBool ("Run", false);
				m_animator.SetBool ("Idle", false);
				m_animator.SetBool ("Die", true);
				break;
			}
		
				
			yield return null;
		}

	}




	public IEnumerator Fire(object Param)
	{
		if (Param != null) {

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {

				switch(player.Weapon.Value.Id){

				case WepId.rifle:
					
					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack", true);

					//weapon.m_weapon.GetComponent<AudioSource> ().Play ();
					weapon.m_fireEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.1f);
					m_animator.SetBool ("Attack", false);


					for (int i = 0; i < GameManager.Instance.ShotGun_bulletPool.Count; i++) {
						if (!GameManager.Instance.ShotGun_bulletPool [i].activeSelf &&
						    GameManager.Instance.ShotGun_bulletPool [i] != null) { // 비활성화
							GameManager.Instance.ShotGun_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
							GameManager.Instance.ShotGun_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;

							GameManager.Instance.ShotGun_bulletPool [i].SetActive (true);
							break;
						}
					}
					break;

				case WepId.rifle_up:

					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack", true);
					weapon.m_SkillAudio.Play ();
					weapon.m_skillEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.1f);
					m_animator.SetBool ("Attack", false);


					for (int i = 0; i < GameManager.Instance.LaserGun_bulletPool.Count; i++) {
						if (!GameManager.Instance.LaserGun_bulletPool [i].activeSelf &&
							GameManager.Instance.LaserGun_bulletPool [i] != null) { // 비활성화
							GameManager.Instance.LaserGun_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
							GameManager.Instance.LaserGun_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;

							GameManager.Instance.LaserGun_bulletPool [i].SetActive (true);
							break;
						}
					}
					break;

				case WepId.yutan:
					
					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack", true);
					//weapon.m_weapon.GetComponent<AudioSource> ().Play ();
					weapon.m_fireEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.1f);
					m_animator.SetBool ("Attack", false);

					for (int i = 0; i < GameManager.Instance.Yutan_bulletPool.Count; i++) {
						if (!GameManager.Instance.Yutan_bulletPool [i].activeSelf &&
							GameManager.Instance.Yutan_bulletPool [i] != null) { // 비활성화
							GameManager.Instance.Yutan_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
							GameManager.Instance.Yutan_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;


							GameManager.Instance.Yutan_bulletPool [i].SetActive (true);
							break;
						}
					}
					break;

				case WepId.yutan_up:
					
					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack", true);
					weapon.m_SkillAudio.Play ();
					weapon.m_skillEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.1f);
					m_animator.SetBool ("Attack", false);

					for (int i = 0; i < GameManager.Instance.PlasmaGun_bulletPool.Count; i++) {
						if (!GameManager.Instance.PlasmaGun_bulletPool [i].activeSelf &&
							GameManager.Instance.PlasmaGun_bulletPool [i] != null) { // 비활성화
							GameManager.Instance.PlasmaGun_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
							GameManager.Instance.PlasmaGun_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;

							GameManager.Instance.PlasmaGun_bulletPool [i].SetActive (true);
							break;
						}
					}
					break;

				case WepId.samurai:
										
					int n = Random.Range (1, 3);

					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack"+n, true);
					weapon.m_weapon.GetComponent<AudioSource> ().Play ();
					//weapon.m_skillEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.3f);

					m_animator.SetBool ("Attack"+n, false);
					break;


				case WepId.samurai_up:
					
					int n2 = Random.Range (1, 3);
					weapon.m_SkillAudio.Play ();
					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					m_animator.SetBool ("Ache", false);
					m_animator.SetBool ("Attack"+n2, true);
					weapon.m_weapon.GetComponent<AudioSource> ().Play ();
					weapon.m_skillEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds(0.5f);

					m_animator.SetBool ("Attack"+n2, false);
					break;

				};

				yield return null;
				weapon.m_fireEffect.gameObject.SetActive (false);
				weapon.m_skillEffect.gameObject.SetActive (false);
			}
		}
	}

	public void RemotePlayer(string Name, TeamState TeamNo, Vector3 Tr, float r)
	{
		
		m_name = Name;
		m_team = TeamNo;
		r_PlayerTr.name = Name;
		r_camera.name = "camera "+Name;
		r_PlayerTr.position = Tr;

		r_PlayerTr.eulerAngles = new Vector3 (0f, r, 0f);

	}


	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{
		
		switch (Event_Type) {
		case EVENT_TYPE.R_PLAYER_MOVE:
			Move (Param);
			break;

		case EVENT_TYPE.R_PLAYER_ROTATE:
			Rotate (Param);
			break;

		case EVENT_TYPE.R_PLAYER_WOUND:
			StartCoroutine ("Wound", Param);
			break;

		case EVENT_TYPE.R_PLAYER_RECALL:
			StartCoroutine ("Recall", Param);
			break;

		case EVENT_TYPE.R_PLAYER_FIRE:
			StartCoroutine ("Fire", Param);

			break;

		case EVENT_TYPE.R_PLAYER_DEAD:
			Dead (Param);
			break;

		case EVENT_TYPE.R_PLAYER_RESPAWN:
			Respawn (Param);
			break;

		case EVENT_TYPE.PLAYER_OUT:
			PlayerOut (Param);
			break;

		}
	}

	public void PlayerOut(object Param)
	{

		if (Param != null) {

			Player player = (Player)Param;
			if (m_name.Equals (player.Name) && player.Name != null) {


				for (int i = 0; i < GameManager.Instance.CameraPool.Count; i++) {
					if (GameManager.Instance.CameraPool [i].name.Equals ("camera " + m_name)) {
						if (GameManager.Instance.m_WebCam == r_camera) {
							GameManager.Instance.Idx= 0;
							GameManager.Instance.CameraPool.RemoveAt (i);
							GameManager.Instance.OnClickCamTranslate ();
							DestroyImmediate (r_camera);
						} else {
							GameManager.Instance.CameraPool.RemoveAt (i);
							DestroyImmediate (r_camera);
						}
						//r_camera.SetActive(false);
						break;
					}
				}
				DestroyImmediate (gameObject);

			}
		} 
	} 

	public void Move (object Param)
	{
		
		if (Param != null) {
			

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null)  {
				

				StopCoroutine (Interpolation());

				DestTransform = new Vector3 (player.Pos.Value.X, r_PlayerTr.position.y, player.Pos.Value.Z);
				isDest = true;

				//
				startTime = Time.time; // 시간
				startPosition = r_PlayerTr;
				endPosition = DestTransform;
				_target = r_PlayerTr;

				journeyLength = Vector3.Distance(startPosition.position, endPosition);  // 이동 거리

				if(!isDie && GameManager.Instance.isGame)
					StartCoroutine (Interpolation ());

				//

				m_horizontal = player.Pos.Value.Horizontal;
				m_vertical = player.Pos.Value.Vertical;

			}
		}
	}

	public IEnumerator Interpolation()
	{
		yield return null;

		while (true)
		{
			r_playerState = R_PlayerState.run;


			Vector3 targetPos = transform.position + (endPosition - transform.position);

			Vector3 framePos = Vector3.MoveTowards(transform.position, targetPos, 1.5f * Time.fixedDeltaTime);
			Vector3 moveDir = (framePos - transform.position) + Physics.gravity;
					
			m_controller.Move(moveDir);

			if (isDie || !GameManager.Instance.isGame)
				break;

			if (Vector3.Distance(framePos , targetPos) < 0.1f) {
				r_playerState = R_PlayerState.Idle;
				isDest = false;
				break;
			}
			yield return new WaitForFixedUpdate();
		
		}
	}

	public void Rotate (object Param)
	{

		if (Param != null) {

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {
				
				//StopCoroutine (InterRotation ());
				r_PlayerTr.localEulerAngles = new Vector3 (0f, player.Rot.Value.R, 0f);
				//DestRotation = new Vector3 (0f, player.Rot.Value.R, 0f);
				//print (DestRotation);
				//isRot = true;

				//StartCoroutine (InterRotation ());
				m_rotate = player.Rot.Value.Rotate;

			}
		}
	}

	public IEnumerator InterRotation()
	{
		yield return null;

		Quaternion tmp = Quaternion.identity;
		tmp.eulerAngles = DestRotation;

		while (true) {
			
			r_PlayerTr.Rotate (0, 60*Time.deltaTime, 0);

			if (r_PlayerTr.localEulerAngles == DestRotation) {
				isRot = false;
				break;
			}

			yield return null;

		}

	}

	public IEnumerator Wound(object Param)
	{

		if (Param != null) {

			Player player = (Player)Param;
			if (m_name.Equals (player.Name) && player.Name != null) {

				yield return new WaitForSeconds(0.2f);

				m_audio.Audio ();
				m_AcheEffect.gameObject.SetActive (true);
				m_HP = (int)player.Hp;
				//m_HP -= 10;
				if(m_team == TeamState.GRAY)
					gameObject.GetComponent<TargetHP> ().targetAche (m_HP);

				if (m_Type != SelectData.CharacterType.SAMURAI) {
					m_animator.SetBool ("Run", false);
					m_animator.SetBool ("Idle", false);
					//m_animator.SetBool ("Attack", false);
					m_animator.SetBool ("Ache", true);
					yield return new WaitForSeconds (0.2f);

					m_animator.SetBool ("Ache", false);
				}
				else
					yield return new WaitForSeconds (0.2f);

				m_AcheEffect.gameObject.SetActive (false);
						
			}
		}
	}

	public void Dead(object Param)
	{

		if (Param != null) {

			Player player = (Player)Param;
			if (m_name.Equals (player.Name) && player.Name != null) {

				StartCoroutine( DieEvent ());
				if(m_team == TeamState.GRAY)
					gameObject.GetComponent<TargetHP> ().targetDie();
			}
		}
	}


	IEnumerator DieEvent(){
		yield return new WaitForSeconds(0.2f);

		m_audio.isDie = true;
		m_audio.Audio ();
		weapon.m_ExplosionEffect.gameObject.SetActive (true);
		isDie = true;

		m_animator.SetBool ("Run", false);
		m_animator.SetBool ("Idle", false);
	
		//m_animator.SetBool ("Attack", false);
		m_animator.SetBool ("Die", true);

		m_controller.enabled = false;
		yield return new WaitForSeconds(2f);
		m_skin.enabled = false;
		weapon.m_ExplosionEffect.gameObject.SetActive (false);
	}

	IEnumerator Recall(object Param){

		yield return null;
		if (Param != null) {
			 
			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {
				r_PlayerTr.position = new Vector3 (player.Aim.Value.X, r_PlayerTr.position.y, player.Aim.Value.Z);
				weapon.m_portalEffect.gameObject.SetActive (true);
			}
		}
	}

	public void Respawn(object Param)
	{
		if (Param != null) {
			
			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {

				r_PlayerTr.position = new Vector3 (player.Pos.Value.X, player.Pos.Value.Y, player.Pos.Value.Z);

				r_PlayerTr.eulerAngles = new Vector3 (0f, player.Pos.Value.R, 0f);

				if (m_team == TeamState.GRAY) {

					gameObject.GetComponent<TargetHP> ().targetRespawn (100);

				}
			
				m_HP = 100;
				m_controller.enabled = true;
				m_skin.enabled = true;
				m_audio.isDie = false;
				m_animator.SetBool ("Idle", true);
				m_animator.SetBool ("Run", false);
				m_animator.SetBool ("Die", false);

				//m_animator.SetBool ("Attack", false);

				isDie = false;

				StopAllCoroutines ();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
				StartCoroutine (R_PlayerStateCheck());
				StartCoroutine (R_PlayerAction());
			}
		}
	}

}