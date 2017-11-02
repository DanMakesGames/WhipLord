using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {
	// Needs the ability to keep track of the ground, not only in terms of floor angle, but also in termms of object, so
	// that elevators and other sorts of moving platforms could be implemented.

	void Start () {
		
	}
	

	void Update () {
		ProcessInput ();

		PerformMovement ();
	}


	/**
	 * Method that is the head of all movement logic. Calling this each update what moves the player.
	 */
	void PerformMovement() {
	
	}


	/**
	 * This processes all the input that has been collected up until this point. 
	 */
	void ProcessInput() {
	}


	/**
	 * Moves player , stops movement if it collides with something it it's way.
	 * 
	 * @return Returns true if move completes with no collision.
	 */
	bool Move() {
		return true;
	}


	/**
	 * Performs the same action as Move, but if the move starts penetrating, it will attempt to resolve the move,
	 * then it trys the same move again.
	 * 
	 * @return Returns true if move fully completes with no collision.
	 */
	public bool MoveAndResolve() {
	}


	private bool ResolvePenetration() {
	}


	private Vector3 CalcPenetrationAdjustment() {
	}


	private bool PerformStepUp() {
	}


	private bool PerformWallSilde() {
	}


	private bool PerformWalkup() {
	}
}
