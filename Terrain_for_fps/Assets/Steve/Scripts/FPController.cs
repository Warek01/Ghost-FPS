using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPController : MonoBehaviour {
   private static readonly int Walking = Animator.StringToHash("walking");
   private static readonly int Reload = Animator.StringToHash("reload");
   private static readonly int ARM = Animator.StringToHash("arm");
   private static readonly int Fire = Animator.StringToHash("fire");
   private static readonly int Dance = Animator.StringToHash("Dance");
   public GameObject cam;
   public GameObject stevePrefab;
   public GameObject weapon;
   public Slider healthbar;
   public TMP_Text ui_ammo;
   public TMP_Text ui_clip;
   public Animator anim; //public exposes object to inspector, drag and drop animation controller over there
   public Transform ShotDirection;
   public AudioSource[] footsteps;

   public AudioSource jump;
   public AudioSource land;
   public AudioSource ammopickup;
   public AudioSource medpickup;
   public AudioSource trigger;
   public AudioSource reload;
   public GameObject canvas;
   public GameObject gameOverPrefab;
   public GameObject LosePrefab;

   float xSensitivity = 2f;
   float ySensitivity = 2f;
   float MinimumX = -90f;
   float MaximumX = 90;
   float x;
   float y;

   int ammo = 10;
   int maxAmmo = 50;

   int health = 0;
   int maxHealth = 100;

   int ammoClip = 10;
   int ammoClipMax = 10;

   float defaultSpeed = 5f;
   float runningSpeed = 11f;
   float currentSpeed;
   bool isRunning;

   Rigidbody rb;
   CapsuleCollider capsule;

   Quaternion cameraRot;
   Quaternion charachterRot;


   void Start() {
      rb = GetComponent<Rigidbody>();
      capsule = GetComponent<CapsuleCollider>();

      cameraRot = cam.transform.localRotation;
      charachterRot = transform.localRotation; //take rotations

      health = maxHealth;
      healthbar.value = health;
      ui_ammo.text = ammo.ToString();
      ui_clip.text = ammoClip.ToString();
      currentSpeed = defaultSpeed;
      isRunning = false;
      //weapon.gameObject.SetActive(false);
   }

   void Update() {
      float yRot = Input.GetAxis("Mouse X") * xSensitivity; //when mouse moves left to right   around y axis
      float xRot = Input.GetAxis("Mouse Y") * ySensitivity; //when mouse moves up and down around x axis

      cameraRot *= Quaternion.Euler(-xRot, 0, 0); //update camera rot
      charachterRot *= Quaternion.Euler(0, yRot, 0);

      transform.localRotation = charachterRot; //update actual position using camera rot
      cam.transform.localRotation = cameraRot;

      if (Input.GetKeyDown(KeyCode.Space) && isGrounded()) {
         rb.AddForce(0, 300, 0);
         jump.Play();
         land.Play(); //this doesn't sound realistic enough
      }

      //input code should stay in update method, not fixedupdate
      float x = Input.GetAxis("Horizontal") * currentSpeed * Time.deltaTime;
      float z = Input.GetAxis("Vertical") * currentSpeed * Time.deltaTime;
      //new Vector3(x * speed, 0, z * speed);
      transform.position += cam.transform.forward * z + cam.transform.right * x;


      if (Input.GetKeyDown(KeyCode.F)) {
         anim.SetBool(ARM, !anim.GetBool(ARM));
      }

      if (Input.GetMouseButtonDown(0)) {
         if (ammoClip > 0) {
            anim.SetTrigger(Fire);
            ammoClip--;
            ui_clip.text = ammoClip.ToString();
            ProcessZombieHit();
         }
         else if (anim.GetBool(ARM)) {
            trigger.Play();
         }
      }

      if (Input.GetKey(KeyCode.LeftShift)) {
         //run
         currentSpeed = runningSpeed;
         isRunning = true;
      }
      else {
         currentSpeed = defaultSpeed;
         isRunning = false;
      }

      if (Input.GetKeyDown(KeyCode.R)) {
         if (ammo > 0) {
            int ammoNeeded = ammoClipMax - ammoClip;
            ammo = Mathf.Clamp(ammo - ammoNeeded, 0, maxAmmo);
            ui_ammo.text = ammo.ToString();
            anim.SetTrigger(Reload);
            ammoClip = Mathf.Clamp(ammoClip + ammoNeeded, 0, ammoClipMax);
            ui_clip.text = ammoClip.ToString();
            reload.Play();
         }
      }

      if (Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0) {
         if (!anim.GetBool(Walking)) {
            anim.SetBool(Walking, true);
            InvokeRepeating(nameof(PlayFootstepsAudio), 0f, isRunning ? 0.25f : 0.4f);
            //PlayFootstepsAudio();   //please make invoking work
         }

         anim.SetBool("walking", true);
      }
      else if (anim.GetBool(Walking)) {
         anim.SetBool(Walking, false);
         CancelInvoke(nameof(PlayFootstepsAudio)); //fix this
      }
   }

   void OnCollisionEnter(Collision col) {
      if (col.gameObject.CompareTag("Ammo") && ammo < maxAmmo) {
         //ammo  pickup
         ammo = Mathf.Clamp(ammo + 10, 0, maxAmmo); //checks or makes  it so that if ammo>maxammo cant update anymore
         ui_ammo.text = ammo.ToString();
         Destroy(col.gameObject);
         ammopickup.Play();
      }


      if (col.gameObject.CompareTag("MedBox") && health < maxHealth) {
         //med health pickup
         health = Mathf.Clamp(health + 20, 0, maxHealth);
         healthbar.value = health;
         Destroy(col.gameObject);
         medpickup.Play();
      }

      if (col.gameObject.CompareTag("Home")) {
         Vector3 pos = new Vector3(transform.position.x,
            Terrain.activeTerrain.SampleHeight(transform.position), transform.position.z);
         GameObject
            steve = Instantiate(stevePrefab, pos, transform.rotation); //add 3rd person anim model for death scene
         steve.GetComponent<Animator>().SetTrigger(Dance);
         GameStats.gameOver = true; //this has to happen BEFORE we destroy this.gameObject!!!
         Destroy(gameObject);
         GameObject gameOverText = Instantiate(gameOverPrefab, canvas.transform, true);
      }
   }

   void PlayFootstepsAudio() {
      AudioSource audioSource = new AudioSource();
      int n = Random.Range(0, footsteps.Length - 1);

      audioSource = footsteps[n];
      audioSource.Play();
      footsteps[n] = footsteps[0];
      footsteps[0] = audioSource;
   }

   Quaternion ClampRotationAroundXAxis(Quaternion q) {
      q.x /= q.w;
      q.y /= q.w;
      q.z /= q.w;
      q.w = 1.0f;

      float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
      angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);
      q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

      return q;
   }

   bool isGrounded() {
      return Physics.SphereCast(
         transform.position,
         capsule.radius,
         Vector3.down,
         out _,
         (capsule.height / 2f) - capsule.radius + 0.1f
      );
   }

   void ProcessZombieHit() {
      RaycastHit hitinfo;
      if (Physics.Raycast(ShotDirection.position, ShotDirection.forward, out hitinfo, 200)) {
         GameObject hitZombie = hitinfo.collider.gameObject;
         if (hitZombie.tag == "Zombie") {
            if (Random.Range(0, 10) < 5) {
               GameObject rdPrefab = hitZombie.GetComponent<ZombieController>().ragdoll;
               GameObject newRD = Instantiate(rdPrefab, hitZombie.transform.position, hitZombie.transform.rotation);
               newRD.transform.Find("Hips").GetComponent<Rigidbody>().AddForce(ShotDirection.forward * 10000);
               Destroy(hitZombie);
            }
            else {
               hitZombie.GetComponent<ZombieController>().KillZombie();
               //dewde
            }
         }
      }
   }

   //Take hit/ die

   public void TakeHit(float amount) {
      //add function as an event into the animator at a given attack moment
      health = (int)Mathf.Clamp(health - amount, 0, maxHealth);
      healthbar.value = health;
      if (health == 0) {
         Vector3 pos = new Vector3(this.transform.position.x,
            Terrain.activeTerrain.SampleHeight(this.transform.position), this.transform.position.z);
         GameObject
            steve = Instantiate(stevePrefab, pos, this.transform.rotation); //add 3rd person anim model for death scene
         steve.GetComponent<Animator>().SetTrigger("Death");
         GameStats.gameOver = true; //this has to happen BEFORE we destroy this.gameObject!!!
         Destroy(this.gameObject);
         GameObject LoseText = Instantiate(LosePrefab);
         LoseText.transform.SetParent(canvas.transform);

         //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
      }
   }

   //Victory
}
