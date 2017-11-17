using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;
/**
 *  This class contains all the necessary info in order to use moves with it. Priamrily this needs to exist so that it 
 * can be assumed that this Character operates under fighing game esque rules:
 * They perform command moves
 * They have command move related player states like BLOCKING, HIT_HITSTUNNED, GROUNDED, KNOCKED_DOWN
 * 
 * This defeinition is at the whim of what CommandMoves Need.
 */
public class CommandController : PawnController
{
	private Command currentCmd;

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

	//protected virtual void TickHitStun() {}
	//protected virtual void TickBlockStun() {}
	protected virtual void TickBlocking() {}
	protected virtual void TickNeutral() {}

	// If true the player can move. If false the palyer cannot
	private bool bCanMove = true;
	public bool CanMove {
		get {return bCanMove;}
		set { bCanMove = value;}
	}


	// Set false when a command starts
	private bool bCommmandOver = true;


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

	protected void ProcessCommand() {
		if (currentCmd == null)
			return;

		if (currentCmd.IsTerminated()) {
			currentCmd = null;
			return;
		}

		currentCmd.Tick ();
	}

	public override void Hurt (float damage, Vector3 impact) {
		base.Hurt (damage, impact);
		if (state != CHAR_STATE.BLOCKING && state != CHAR_STATE.BLOCK_STUN) {
			ChangeState (CHAR_STATE.HIT_STUN);
		} else {
			ChangeState (CHAR_STATE.BLOCK_STUN);
		}
	}

	protected void TickHitStun () {
		if (StateFrame > 30) {
			ChangeState (CHAR_STATE.NEUTRAL);
			return;
		}
		//Debug.Log (StateFrame);
		MoveCont.AddMovementInput (Vector3.back / 2);
	}

	protected virtual void TickBlockStun() {
		if (StateFrame > 15) {
			ChangeState (CHAR_STATE.BLOCKING);
			return;
		}


	}




}
