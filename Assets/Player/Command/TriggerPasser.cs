using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;

public class TriggerPasser : MonoBehaviour {
	// Destination SateBox. The destination of the Trigger event pass.
	private StateBox destinationStateBox;
	public StateBox DestinationStateBox { 

		get{ return destinationStateBox; } 
		set{ destinationStateBox = value; } 
	
	}
		
	void OnTriggerEnter(Collider hit) {
		//Debug.Log ("logey");
		destinationStateBox.OnTrigger (hit);
	}
}
