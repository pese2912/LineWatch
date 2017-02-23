using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Protocol;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
	
public class PlayerMoveCtrl : MonoBehaviour, IListener{

	[Header("Controller Settings")]
	public CharacterController m_controller;
	private Vector3 m_moveDirection = Vector3.zero;
	private Transform m_PlayerTr;
	public Transform Tr { get { return m_PlayerTr; } set { m_PlayerTr = Tr; } } 
	[SerializeField]
	private float m_speed;
	private float m_horizontal = 0f;
	public float horizontal = 0f;
	private float m_vertical = 0f;
	public float vertical = 0f;
	private float m_speedRotation=50;
	private float m_rotate;
	public float rotate;


	public enum PlayerState {Idle, run, Die};

	[SerializeField]
	public PlayerState playerState;

	public enum TeamState
	{
		GRAY =0,
		RED = 1,
		BLUE = 2,
	};

	public TeamState m_team;

	public enum MoveState
	{
		Idle = 0,
		East = 1,
		South = 2,
		West = 3,
		North = 4,
		ES = 5,
		SW = 6,
		WN = 7,
		NE = 8,
	};

	public MoveState moveState;

	public SelectData.CharacterType m_Type;

	public enum RotateState{
		
		Idle = 0,
		Left = 1,
		Right = 2,

	}

	public RotateState rotateState;


	[Header("PlayerInit Settings")]
	[SerializeField]
	private int m_HP;
	public int HP{ get { return m_HP;} set {m_HP = HP;}}
	private int initHp;
	private float guage;

	[SerializeField]
	private bool isDie;
	public bool IsDie {get { return isDie;} set{ isDie = IsDie;}}
	[SerializeField]
	private string m_name;
	public string Name{ get { return m_name;} set {m_name = Name;}}
	public MeshRenderer m_skin;
	public Image m_imgHpbar;
	public Image m_GuageBar;
	public CameraShake m_shake;
	public Animator m_animator;
	public AudioSource m_audioSource;



	[Header("Weapon Settings")]
	public Weapons weapon;
	private float timer=0;
	public float attackCoolTime = 0.3f;
	private Vector3 m_OriginalWeaponPosition;
	private bool isGuage;
	private bool isRecall;

