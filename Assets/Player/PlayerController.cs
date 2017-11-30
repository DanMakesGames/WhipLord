using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;

public class PlayerController : CommandController {
	

	//MovementController moveCont;
	Camera playerCamera;
	Animator animator;
	//Command[] playerCommands;
	private bool bPlayerWin = false;
	private bool bEnemyWin = false;
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

		animator = GetComponent<Animator> ();
	}
	

	void OnGUI () {
		
		if(bPlayerWin || bEnemyWin) {
			Debug.Log ("printing");
			Texture splashTextrue = null;
			if (bPlayerWin)
				splashTextrue = (Texture)Resources.Load ("WinSplash");
			if (bEnemyWin)
				splashTextrue = (Texture)Resources.Load ("LoseSplash");
			Rect splashRect = new Rect (Screen.width / 2 - (splashTextrue.width / 2), (Screen.height / 2) - (splashTextrue.height / 2),splashTextrue.width,splashTextrue.height);

			GUI.DrawTexture (splashRect, splashTextrue);
		}
	}

	void FixedUpdate() {
		/*
		if(getHealth() <= 0)
		{
			Application.Quit ();
			//Destroy (gameObject);
		}
		*/


		ProcessInput ();
		ProcessCommand ();
		ProcessState ();
	
	}

	void ProcessInput() {
		
		float attackInput = Input.GetAxis ("Attack");
		if(attackInput == 1) {
			Command newCommand = new LongPoke ();
			if (newCommand.Initialize (this)) {
				newCommand.OnStart ();
				CurrentCmd = newCommand;
				animator.SetTrigger ("Shoot");
			}
		}



		if (Input.GetKey (KeyCode.Space)) {
			if (State == CHAR_STATE.NEUTRAL) {
				ChangeState (CHAR_STATE.BLOCKING);
			}
		}
		else {
			if (State == CHAR_STATE.BLOCKING) {
				ChangeState (CHAR_STATE.NEUTRAL);
			}
		}

		//if (Input.GetKeyUp (KeyCode.Space)) {
			
		//}

		if(CanMove) {
			float horizontalInput = Input.GetAxis ("Horizontal");
			MoveCont.AddMovementInput (new Vector3(horizontalInput,0,0));

			float verticalInput = Input.GetAxis ("Vertical");
			MoveCont.AddMovementInput (new Vector3(0,0,verticalInput));

			float roll = - Input.GetAxis ("Mouse Y");
			playerCamera.transform.rotation = playerCamera.transform.rotation * Quaternion.Euler (roll,0,0);

			float pitch = Input.GetAxis("Mouse X");
			MoveCont.AddRotationInput (Quaternion.Euler(0,pitch,0));
		}

		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}
	}
		
	public override void Hurt (float damage, Vector3 impact)
	{
		base.Hurt (damage, impact);
		Debug.DrawRay (transform.position, Vector3.up, Color.green);
		if (State == CHAR_STATE.NEUTRAL || State == CHAR_STATE.HIT_STUN)
			setHealth (getHealth() - damage);
	}

	public void PlayerWin() {
		bPlayerWin = true;
	}

	public void EnemyWin() {
		bEnemyWin = true;
	}




}
