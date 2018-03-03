using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectController : MonoBehaviour {

    AudioSource audio;
    public GameObject parent;

	// Use this for initialization
	void Start () {

        audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void onTap() {
        Debug.Log("Tappppp!");
        parent.GetComponent<PlaneManager>().Reset();
    }
}