	[Header("HeadBob Settings")]
	[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
	[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
	[SerializeField] private float m_StepInterval;
	[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
	private float m_StepCycle;
	private float m_NextStep;


	// Use this for initialization
	void Awake () {
		
		m_PlayerTr = GetComponent<Transform> ();
	
		//m_imgHpbar = GameObject.FindWithTag ("PLAYER_HP").GetComponent<Image>();
		playerState = PlayerState.Idle;
		moveState = MoveState.Idle;
		rotateState = RotateState.Idle;
		m_speed = 3f;
		guage = 0f;
		m_HP = 100;
		isDie = false;
		isGuage = false;
		isRecall = false;
		m_StepCycle = 0f;
		m_NextStep = m_StepCycle/2f;
		initHp = m_HP;

	}

	void Start()
	{
		m_HeadBob.Setup(weapon.m_weapon.transform, m_StepInterval);
		m_OriginalWeaponPosition = weapon.m_weapon.transform.localPosition;
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_RESPAWN, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_WOUND, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_RECALL, this);
		EventManager.Instance.AddListener (EVENT_TYPE.R_PLAYER_DEAD, this);
	}



	void Update()
	{
		timer += Time.deltaTime;
		if (GameManager.Instance.isGame && !isDie && !isRecall) {
			
	
			//회전

			if (Input.GetMouseButtonDown (0)) {

				StartCoroutine (Fire ());

			}

			if (Input.GetButton ("L1") || Input.GetButton ("Jump")) {
				if (timer >= attackCoolTime) {
					StartCoroutine (Fire ());
					timer = 0;
				}

			}

			if (Input.GetButtonDown ("B")) {
				if (isGuage) {
					StartCoroutine (SkillAtack ());
				}
			}
		}

	}
	// Update is called once per frame
	void FixedUpdate () {
		


		if (GameManager.Instance.isGame && !isDie && !isRecall) {
			m_horizontal = Input.GetAxis ("Horizontal"); 
			m_vertical = Input.GetAxis ("Vertical"); 

			m_rotate = Input.GetAxis("Oculus_GearVR_RThumbstickX") * m_speedRotation; // 회전
			//회전
			/*
			if (Input.GetMouseButtonDown (0)) {
				
				StartCoroutine (Fire ());

			}

			if (Input.GetButton ("L1") || Input.GetButton("Jump")) {
				if (timer >= attackCoolTime) {
					StartCoroutine (Fire ());
					timer = 0;
				}

			}

			if (Input.GetButtonDown ("B")) {
				if (isGuage) {
					StartCoroutine (SkillAtack ());
				}
			}
				*/

			if ((Mathf.Abs (m_vertical) <= 0.05f && Mathf.Abs (m_horizontal) <= 0.05f)) {

				if (moveState != MoveState.Idle) {
					horizontal = 0f;
					vertical = 0f;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}

				moveState = MoveState.Idle;
			} else if (m_horizontal >= 0.5f && 0 <= m_vertical && m_vertical < 0.5f) {
				if (moveState != MoveState.East) {
					horizontal = 1f; //m_horizontal;
					vertical = 0f; //m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.East;
			} else if (m_horizontal <= -0.5f && 0 <= m_vertical && m_vertical < 0.5f) {
				if (moveState != MoveState.West) {
					horizontal = -1f; //m_horizontal;
					vertical = 0f; //m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.West;

			} else if (m_horizontal >= 0 && m_horizontal < 0.5f && m_vertical >= 0.5f) {
				if (moveState != MoveState.North) {
					horizontal = 0f;//m_horizontal;
					vertical = 1f; //m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.North;
			} else if (m_horizontal >= 0 && m_horizontal < 0.5f && m_vertical <= -0.5f) {
				if (moveState != MoveState.South) {
					horizontal = 0f;//m_horizontal;
					vertical = -1f;//m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.South;
			} else if (m_horizontal >= 0.5f && m_vertical >= 0.5f) {
				if (moveState != MoveState.NE) {
					horizontal = 1f;//m_horizontal;
					vertical = 1f; //m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.NE;
			} else if (m_horizontal >= 0.5f && m_vertical <= -0.5f) {
				if (moveState != MoveState.ES) {
					horizontal = 1f;//m_horizontal;
					vertical = -1f; //m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.ES;
			} else if (m_horizontal <= -0.5f && m_vertical <= -0.5f) {
				if (moveState != MoveState.SW) {
					horizontal = -1f;//m_horizontal;
					vertical = -1f;//m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}

				moveState = MoveState.SW;
			} else if (m_horizontal <= -0.5f && m_vertical >= 0.5f) {
				if (moveState != MoveState.WN) {
					horizontal = -1f;//m_horizontal;
					vertical = 1f;//m_vertical;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
				}
				moveState = MoveState.WN;
			}

		
			if ((Mathf.Abs (m_rotate) <= 3f)) {
				if (rotateState != RotateState.Idle) {
					rotate = 0f;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_ROTATE, this, gameObject);
				}
				rotateState = RotateState.Idle;
			} else if (m_rotate <= -50f) {
				if (rotateState != RotateState.Left) {
					rotate = -60f;//m_rotate;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_ROTATE, this, gameObject);
				}
				rotateState = RotateState.Left;
			} else if (m_rotate >= 50f) {
				if (rotateState != RotateState.Right) {
					rotate = 60f; //m_rotate;
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_ROTATE, this, gameObject);
				}
				rotateState = RotateState.Right;
			}
		
			/*
			//전진,후진
			m_moveDirection = new Vector3 (horizontal, 0, vertical);
			m_moveDirection.y -= 9.8f;
			m_moveDirection = transform.TransformDirection (m_moveDirection);

			m_controller.Move (m_moveDirection * Time.deltaTime * m_speed);

			*/
			Vector3 desiredMove = transform.forward*vertical + transform.right*horizontal;

			// get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_controller.radius, Vector3.down, out hitInfo,
				m_controller.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_moveDirection.x = desiredMove.x*m_speed;
			m_moveDirection.y -= 9.8f;
			m_moveDirection.z = desiredMove.z*m_speed;

			m_controller.Move(m_moveDirection*Time.fixedDeltaTime);

		
			transform.Rotate(0, rotate*Time.fixedDeltaTime, 0);
					
			UpdateWeaponPosition(m_speed);
			ProgressStepCycle (7f);
			if(m_GuageBar != null)
				ProgressGuageBar ();


		}
	}

	private void ProgressGuageBar()
	{
		
		guage += Time.fixedDeltaTime*20f;	
		m_GuageBar.fillAmount = (guage / (float)initHp);

		if (m_GuageBar.fillAmount >= 1)
			isGuage = true;
	}

	public IEnumerator SkillAtack()
	{
		yield return null;


		if (!isDie) {
			
			isGuage = false;
			guage = 0f;

			switch(m_Type){
			case SelectData.CharacterType.RIFLER:

				weapon.m_damage = 40;
				weapon.m_distance = 50;
				weapon.m_Type = Weapons.WeaponType.RIFLE_UP;
				EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_FIRE, this, gameObject);

				weapon.m_skillEffect.gameObject.SetActive (true);
				yield return new WaitForSeconds (0.2f);
				weapon.m_SkillAudio.Play ();

				for (int i = 0; i < GameManager.Instance.LaserGun_bulletPool.Count; i++) {
					
					if (!GameManager.Instance.LaserGun_bulletPool [i].activeSelf &&
						GameManager.Instance.LaserGun_bulletPool [i] != null) { // 비활성화
						GameManager.Instance.LaserGun_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
						GameManager.Instance.LaserGun_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;

						GameManager.Instance.LaserGun_bulletPool [i].SetActive (true);
						break;
					}
				}

				yield return new WaitForSeconds(0.1f);
				weapon.m_damage = 10;
				weapon.m_distance = 15;

				weapon.m_Type = Weapons.WeaponType.RIFLE;
				break;

			case SelectData.CharacterType.YUTANER:
				
				weapon.m_damage = 70;
				weapon.m_distance = 50;

				weapon.m_Type = Weapons.WeaponType.YUTAN_UP;
				EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_FIRE, this, gameObject);

				weapon.m_skillEffect.gameObject.SetActive (true);
				yield return new WaitForSeconds(0.2f);
				weapon.m_SkillAudio.Play ();

				for (int i = 0; i < GameManager.Instance.PlasmaGun_bulletPool.Count; i++) {
					if (!GameManager.Instance.PlasmaGun_bulletPool [i].activeSelf &&
						GameManager.Instance.PlasmaGun_bulletPool [i] != null) { // 비활성화
						GameManager.Instance.PlasmaGun_bulletPool [i].GetComponent<Transform> ().position = weapon.m_firePos.position;
						GameManager.Instance.PlasmaGun_bulletPool [i].GetComponent<Transform> ().rotation = weapon.m_firePos.rotation;


						GameManager.Instance.PlasmaGun_bulletPool [i].SetActive (true);
						break;
					}
				}
			
				yield return new WaitForSeconds(0.1f);
				weapon.m_damage = 30;
				weapon.m_distance = 50;

				weapon.m_Type = Weapons.WeaponType.YUTAN;
				break;

			case SelectData.CharacterType.SAMURAI:
				
				weapon.m_damage = 0;
				weapon.m_distance = 20;
				weapon.m_speed = 100;
				weapon.m_Type = Weapons.WeaponType.SAMURAI_UP;

				EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_FIRE, this, gameObject);
				weapon.m_SkillAudio.Play ();
				m_animator.SetBool ("Idle", false);
				m_animator.SetBool ("Attack", true);
				weapon.m_skillEffect.gameObject.SetActive (true);
				yield return new WaitForSeconds(0.2f);
				weapon.m_weapon.GetComponent<AudioSource> ().Play ();
				yield return new WaitForSeconds(0.3f);
				m_animator.SetBool ("Idle", true);
				m_animator.SetBool ("Attack", false);

				weapon.m_damage = 20;
				weapon.m_distance = 1;
				weapon.m_speed = 10;
				weapon.m_Type = Weapons.WeaponType.SAMURAI;

				break;
			};

			yield return new WaitForSeconds(0.1f);
			weapon.m_skillEffect.gameObject.SetActive (false);

		}
			
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
		m_audioSource.clip = m_FootstepSounds[n];
		m_audioSource.PlayOneShot(m_audioSource.clip);
		// move picked sound to index 0 so it's not picked next time
		m_FootstepSounds[n] = m_FootstepSounds[0];
		m_FootstepSounds[0] = m_audioSource.clip;
	}

	private void UpdateWeaponPosition(float speed)
	{
		Vector3 newWeaponPosition;

		if (m_controller.velocity.magnitude > 0 && m_controller.isGrounded)
		{
			weapon.m_weapon.transform.localPosition =
				m_HeadBob.DoHeadBob(m_controller.velocity.magnitude +
					(speed));
			newWeaponPosition = weapon.m_weapon.transform.localPosition;
			newWeaponPosition.y = weapon.m_weapon.transform.localPosition.y; //- m_JumpBob.Offset();
		
		}

		else
		{
			newWeaponPosition = weapon.m_weapon.transform.localPosition;
			newWeaponPosition.y = m_OriginalWeaponPosition.y;// - m_JumpBob.Offset();
		}

		weapon.m_weapon.transform.localPosition = newWeaponPosition;
	}
		
	public void WeaponChange(GameObject col)
	{
		
		//weapon.m_weaponeName = col.GetComponent<WeaponItem> ().name;
		weapon.m_damage = col.GetComponent<WeaponItem> ().damage;
		weapon.m_weapon.SetActive (false);
		//weapon.m_weapon = gameObject.transform.FindChild (weapon.m_weaponeName).gameObject;
		weapon.m_weapon.SetActive (true);
		weapon.m_firePos = weapon.m_weapon.transform.FindChild ("FirePos").GetComponent<Transform>();
		//weapon.m_fireEffect = weapon.m_weapon.transform.FindChild (weapon.m_weaponeName).GetComponent<Transform>();
		col.gameObject.SetActive (false);
		m_HeadBob.Setup(weapon.m_weapon.transform, m_StepInterval);
		m_OriginalWeaponPosition = weapon.m_weapon.transform.localPosition;
	}

	public IEnumerator Wound(object Param)
	{
		yield return null;
		if (Param != null) {

			Player player = (Player)Param;
			if (m_name.Equals (player.Name) && player.Name != null) {
				m_shake.enabled = true;
				//m_ExplosionEffect.gameObject.SetActive (true);
				m_HP = player.Hp;
				m_imgHpbar.fillAmount = ((float)m_HP / (float)initHp);
				yield return new WaitForSeconds (0.3f);
				//m_ExplosionEffect.gameObject.SetActive (false);
				m_shake.enabled = false;
			}
		}
	}

	IEnumerator DieEvent(object Param){

		yield return null;

		if (Param != null) {

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {
				m_shake.isDie = true;
				m_shake.enabled = true;
				weapon.m_ExplosionEffect.gameObject.SetActive (true);
				isDie = true;
				m_skin.enabled = false;
				m_controller.enabled = false;
				yield return new WaitForSeconds (2f);
				weapon.m_ExplosionEffect.gameObject.SetActive (false);
				m_shake.enabled = false;
			}
		}
	}

	IEnumerator Recall(object Param){

		yield return null;
		if (Param != null) {

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {
					
				if (m_name.Equals (player.Name) && player.Name != null) {
					
					isRecall = true;
					horizontal = 0; 
					vertical = 0;
					m_PlayerTr.position = new Vector3 (player.Aim.Value.X, m_PlayerTr.position.y, player.Aim.Value.Z);
					EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_MOVE, this, gameObject);
					weapon.m_portalEffect.gameObject.SetActive (true);
					yield return new WaitForSeconds (3f);
					isRecall = false;
				}
			}
		}
	}

	public void Respawn(object Param)
	{
		
		if (Param != null) {

			Player player = (Player)Param;

			if (m_name.Equals (player.Name) && player.Name != null) {

				m_PlayerTr.position = new Vector3 (player.Pos.Value.X, player.Pos.Value.Y, player.Pos.Value.Z);
				m_PlayerTr.eulerAngles = new Vector3 (0f, player.Pos.Value.R, 0f);

			
				m_HP = 100;
				initHp = m_HP;
				m_imgHpbar.fillAmount = 1;
				m_controller.enabled = true;
				m_shake.isDie = false;
				m_skin.enabled = true;

				isDie = false;

				StopAllCoroutines ();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   

	
			}
		}
	}

	public IEnumerator Fire()
	{
		yield return null;

		if (!isDie) {

	
			EventManager.Instance.PostNotification (EVENT_TYPE.PLAYER_FIRE, this, gameObject);

			switch(m_Type){
			case SelectData.CharacterType.RIFLER:

				weapon.m_fireEffect.gameObject.SetActive (true);
				yield return new WaitForSeconds(0.1f);
				//weapon.m_weapon.GetComponent<AudioSource> ().Play ();
	
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

			case SelectData.CharacterType.YUTANER:

				weapon.m_fireEffect.gameObject.SetActive (true);
				yield return new WaitForSeconds(0.1f);
				//weapon.m_weapon.GetComponent<AudioSource> ().Play ();

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

			case SelectData.CharacterType.SAMURAI:

				m_animator.SetBool ("Idle", false);
				m_animator.SetBool ("Attack", true);
				//weapon.m_fireEffect.gameObject.SetActive (true);
				weapon.m_weapon.GetComponent<AudioSource> ().Play ();
				yield return new WaitForSeconds(0.3f);

				m_animator.SetBool ("Idle", true);
				m_animator.SetBool ("Attack", false);

				break;
			};
				
			yield return new WaitForSeconds(0.1f);
			weapon.m_fireEffect.gameObject.SetActive (false);
		}
	}
		

	public void Player(string Name, TeamState TeamNo, Vector3 Tr, float r)
	{
		
		m_name = Name;
		m_team = TeamNo;
		m_PlayerTr.name = Name;
		m_PlayerTr.position = Tr;
		m_PlayerTr.eulerAngles = new Vector3 (0f, r, 0f);
				
	}


	public void OnEvent(EVENT_TYPE Event_Type, Component Sender, object Param)
	{
		
		switch (Event_Type) {
		case EVENT_TYPE.R_PLAYER_RESPAWN:
			Respawn (Param);
			break;
		case EVENT_TYPE.R_PLAYER_DEAD:
			StartCoroutine ("DieEvent", Param);
			break;
		case EVENT_TYPE.R_PLAYER_WOUND:
			StartCoroutine ("Wound", Param);
			break;
		case EVENT_TYPE.R_PLAYER_RECALL:
			StartCoroutine ("Recall", Param);
			break;
		}
	}
}
