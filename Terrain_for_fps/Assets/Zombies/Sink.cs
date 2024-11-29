using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sink : MonoBehaviour
{
    float destroyHeight;
    // Start is called before the first frame update

    void Start(){
        if(this.gameObject.tag == "Ragdoll")
            Invoke("StartSink",5);   //Don't ask

    }
    public void StartSink()
    {
        destroyHeight = Terrain.activeTerrain.SampleHeight(this.transform.position)-5;
        Collider [] colList = this.transform.GetComponentsInChildren<Collider>();    //list of all zombie colliders
        foreach(Collider c in colList){         //loop through colList to destroy colliders before sinking
            Destroy(c);
        }

        InvokeRepeating("SinkIntoGround",10,0.1f);
    }

    // Update is called once per frame
    void SinkIntoGround()
    {
        this.transform.Translate(0,-0.001f,0);
        if(this.transform.position.y<destroyHeight){
            Destroy(this.gameObject);
        }
        
    }
}
