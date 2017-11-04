using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
	MovementController moveCont;
	Camera playerCamera;

	void Awake() {
		Cursor.lockState = CursorLockMode.Locked;
	}


	void Start () {
		moveCont = GetComponent<MovementController> ();
		playerCamera = GetComponentInChildren<Camera> ();
	}
	

	void Update () {
		
		ProcessInput ();

	}

	void ProcessInput() {
		
		float horizontalInput = Input.GetAxis ("Horizontal");
		moveCont.AddMovementInput (new Vector3(horizontalInput,0,0));

		float verticalInput = Input.GetAxis ("Vertical");
		moveCont.AddMovementInput (new Vector3(0,0,verticalInput));


		float roll = - Input.GetAxis ("Mouse Y");
		playerCamera.transform.rotation = playerCamera.transform.rotation * Quaternion.Euler (roll,0,0);

		float pitch = Input.GetAxis("Mouse X");
		moveCont.AddRotationInput (Quaternion.Euler(0,pitch,0));


	}
}
