using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class ZombieController : MonoBehaviour
{
    public GameObject target;
    public GameObject ragdoll;
    public float WalkingSpeed;
    public float RunningSpeed;
    public float damageAmount = 5;
    public AudioSource[] splats;

    Animator anim;
    NavMeshAgent agent;

    enum STATE {IDLE, WANDER, ATTACK, CHASE, DEAD};
    STATE state = STATE.IDLE;

    void turnTriggersOff(){
        anim.SetBool("isWalking",false);
        anim.SetBool("isAttacking",false);
        anim.SetBool("isRunning",false);
        anim.SetBool("isDead",false);
        

    }

    float DistanceToPlayer(){
        if(GameStats.gameOver) return 1000f;
        return Vector3.Distance(target.transform.position,this.transform.position);
    }

    bool CanSeePlayer(){
        if(DistanceToPlayer()< 10)
            return true;
        return false;
    }


    bool ForgetPlayer(){
        if(DistanceToPlayer()> 20)
            return true;
        return false;
    }

    public void KillZombie(){
        turnTriggersOff();
        anim.SetBool("isDead", true);
        state = STATE.DEAD;
    }

    void Start()
    {
        anim = this.GetComponent<Animator>();
        agent = this.GetComponent<NavMeshAgent>();
        anim.SetBool("isWalking", true);

        
    }

    void Update()
    {
        if(target == null && !GameStats.gameOver){
            target = GameObject.FindWithTag("Player");
            return;
        }
        switch (state){         //this is an animation state machine done using a basic switch, where each state states when and how another state is triggered
            case STATE.IDLE:
                if (CanSeePlayer()) state = STATE.CHASE;
                else
                    state = STATE.WANDER;
                break;
            case STATE.WANDER:
                if(!agent.hasPath){
                    float newX = this.transform.position.x+Random.Range(-5,5);
                    float newZ = this.transform.position.z+Random.Range(-5,5);
                    float newY = Terrain.activeTerrain.SampleHeight(new Vector3(newX, 0, newZ));
                    Vector3 dest = new Vector3(newX, newY, newZ);
                    agent.SetDestination(dest);
                    agent.stoppingDistance = 0;
                    turnTriggersOff();
                    anim.SetBool("isWalking", true);
                   
                }
                if (CanSeePlayer() ) state = STATE.CHASE;
                break;
            case STATE.CHASE:
                if(GameStats.gameOver) {turnTriggersOff(); state = STATE.WANDER; return;}
                agent.SetDestination(target.transform.position);
                agent.stoppingDistance=2;
                turnTriggersOff();
                anim.SetBool("isRunning", true);

                if(agent.remainingDistance<= agent.stoppingDistance && !agent.pathPending ){
                    state = STATE.ATTACK;
                }
                break;
            case STATE.ATTACK:
                if(GameStats.gameOver) {turnTriggersOff(); state = STATE.WANDER; return;}
                turnTriggersOff();
                anim.SetBool("isAttacking", true);
                this.transform.LookAt(target.transform.position);
                if(DistanceToPlayer()>agent.stoppingDistance + 3){ //prevent animation glitch
                    state = STATE.CHASE;
                }
                if(ForgetPlayer()) {
                    state = STATE.WANDER;
                    agent.ResetPath();
                }
                break;
            case STATE.DEAD:
                Destroy(agent);
                this.GetComponent<Sink>().StartSink();
                break;

        }
        
    }
     void PlaySplatsAudio(){
            AudioSource audioSource = new AudioSource();
            int n = Random.Range(0, splats.Length-1);

            audioSource = splats[n];
            audioSource.Play();
            splats[n] = splats[0];
            splats[0] = audioSource;

        }

    public void DamagePlayer(){

        if(target != null){    //prevent zombies trying to access steve after destroying his gameobject
            target.GetComponent<FPController>().TakeHit(damageAmount);
            PlaySplatsAudio();
        }

    }
}
