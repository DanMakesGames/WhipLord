using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * This is the head of what a Pawn is. They have a Movement Controller, and update funcation. IN the future they will have 
 * health and damage. Basically this is some sort of Enemy, Player, AI beinging that is a person or creature in the world.
 */
public abstract class PawnController : MonoBehaviour
{
	private MovementController moveCont = null;
	public MovementController MoveCont {
		get {
			return moveCont;
		}
		protected set {
			moveCont = value;
		}
	}

	// FUTURE: Some sort of damage type
	public virtual void Hurt (float damage, Vector3 impact) {}

}


