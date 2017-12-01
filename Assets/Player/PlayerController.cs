/**
 * Written by Daniel Mann.
 * created in 2017
 * DanielMannGames@outlook.com
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;


/**
 * Player controller is the class used by the player in this game. It handles all the player input.
 */
public class PlayerController : CommandController {
	


	Camera playerCamera;
	Animator animator;

	// Used for displaying win message. Set when the game ends.
	private bool bPlayerWin = false;
	private bool bEnemyWin = false;


	void Awake() {
		Cursor.lockState = CursorLockMode.Locked;
	}


	void Start () {

		// Set up component references.
		MoveCont = GetComponent<MovementController> ();
		playerCamera = GetComponentInChildren<Camera> ();

		// Set up collision masks for the move controller. Nocollision 
		MoveCont.CastLayerMask = 1 << 8;
		MoveCont.CastLayerMask = MoveCont.CastLayerMask | (1 << 7);
		MoveCont.CastLayerMask = MoveCont.CastLayerMask | (1 << 9);
		MoveCont.CastLayerMask = MoveCont.CastLayerMask | (1 << 10);
		MoveCont.CastLayerMask = ~ MoveCont.CastLayerMask;

		// get animator reference.
		animator = GetComponent<Animator> ();
	}


	// This is used only to draw the Win and Lose messages.
	void OnGUI () {
		
		if(bPlayerWin || bEnemyWin) {
			
			Texture splashTextrue = null;
			if (bPlayerWin)
				splashTextrue = (Texture)Resources.Load ("WinSplash");
			if (bEnemyWin)
				splashTextrue = (Texture)Resources.Load ("LoseSplash");
			Rect splashRect = new Rect (Screen.width / 2 - (splashTextrue.width / 2), (Screen.height / 2) - (splashTextrue.height / 2),splashTextrue.width,splashTextrue.height);

			GUI.DrawTexture (splashRect, splashTextrue);
		}
	}

	// Main update. 
	void FixedUpdate() {
		
		ProcessInput ();
		ProcessCommand ();
		ProcessState ();
	}


	/**
	 * Handles player input.
	 */
	void ProcessInput() {
		
		// IF attack input is read, start the LongPoke Command.
		float attackInput = Input.GetAxis ("Attack");

		if(attackInput == 1) {
			Command newCommand = new LongPoke ();
			// If we can start command, then do so.
			if (newCommand.Initialize (this)) {
				newCommand.OnStart ();

				// Set the current command to this new command.
				CurrentCmd = newCommand;
			}
		}

		// Handle player positions and rotation input.
		if(CanMove) {
			float horizontalInput = Input.GetAxis ("Horizontal");
			MoveCont.AddMovementInput (new Vector3(horizontalInput,0,0));

			float verticalInput = Input.GetAxis ("Vertical");
			MoveCont.AddMovementInput (new Vector3(0,0,verticalInput));

			float pitch = Input.GetAxis("Mouse X");
			MoveCont.AddRotationInput (Quaternion.Euler(0,pitch,0));
		}

		// End the game
		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}
	}


	public override void Hurt (float damage, Vector3 impact)
	{
		base.Hurt (damage, impact);

		// Only accept damage if the player is not blocking. This is not really needed since I removed blocking from the game.
		if (State == CHAR_STATE.NEUTRAL || State == CHAR_STATE.HIT_STUN)
			setHealth (getHealth() - damage);
	}


	// If this is called the player win message will be displayed
	public void PlayerWin() {
		bPlayerWin = true;
	}

	// If this is called the player lose message will be displayed
	public void EnemyWin() {
		bEnemyWin = true;
	}




}
