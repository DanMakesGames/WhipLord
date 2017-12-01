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
 * This class contains all the necessary info in order to use moves with it. Priamrily this needs to exist so that it 
 * can be assumed that this Character operates under fighing game esque rules:
 * They perform command moves
 * They have command move related player states like BLOCKING, HIT_HITSTUNNED, GROUNDED, KNOCKED_DOWN
 * 
 * This class implements blocking, block stun, and hit stun.
 * 
 * This defeinition is at the whim of what CommandMoves Need.
 */
public class CommandController : PawnController
{
	// Health value that 
	float health = 100;

	public float getHealth(){
		return health;
	}
	public void setHealth(float inHealth) {
		health = inHealth;
	}


	private Command currentCmd;
	public string getCurrentCommand() {
		if (currentCmd != null)
			return currentCmd.commandID;
		else
			return null;
	}

	public enum CHAR_STATE
	{
		HIT_STUN, 	// recovering from being hit.
		BLOCK_STUN, // recovering from blocking
		BLOCKING, 	// currently blocking
		NEUTRAL		// default normal state
	}

	CHAR_STATE state = CHAR_STATE.NEUTRAL;
	public CHAR_STATE State {
		get {return state;}
		set {state = value;}
	}

	// Number of frames the character has been in the state.
	private int stateFrame = 0;
	public int StateFrame {
		get { return stateFrame; }
		set { stateFrame = value; }
	}


	/**
	 * Changes state unconditionally.
	 */
	public void ChangeState(CHAR_STATE newState) {
		if (state == newState)
			return;

		stateFrame = 0;
		state = newState;

		switch (newState) {

		case CHAR_STATE.HIT_STUN:
			OnStartHitStun ();
			return;

		case CHAR_STATE.BLOCK_STUN:
			OnStartBlockStun ();
			return;

		case CHAR_STATE.BLOCKING:
			OnStartBlocking ();
			return;

		case CHAR_STATE.NEUTRAL:
			OnStartNeutral ();
			return;
		}
		
	}

	protected virtual void OnStartHitStun() {
		bCanMove = false;
	}
	protected virtual void OnStartBlockStun() {}
	protected virtual void OnStartBlocking() {
		bCanMove = false;
	}
	protected virtual void OnStartNeutral() {
		bCanMove = true;
	}


	protected void ProcessState() {

		switch (state) {
		case CHAR_STATE.BLOCKING:
			TickBlocking ();
			stateFrame++;
			return;

		case CHAR_STATE.BLOCK_STUN:
			TickBlockStun ();
			stateFrame++;
			return;

		case CHAR_STATE.HIT_STUN:
			TickHitStun ();
			stateFrame++;
			return;

		case CHAR_STATE.NEUTRAL:
			TickNeutral ();
			stateFrame++;
			return;
		}


	}




	// If true the player can move. If false the palyer cannot
	private bool bCanMove = true;
	public bool CanMove {
		get {return bCanMove;}
		set { bCanMove = value;}
	}

	/* on being set, it terminates any currently running command.*/
	public Command CurrentCmd {
		get {
			return currentCmd;
		}
		protected set {
			if (currentCmd != null) {
				currentCmd.Terminate ();
			currentCmd.CurrentState = 0;
		}
			currentCmd = value;
		}
	}

	/**
	 * Should be called every frame by the command user. this ticks the move and is the head of all its logic.
	 */
	protected void ProcessCommand() {
		if (currentCmd == null)
			return;

		// Clean up terminated move.
		if (currentCmd.IsTerminated()) {
			currentCmd = null;
			return;
		}

		currentCmd.Tick ();
	}

	/**
	 * Called in order to do damage to the character. Changes state apropriately.
	 */
	public override void Hurt (float damage, Vector3 impact) {
		base.Hurt (damage, impact);
		if (state != CHAR_STATE.BLOCKING && state != CHAR_STATE.BLOCK_STUN) {
			ChangeState (CHAR_STATE.HIT_STUN);
		} else {
			ChangeState (CHAR_STATE.BLOCK_STUN);
		}
	}



	// All of these are tick functions for the current state. 

	protected virtual void TickBlocking() {}
	protected virtual void TickNeutral() {}

	// Make it so the hit stun ends after 30 seconds.
	protected void TickHitStun () {
		if (StateFrame > 30) {
			ChangeState (CHAR_STATE.NEUTRAL);
			return;
		}
		//Debug.Log (StateFrame);
		MoveCont.AddMovementInput (Vector3.back / 2);
	}

	// Cooldown for block stun
	protected virtual void TickBlockStun() {
		if (StateFrame > 15) {
			ChangeState (CHAR_STATE.BLOCKING);
			return;
		}


	}




}
