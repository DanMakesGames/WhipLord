using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;

public class TriggerPasser : MonoBehaviour {
	// Destination SateBox. The destination of the Trigger event pass.
	public bool visualDebug;
	private StateBox destinationStateBox;
	public StateBox DestinationStateBox { 

		get{ return destinationStateBox; } 
		set{ destinationStateBox = value; } 
	
	}

	void Awake () {
		BoxCollider collider = GetComponent<BoxCollider> ();

	}

	void OnTriggerEnter(Collider hit) {
		//Debug.Log ("logey");
		AudioSource hitSound = GetComponent<AudioSource>();
		if (hitSound != null) {
			hitSound.Play ();
		}

		destinationStateBox.OnTrigger (hit);
	}
}
