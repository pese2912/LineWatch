using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DProjectile : MonoBehaviour
    {
        public F3DFXType fxType; // Weapon type 
        public LayerMask layerMask;
        public float lifeTime = 5f; // Projectile life time
        public float despawnDelay; // Delay despawn in ms
        public float velocity = 300f; // Projectile velocity
        public float RaycastAdvance = 2f; // Raycast advance multiplier 
		public bool DelayDespawn = false; // Projectile despawn flag 
        public ParticleSystem[] delayedParticles; // Array of delayed particles
        ParticleSystem[] particles; // Array of projectile particles 
        new Transform transform; // Cached transform 
        RaycastHit hitPoint; // Raycast structure 
        bool isHit = false; // Projectile hit flag
        bool isFXSpawned = false; // Hit FX prefab spawned flag 
        float timer = 0f; // Projectile timer
        float fxOffset; // Offset of fxImpact


		public AudioSource audioSource;
        void Awake()
        {
			
            // Cache transform and get all particle systems attached
            transform = GetComponent<Transform>();
            particles = GetComponentsInChildren<ParticleSystem>();
        }

		void OnEnable()
		{
		
			if(audioSource != null)
				audioSource.Play ();
			isHit = false;
			isFXSpawned = false;
			timer = 0f;
			hitPoint = new RaycastHit();

		}
		void OnDisable()
		{
			if (hitPoint.rigidbody != null)
				hitPoint.rigidbody.velocity = Vector3.zero;
		}
        // OnSpawned called by pool manager 
        public void OnSpawned()
        {
            // Reset flags and raycast structure
            isHit = false;
            isFXSpawned = false;
            timer = 0f;
            hitPoint = new RaycastHit();
        }
		void Start()
		{
			
		}
        // OnDespawned called by pool manager 
        public void OnDespawned()
        {
			
        }

        // Stop attached particle systems emission and allow them to fade out before despawning
        void Delay()
        {
            if (particles.Length > 0 && delayedParticles.Length > 0)
            {
                bool delayed;
                for (int i = 0; i < particles.Length; i++)
                {
                    delayed = false;
                    for (int y = 0; y < delayedParticles.Length; y++)
                        if (particles[i] == delayedParticles[y])
                        {
                            delayed = true;
                            break;
                        }
                    particles[i].Stop(false);
                    if (!delayed)
                        particles[i].Clear(false);
                }
            }
        }

        // OnDespawned called by pool manager 
        void OnProjectileDestroy()
        {


           // F3DPoolManager.Pools["GeneratedPool"].Despawn(transform);
		//	Destroy(gameObject);
			gameObject.SetActive(false);
		}

        // Apply hit force on impact
        void ApplyForce(float force)
        {
            if (hitPoint.rigidbody != null)
                hitPoint.rigidbody.AddForceAtPosition(transform.forward*force, hitPoint.point, ForceMode.VelocityChange);
        }

        void Update()
        {
            // If something was hit
            if (isHit)
            {
                // Execute once
                if (!isFXSpawned)
                {
                    // Invoke corresponding method that spawns FX
                    switch (fxType)
                    {
                        case F3DFXType.Vulcan:
                            //F3DFXController.instance.VulcanImpact(hitPoint.point + hitPoint.normal*fxOffset);
                            ApplyForce(2.5f);
                            break;

                        case F3DFXType.SoloGun:
                            //F3DFXController.instance.SoloGunImpact(hitPoint.point + hitPoint.normal*fxOffset);
                            ApplyForce(25f);
                            break;

                        case F3DFXType.Seeker:
                           // F3DFXController.instance.SeekerImpact(hitPoint.point + hitPoint.normal*fxOffset);
                            ApplyForce(30f);
                            break;

                        case F3DFXType.PlasmaGun:
                           // F3DFXController.instance.PlasmaGunImpact(hitPoint.point + hitPoint.normal*fxOffset);
                            ApplyForce(25f);
                            break;

                        case F3DFXType.LaserImpulse:
                        //    F3DFXController.instance.LaserImpulseImpact(hitPoint.point + hitPoint.normal*fxOffset);
                            ApplyForce(25f);
                            break; 
                    }

                    isFXSpawned = true;
                }

                // Despawn current projectile 
                if (!DelayDespawn || (DelayDespawn && (timer >= despawnDelay)))
                    OnProjectileDestroy();
            }

            // No collision occurred yet
            else
            {
                // Projectile step per frame based on velocity and time
                Vector3 step = transform.forward*Time.deltaTime*velocity;

                // Raycast for targets with ray length based on frame step by ray cast advance multiplier
                if (Physics.Raycast(transform.position, transform.forward, out hitPoint, step.magnitude*RaycastAdvance,
                    layerMask))
                {
                    isHit = true;


					if (fxType == F3DFXType.Seeker) {
						
						for (int i = 0; i < GameManager.Instance.ShotGun_FlarePool.Count; i++) {
							if (!GameManager.Instance.ShotGun_FlarePool [i].activeSelf &&
								GameManager.Instance.ShotGun_FlarePool [i] != null) { // 비활성화
								GameManager.Instance.ShotGun_FlarePool [i].GetComponent<Transform> ().position = new Vector3(transform.position.x-0.5f,
									transform.position.y, transform.position.z); 
								GameManager.Instance.ShotGun_FlarePool [i].GetComponent<Transform> ().rotation = transform.rotation;


								GameManager.Instance.ShotGun_FlarePool [i].SetActive (true);
								break;
							}
						} 
					} else if (fxType == F3DFXType.LaserImpulse) {

						for (int i = 0; i < GameManager.Instance.LaserGun_FlarePool.Count; i++) {
							
							if (!GameManager.Instance.LaserGun_FlarePool [i].activeSelf &&
								GameManager.Instance.LaserGun_FlarePool [i] != null) { // 비활성화
								GameManager.Instance.LaserGun_FlarePool [i].GetComponent<Transform> ().position = new Vector3(transform.position.x-0.5f,
									transform.position.y, transform.position.z); 
								GameManager.Instance.LaserGun_FlarePool [i].GetComponent<Transform> ().rotation = transform.rotation;

								GameManager.Instance.LaserGun_FlarePool [i].SetActive (true);
								break;
							}
						} 
					}
					else if (fxType == F3DFXType.Vulcan) {

						for (int i = 0; i < GameManager.Instance.Yutan_FlarePool.Count; i++) {
							
							if (!GameManager.Instance.Yutan_FlarePool [i].activeSelf &&
								GameManager.Instance.Yutan_FlarePool [i] != null) { // 비활성화
								GameManager.Instance.Yutan_FlarePool [i].GetComponent<Transform> ().position = new Vector3(transform.position.x-0.2f,
									transform.position.y, transform.position.z); 
								GameManager.Instance.Yutan_FlarePool [i].GetComponent<Transform> ().rotation = transform.rotation;

								GameManager.Instance.Yutan_FlarePool [i].SetActive (true);


						

								break;
							}
						} 
					}

					else if (fxType == F3DFXType.PlasmaGun) {

						for (int i = 0; i < GameManager.Instance.PlasmaGun_FlarePool.Count; i++) {

							if (!GameManager.Instance.PlasmaGun_FlarePool [i].activeSelf &&
								GameManager.Instance.PlasmaGun_FlarePool [i] != null) { // 비활성화
								GameManager.Instance.PlasmaGun_FlarePool [i].GetComponent<Transform> ().position = new Vector3(transform.position.x-0.2f,
									transform.position.y, transform.position.z); 
								GameManager.Instance.PlasmaGun_FlarePool [i].GetComponent<Transform> ().rotation = transform.rotation;

								GameManager.Instance.PlasmaGun_FlarePool [i].SetActive (true);


								break;
							}
						} 
					}
                    // Invoke delay routine if required
                    if (DelayDespawn)
                    {
						
                        // Reset projectile timer and let particles systems stop emitting and fade out correctly
                        timer = 0f;
                        Delay();
                    }
                }
                // Nothing hit
                //else
                //{
                    // Projectile despawn after run out of time
				if (timer >= lifeTime) {
					OnProjectileDestroy ();
				}
                //}

                // Advances projectile forward
                transform.position += step;
            }

            // Updates projectile timer
            timer += Time.deltaTime;
        }

        //Set offset
        public void SetOffset(float offset)
        {
            fxOffset = offset;
        }
    }
}