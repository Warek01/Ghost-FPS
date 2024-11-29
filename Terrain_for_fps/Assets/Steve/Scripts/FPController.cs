using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FPController : MonoBehaviour
{
    public GameObject cam; 
    public GameObject stevePrefab;
    public GameObject weapon;
    public Slider healthbar;
    public TMPro.TMP_Text ui_ammo;
    public TMPro.TMP_Text ui_clip;
    public Animator anim;   //public exposes object to inspector, drag and drop animation controller over there
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

    int ammoClip= 10;
    int ammoClipMax = 10;

    float defaultSpeed = 5f;
    float runningSpeed = 11f;
    float currentSpeed;
    bool isRunning;

    Rigidbody rb;
    CapsuleCollider capsule;

    Quaternion cameraRot;
    Quaternion charachterRot;


    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        capsule=this.GetComponent<CapsuleCollider>();

        cameraRot=cam.transform.localRotation;
        charachterRot=this.transform.localRotation;   //take rotations

        health=maxHealth;
        healthbar.value=health;
        ui_ammo.text = ammo.ToString();
        ui_clip.text = ammoClip.ToString();
        currentSpeed=defaultSpeed;
        isRunning=false;
        //weapon.gameObject.SetActive(false);
        
        
    }

    // Update is called once per frame
    void Update()
    {
        float yRot= Input.GetAxis("Mouse X")*xSensitivity;   //when mouse moves left to right   around y axis
        float xRot=Input.GetAxis("Mouse Y")*ySensitivity;   //when mouse moves up and down around x axis

        cameraRot *= Quaternion.Euler(-xRot, 0, 0);     //update camera rot
        charachterRot *= Quaternion.Euler(0,yRot,0);

        this.transform.localRotation=charachterRot;     //update actual position using camera rot
        cam.transform.localRotation=cameraRot;

        if(Input.GetKeyDown(KeyCode.Space) && isGrounded()){
            rb.AddForce(0,300,0);
            jump.Play();
            land.Play();    //this doesn't sound realistic enough


        }
        
        float x = Input.GetAxis("Horizontal")*currentSpeed*Time.deltaTime;   //input code should stay in update method, not fixedupdate
        float z = Input.GetAxis("Vertical")*currentSpeed*Time.deltaTime;
        transform.position += cam.transform.forward * z + cam.transform.right * x; //new Vector3(x * speed, 0, z * speed);


        if(Input.GetKeyDown(KeyCode.F)){
            anim.SetBool("arm", !anim.GetBool("arm"));
        }
        if(Input.GetMouseButtonDown(0)){
            if(ammoClip>0){
                anim.SetTrigger("fire");
                ammoClip--;
                ui_clip.text = ammoClip.ToString();
                ProcessZombieHit();
            }else if(anim.GetBool("arm")){
                trigger.Play();    

            }
            

            
            //shot.Play();
        }
        if(Input.GetKey(KeyCode.LeftShift)){ //run
            currentSpeed=runningSpeed;
            isRunning=true;
        }else{
            currentSpeed=defaultSpeed;
            isRunning=false;
        }

        if(Input.GetKeyDown(KeyCode.R)){

            if(ammo>0){
                int ammoNeeded = ammoClipMax-ammoClip;
                ammo = Mathf.Clamp(ammo-ammoNeeded,0,maxAmmo);
                ui_ammo.text = ammo.ToString();
                anim.SetTrigger("reload");
                ammoClip=Mathf.Clamp(ammoClip+ammoNeeded,0,ammoClipMax);
                ui_clip.text = ammoClip.ToString();
                reload.Play();
            }
        }

        if(Mathf.Abs(x)>0||Mathf.Abs(z)>0){
            if(!anim.GetBool("walking"))    {
                anim.SetBool("walking", true);
                if (isRunning)
                {
                    InvokeRepeating("PlayFootstepsAudio", 0f, 0.25f);
                }else{
                    InvokeRepeating("PlayFootstepsAudio", 0f, 0.4f);
                }      
                //PlayFootstepsAudio();   //please make invoking work
            }
            anim.SetBool("walking", true);

        } else if(anim.GetBool("walking")){
            anim.SetBool("walking", false);
            CancelInvoke("PlayFootstepsAudio");          //fix this
        }



    }

        void OnCollisionEnter(Collision col) {
            if(col.gameObject.tag=="Ammo" && ammo<maxAmmo){        //ammo  pickup
                ammo=Mathf.Clamp(ammo+10,0,maxAmmo);//checks or makes  it so that if ammo>maxammo cant update anymore
                ui_ammo.text = ammo.ToString();
                Destroy(col.gameObject);
                ammopickup.Play();
                }



            if(col.gameObject.tag=="MedBox" && health<maxHealth){                   //med health pickup
                health = Mathf.Clamp(health+20,0,maxHealth);
                healthbar.value=health;
                Destroy(col.gameObject);
                medpickup.Play();
                }

            //if(col.gameObject.tag=="Lava"){
            //    health=Mathf.Clamp(health-50,0,maxHealth);
            //    Debug.Log("Lava, health is " + health);
            //}

            if(col.gameObject.tag == "Home"){
                Vector3 pos = new Vector3(this.transform.position.x, Terrain.activeTerrain.SampleHeight(this.transform.position), this.transform.position.z);
                GameObject steve = Instantiate(stevePrefab,pos, this.transform.rotation);            //add 3rd person anim model for death scene
                steve.GetComponent<Animator>().SetTrigger("Dance");
                GameStats.gameOver = true;     //this has to happen BEFORE we destroy this.gameObject!!!
                Destroy(this.gameObject);
                GameObject gameOverText = Instantiate(gameOverPrefab);
                gameOverText.transform.SetParent(canvas.transform);

        }

            
        }

        void PlayFootstepsAudio(){
            AudioSource audioSource = new AudioSource();
            int n = Random.Range(0, footsteps.Length-1);

            audioSource = footsteps[n];
            audioSource.Play();
            footsteps[n] = footsteps[0];
            footsteps[0] = audioSource;

        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }

    bool isGrounded(){
        RaycastHit hitinfo;
        if(Physics.SphereCast(transform.position,capsule.radius,Vector3.down,out hitinfo,(capsule.height/2f)-capsule.radius+0.1f)){   //spherecast detects whether a collison has occured,in this case the "sphere" and the ground
            return true;
        }
        return false;
    }
    void ProcessZombieHit(){
        RaycastHit hitinfo;
        if (Physics.Raycast(ShotDirection.position, ShotDirection.forward, out hitinfo, 200)){
            GameObject hitZombie = hitinfo.collider.gameObject;
            if(hitZombie.tag == "Zombie"){
                if(Random.Range(0,10)<5){
                    GameObject rdPrefab = hitZombie.GetComponent<ZombieController>().ragdoll;
                    GameObject newRD = Instantiate(rdPrefab, hitZombie.transform.position, hitZombie.transform.rotation);
                    newRD.transform.Find("Hips").GetComponent<Rigidbody>().AddForce(ShotDirection.forward*10000);
                    Destroy(hitZombie);
                }else{
                    hitZombie.GetComponent<ZombieController>().KillZombie();
                    //dewde
                }
            }
        }

    }

    //Take hit/ die

    public void TakeHit(float amount){    //add function as an event into the animator at a given attack moment
        health = (int) Mathf.Clamp(health-amount, 0, maxHealth);
        healthbar.value=health;
        if(health == 0){
            Vector3 pos = new Vector3(this.transform.position.x, Terrain.activeTerrain.SampleHeight(this.transform.position), this.transform.position.z);
            GameObject steve = Instantiate(stevePrefab,pos, this.transform.rotation);            //add 3rd person anim model for death scene
            steve.GetComponent<Animator>().SetTrigger("Death");
            GameStats.gameOver = true;     //this has to happen BEFORE we destroy this.gameObject!!!
            Destroy(this.gameObject);
            GameObject LoseText = Instantiate(LosePrefab);
            LoseText.transform.SetParent(canvas.transform);
            
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

    }

    //Victory

    


}
