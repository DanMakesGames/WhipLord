using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;

public class TriggerPasser : MonoBehaviour {
	// Destination SateBox. The destination of the Trigger event pass.
	private StateBox destinationStateBox;
	public StateBox DestinationStateBox { get; set; }
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}



	void OnTriggerEnter(Collider hit) {
		destinationStateBox.OnTrigger (hit);
	}
}
