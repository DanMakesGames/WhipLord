using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	CommandController playerController;
	CommandController enemyController;

	// Use this for initialization
	void Start () {
		playerController = GameObject.FindGameObjectWithTag ("Player").GetComponent<CommandController> ();
		enemyController = GameObject.FindGameObjectWithTag ("Enemy").GetComponent<CommandController> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (playerController.getHealth () <= 0) {
			
		} else if (enemyController.getHealth () <= 0) {
		
		}
	}

	//make end game function on another thread.
}
