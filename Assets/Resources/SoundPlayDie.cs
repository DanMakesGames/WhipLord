using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayDie : MonoBehaviour {

	// Use this for initialization
	void Start () {
		AudioSource audioSource = GetComponent<AudioSource> ();
		audioSource.Play ();
	
		Destroy (gameObject, audioSource.clip.length);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
