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
	public Command CurrentCmd {
		get {
			return currentCmd;
		}
		protected set {
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

}
