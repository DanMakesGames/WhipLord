﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;

public class PlayerController : CommandController {
	//MovementController moveCont;
	Camera playerCamera;
	//Command[] playerCommands;

	void Awake() {
		Cursor.lockState = CursorLockMode.Locked;
	}


	void Start () {
		MoveCont = GetComponent<MovementController> ();
		playerCamera = GetComponentInChildren<Camera> ();
		MoveCont.CastLayerMask = 1 << 9;
		MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 10;
		MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 11;
		MoveCont.CastLayerMask = ~ MoveCont.CastLayerMask;
	}
	

	void Update () {
		//Debug.Log ("Update: " + Time.deltaTime);

	}

	void FixedUpdate() {
		//Debug.Log ("Fixed Update: " + Time.deltaTime);
		ProcessInput ();
		ProcessCommand ();
	}

	void ProcessInput() {
		
		float attackInput = Input.GetAxis ("Attack");
		if(attackInput == 1) {
			Command newCommand = new WhipCommand ();
			if (newCommand.Initialize (this)) {
				CurrentCmd = newCommand;
			}
		}

		if(Input.GetAxis("Alt Fire") == 1) {
			Command newCommand = new KickCommand ();
			if (newCommand.Initialize (this)) {
				CurrentCmd = newCommand;
			}
		}
		float horizontalInput = Input.GetAxis ("Horizontal");
		MoveCont.AddMovementInput (new Vector3(horizontalInput,0,0));

		float verticalInput = Input.GetAxis ("Vertical");
		MoveCont.AddMovementInput (new Vector3(0,0,verticalInput));

		float roll = - Input.GetAxis ("Mouse Y");
		playerCamera.transform.rotation = playerCamera.transform.rotation * Quaternion.Euler (roll,0,0);

		float pitch = Input.GetAxis("Mouse X");
		MoveCont.AddRotationInput (Quaternion.Euler(0,pitch,0));

	}


}
