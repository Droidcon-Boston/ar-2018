using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectController : MonoBehaviour {

    AudioSource audio;

	// Use this for initialization
	void Start () {

        audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void onHold() {
        Debug.Log("Holddddd!");
        audio.Play();

    }
}
