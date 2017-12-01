/**
 * Written by Daniel Mann.
 * created in 2017
 * DanielMannGames@outlook.com
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class MovementController : MonoBehaviour {

	// Distance to place the player from stopping hit when moving. This spacing helps prevent future penetration.
	private const float MOVE_HIT_SPACE = 0.001f;

	// Tolerance to teleport resolve
	private const float RESOLVE_STRICTNESS = 0.001f;

	// Additional spacing when resolving penetration 
	private const float PENETRATE_ADDITIONAL_SPACING = 0.125f;

	// Tolerance for detection if a hit is a ground hit
	private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.005f;

	// Max and min distances from ground before correction.
	private const float MAX_FLOOR_DIST = 0.03f;
	private const float MIN_FLOOR_DIST = 0.01f;


	enum MOVE_STATE { WALKING, // Grounded movement
					  FALLING, // Uncontrolled falling movement.
					  HALTED // This state prevents any movement from happening. Freezes the movement.
					};
	private MOVE_STATE moveState;

	[SerializeField]
	private float maxMoveSpeed = 1;
	[SerializeField]
	private float gravity = -10;

	// In degrees, the max angle from Up vector that hte player can walk
	[SerializeField]
	private float maxWalkableSlope = 60;

	// This mask determines what layers this movement controller colides with.
	private int castLayerMask;
	public int CastLayerMask{
		get { return castLayerMask;}
		set { castLayerMask = value;}
	}

	Animator animator;

	// Current player velocities.
	private Vector3 velocity;
	public Vector3 oldVelocity;
	private Quaternion rotVelocity;

	// This stores the desired movement input. Zeroed at end of every tick. Keep in mind that this is realtive to the player space.
	private Vector3 inputVelocity;
	private Quaternion inputRotVelocity;

	// This is the body that is used for movement. Used to test collision.
	private CapsuleCollider collisionBody;

	// Vector representing the plane the player is walking on.
	private Vector3 groundPlane;
	// Vector representing the plane the player is walking on.
	public Vector3 GroundPlane {
		get {
			return groundPlane;
		}
		protected set {
			groundPlane = value.normalized;
		}
	}

	// If true, the movement controller ignores inintial penetrations durring the Move function. Used for penetration 
	// resolution.
	private bool bIgnoreInitPenetration;

	void Start () {
		// Needs a capsule collider in order to function.
		collisionBody = GetComponent<CapsuleCollider> ();

		// Start falling
		moveState = MOVE_STATE.FALLING;

		animator = GetComponent<Animator> ();

		bIgnoreInitPenetration = false;
	}
	

	void Update () {

		// Dont run with there is no collision capslue attached to this gameObject.
		if (collisionBody == null)
			return;

		//first consume input
		ProcessInput ();

		//
		if(moveState != MOVE_STATE.HALTED)
			PerformMovement ();

		UpdateAnimation ();
	}

	/**
	 * Update the animation of the character usiing this movement controller. In future implementations remove this 
	 * functionality from this class.
	 */
	void UpdateAnimation() {


		if (animator != null) {
			
			if (oldVelocity.magnitude > 0 && moveState != MOVE_STATE.FALLING) {

				// Start run aimation if the player is moveing.
				animator.SetBool ("WalkForward", true);

				// Based on the heading of the player set the Heading property in the animator. This controls the 
				// direction the legs of the character run in. 
				Vector3 velocityHeading = oldVelocity;
				velocityHeading.y = 0;

				// Convert to the GameObject local space
				Vector3 heading = Quaternion.Inverse (transform.rotation) * velocityHeading;

				float finalHeading = Vector3.Dot (heading.normalized, Vector3.right) * 0.5f ;

				// Make it so the legs have the opposite rotation if the palyer is moving backwards
				if(Vector3.Dot(heading.normalized,Vector3.forward) < -0.1) {
					finalHeading = -finalHeading;
				}


				animator.SetFloat ("Heading", finalHeading + 0.5f);

			}
			else
				animator.SetBool ("WalkForward", false);
		}
	}
	
	/**
	 * Method that is the head of all movement logic. Calling this each update is what moves the player.
	 */
	void PerformMovement() {

		//Velocity Update
		if (moveState == MOVE_STATE.FALLING) {
			velocity += new Vector3 (0, gravity, 0) * Time.deltaTime;
		}

		// moveDelta is the desired position change.
		Vector3 moveDelta = velocity * Time.deltaTime;

		// rotDelta is the desired rotation change.
		Quaternion rotDelta = Quaternion.Euler(rotVelocity.eulerAngles * Time.deltaTime);

		// Initial Move
		RaycastHit hit;
		bool bCompleteMove = MoveAndResolve(moveDelta, rotDelta, out hit);

		//Handle Blocking collision
		if (!bCompleteMove) {
			
			// If the Hit is a bottom hit
			if (IsBottomHit (hit)) {

				// If a walkable surface is hit, stop falling
				if (moveState == MOVE_STATE.FALLING) {
					if (IsSlopeWalkable (hit.normal)) {
						GroundPlane = hit.normal;
						moveState = MOVE_STATE.WALKING;
					}
				} 
				else if (moveState == MOVE_STATE.WALKING) {
					

					// IF we hit a slope, start walkup if possible.
					if (IsSlopeWalkable (hit.normal)) {

						// walkup.
						RaycastHit walkupHit;
						Vector3 walkupDelta = moveDelta * (1 - (hit.distance / moveDelta.magnitude));
						bool bCompleteWalkup = PerformWalkup (walkupDelta, hit.normal, out walkupHit);

						// Walkup into wallslide
						if (!bCompleteWalkup) {
							Vector3 walkupWallSlideDelta = walkupDelta * (1 - (walkupHit.distance / walkupDelta.magnitude));
							PerformWallSlide (walkupWallSlideDelta, walkupHit.normal);
						}
					}

					// If the slope is not walkable, wall-side against it.
					else {
						Vector3 slopeWallSlide = moveDelta * (1 - (hit.distance / moveDelta.magnitude));
						bool bCompleteWallSlide = PerformWallSlide (slopeWallSlide,hit.normal);
					}
				}
			}

			// Hit is on side of body.
			else {
				if (moveState == MOVE_STATE.FALLING) {
					velocity -= Vector3.Dot (hit.normal, velocity.normalized) * velocity.magnitude * hit.normal;
				}
				// Perform wallSilde
				if (moveState == MOVE_STATE.WALKING) {
					Vector3 wallSildeDelta = moveDelta * (1 - (hit.distance / moveDelta.magnitude) );
					PerformWallSlide (wallSildeDelta, hit.normal);
				}

			}

		}

		// Capture velocity 
		oldVelocity = velocity;

		// Peform Ground Check and update ground state. 
		if (moveState == MOVE_STATE.WALKING) {
			
			// Fist sweep downward and check for ground
			RaycastHit[] hits = Physics.CapsuleCastAll(transform.position + new Vector3(0,collisionBody.height / 2.0f - collisionBody.radius, 0), 
				transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
				collisionBody.radius, Vector3.down, MAX_FLOOR_DIST, castLayerMask);
			
			// Set true is valid ground is found
			bool bGroundFound = false;

			//Loop through the hits looking for a valid ground
			for(int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {

				// If hit is a vlid ground
				if (hits[hitIndex].collider.gameObject != gameObject && IsBottomHit (hits [hitIndex]) && IsSlopeWalkable(hits[hitIndex].normal) ) {

					//Set Ground plane
					GroundPlane = hits[hitIndex].normal;
				

					// Floor Magentism. Move down so the distance is minimum.
					if( hits[hitIndex].distance > MIN_FLOOR_DIST || hits[hitIndex].distance < MIN_FLOOR_DIST) {
						
						RaycastHit adjustHit;
						MoveAndResolve (new Vector3(0.0f, MIN_FLOOR_DIST - hits[hitIndex].distance, 0.0f), transform.rotation, out adjustHit);
					}

					bGroundFound = true;
					break;
				}
			
			}

			// If no valid ground is found then the player should begin to fall.
			if (!bGroundFound) {
				
				moveState = MOVE_STATE.FALLING;
			}
			// If there is a ground apply ground friction. Prevent player from maintaining velocity.
			else {
				
				velocity = Vector3.zero;
			}
		}
	}


	/**
	 * This function is the interface with player controller to apply movement input.
	 * 
	 * @param Input Vector of movement input to be added.
	 */
	public void AddMovementInput(Vector3 inputVector) {
		if(moveState == MOVE_STATE.WALKING) {
			inputVelocity += inputVector;
		}
	}


	/**
	 * This handles interface with player controller for controlling player rotation. Can only effect yaw.
	 * 
	 * @param inputRotation Rotation to add to the rotation of the player.
	 */
	public void AddRotationInput(Quaternion inputRotation) {
		inputRotVelocity = inputRotVelocity * inputRotation;
	}


	/**
	 * This processes all the input that has been collected up until this point. 
	 */
	void ProcessInput() {

		// If we are walking transfrom inputVelocity into velocity.
		if (moveState == MOVE_STATE.WALKING) {
			Vector3 rotSafeInput = (transform.rotation * inputVelocity).normalized;
			float Y = -Vector3.Dot (GroundPlane, rotSafeInput) / GroundPlane.y;
			Vector3 direction = rotSafeInput;
			direction.y = Y;
			velocity += direction.normalized * maxMoveSpeed;
		}

		rotVelocity = inputRotVelocity;

		//Reset input in preperation for next Frame of input collection
		inputVelocity = new Vector3 (0,0,0);
		inputRotVelocity = Quaternion.Euler (0, 0, 0);
	}
		

	/**
	 * Moves player , stops movement if it collides with something it it's way. Foundation of all movement.
	 * 
	 * @param moveDelta Change in position.
	 * @param rotDelta Change in rotation.
	 * @param outHit returns the hit object if this move does not fully complete.
	 * @param ignoredObjects List of specific objects to not collide with during this move.
	 * @return Returns true if move completes with no collision.
	 */
	public bool Move(Vector3 moveDelta, Quaternion rotDelta, out RaycastHit outHit, LinkedList<GameObject> ignoreObjects) {
		outHit = new RaycastHit();

		//Start with rotation update.
		transform.rotation = transform.rotation * rotVelocity;

		// Start movement change by doing collision sweep.
		RaycastHit[] hits = Physics.CapsuleCastAll (transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0),
			transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius, moveDelta.normalized, 
			moveDelta.magnitude, castLayerMask);

		// Keeps track if this move completed withough hitting anything.
		bool bCompleteMove = true;

		//Search for initial penetrations.
		for(int hitIndex = hits.Length - 1; hitIndex >= 0; hitIndex--) {
			if (hits[hitIndex].collider.gameObject != gameObject && hits [hitIndex].distance == 0 && hits [hitIndex].point == Vector3.zero) {
				bool bIgnored = false;

				if (ignoreObjects != null) {
					LinkedListNode<GameObject> currentNode = ignoreObjects.First;
					for (int ignoreIndex = 0; ignoreIndex < ignoreObjects.Count; ignoreIndex++) {
						//If this actor is one of the ignored actors.
						if (currentNode.Value == hits [hitIndex].collider.gameObject) {
							
							bIgnored = true;
							break;
						}
						currentNode = currentNode.Next;
					}
				}


				if(!bIgnored && bIgnoreInitPenetration == false) {
					Debug.Log ("Start Penetrating");
					outHit = hits [hitIndex];
					return false;
				}
			}
		}


		// Holds the distnace of the closest hit.
		double smallest = -1;

		// Process all hits, find the first valid occuring one.
		for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {

			// keeps track if this hit should be igrnored (is not valid)
			bool bIgnored = false;

			// if this is the movecomponent body then ignore
			if(hits[hitIndex].collider.gameObject == gameObject) {
				
				bIgnored = true;
			}
				
			//Testing if this actor on list of ignored
			if (ignoreObjects != null) {
				
				LinkedListNode<GameObject> currentNode = ignoreObjects.First;

				for (int ignoreIndex = 0; ignoreIndex < ignoreObjects.Count; ignoreIndex++) {
					

					//If this actor is one of the ignored actors.
					if (currentNode.Value == hits [hitIndex].collider.gameObject) {
						Debug.Log ("\tIGNORING: " + hits [hitIndex].collider.gameObject.name);
						bIgnored = true;
						break;
					}

					currentNode = currentNode.Next;
				}
			}

			// TODO: remove this block.
			// Test if the hit is a start penetrating hit. If so, return the function.
			if (!bIgnored && hits [hitIndex].distance == 0 && hits [hitIndex].point == Vector3.zero) {
				if (bIgnoreInitPenetration == true) {
					
					bIgnored = true;
				} else {
					
					outHit = hits [hitIndex];
					return false;
				}
			}
				
			// If the hit is not ignored, and is a potential real one.
			if( !bIgnored && Vector3.Dot(hits[hitIndex].normal, moveDelta.normalized) < 0) {
				
				bCompleteMove = false;

				// Test if this hit is the closest (and thus the one that would occur first).
				if (hits[hitIndex].distance < smallest || smallest == -1) {
					outHit = hits [hitIndex];
					smallest = hits [hitIndex].distance;
				}

			}

		}

		// If check completed withough finding a valid hit, then perform the full move.
		if (bCompleteMove) {
			
			transform.position += moveDelta;
			return true;
		}
		// Otherwise move up to the hit location.
		else {
			if (bIgnoreInitPenetration)
				transform.position += moveDelta * ((outHit.distance / moveDelta.magnitude));
			else
				transform.position += moveDelta * ((outHit.distance / moveDelta.magnitude)) + (outHit.normal * MOVE_HIT_SPACE);
			return false;
		}

	}


	/**
	 * Move overload where no actors are ignored.
	 */
	bool Move(Vector3 moveDelta, Quaternion rotDelta, out RaycastHit outHit) {
		return Move (moveDelta, rotDelta, out outHit, null);
	}


	/**
	 * Performs the same action as Move, but if the move starts penetrating, it will attempt to resolve the move,
	 * then it trys the same move again.
	 * 
	 * @return Returns true if move fully completes with no collision.
	 */
	public bool MoveAndResolve(Vector3 moveDelta, Quaternion rotDelta, out RaycastHit outHit) {
		RaycastHit hit;

		//Initial Move Attempt
		bool bCompleteMove = Move(moveDelta, rotDelta, out hit);

		// If the move started penetrating, resolve the penetration and try move again.
		if(!bCompleteMove && IsStartPenetrating(hit)) {
			Vector3 penetrationAdjustment = CalcPenetrationAdjustment (hit);
			ResolvePenetration (penetrationAdjustment, hit, transform.rotation);
			bCompleteMove = Move (moveDelta, rotDelta, out hit);
		}

		outHit = hit;

		return bCompleteMove;
	}


	/**
	 * Attempts to move the player out of a penetration. Tries a couple of different techniques before giving up.
	 * 
	 * @param proposedAdjustment This is the proposed adjustment to perform. Relative to palyer current location.
	 * @param hit This is the overlapping hit between the character and the object they are penetrating.
	 * @param newRotation New rotation to change to. TODO: remove this from the function, it is unneeded.
	 * @return Returns ture if penetration resolution succeeds, false if not.
	 */
	private bool ResolvePenetration(Vector3 proposedAdjustment, RaycastHit hit, Quaternion newRotation) {
		
		//Frist overlap check and see if the location we want to move to is clean of other objects
		Collider[] overlaps = Physics.OverlapCapsule(transform.position + proposedAdjustment + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
			transform.position + proposedAdjustment - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius +  RESOLVE_STRICTNESS, castLayerMask);

		Debug.Log ("PENETRATION RESOLVE: " + hit.collider.gameObject.name + " :: " +hit.normal + "::" + proposedAdjustment);

		//Loop and find the first overlap
		bool bValidOverlapOccured = false;
		for (int index = 0; index < overlaps.Length; index++) {
			if( overlaps[index].gameObject != gameObject) {
				bValidOverlapOccured = true;
			}
		}

		// No overlap occured, so the player can just be teleported
		if (!bValidOverlapOccured) {
			Debug.Log ("\tTeleport Resolution");
			transform.position += proposedAdjustment;
		} 
		// If we cannot teleport out try the two methods of resolution.
		else {
			Debug.Log ("\tMove out Resolution: wall");

			// First try to move out of the penetration.
			RaycastHit moveOutHit;
			bIgnoreInitPenetration = true;


			LinkedList<GameObject> ignoreList = new LinkedList<GameObject> ();
			ignoreList.AddFirst ( new LinkedListNode<GameObject>(hit.collider.gameObject));
			bool bCompleteMove = Move (proposedAdjustment, newRotation, out moveOutHit, ignoreList);


			bIgnoreInitPenetration = false;
			Debug.Log ("Distance Traveled: " + moveOutHit.distance + "::point:: " + moveOutHit.point);

			// Dual Penetration solver
			if(!bCompleteMove && IsStartPenetrating(moveOutHit)) {
				Debug.Log ("\t\tDual Penetration solver");
				Vector3 otherAdjustment = CalcPenetrationAdjustment(moveOutHit);
				Vector3 comboAdjustment = otherAdjustment + proposedAdjustment;

				if(otherAdjustment != proposedAdjustment && comboAdjustment != Vector3.zero) {
					bIgnoreInitPenetration = true;
					bCompleteMove = Move (comboAdjustment, transform.rotation,out moveOutHit);
					bIgnoreInitPenetration = false;
				}
			
			}

			// Third occasional solver. Not very effective. Last resort.
			if (!bCompleteMove) {
				Debug.Log ("\t\tOTHER SOLVER");
				Vector3 otherAdjustment = moveOutHit.normal * PENETRATE_ADDITIONAL_SPACING;
				Vector3 comboAdjustment = otherAdjustment + proposedAdjustment;

				bIgnoreInitPenetration = true;
				Move (comboAdjustment, transform.rotation, out moveOutHit);
				bIgnoreInitPenetration = false;
			}

			ignoreList.Clear();
		
		}

		bIgnoreInitPenetration = false;

		//Check if still penetrating and return false if we are.
		Collider[] overlapss = Physics.OverlapCapsule(transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
			transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius);
		if (overlapss.Length > 0) {
			Debug.Log ("Resolve Fail");
			return false;
		}
			
		return true;
	}


	/**
	 * Calculate a movement that will remove the character from penetation.
	 * 
	 * @param hit Penetrating hit.
	 * @return movement that should resolve the penetration.
	 */
	private Vector3 CalcPenetrationAdjustment(RaycastHit hit) {

		//If not penetrating then return nothing.
		if(!IsStartPenetrating(hit)) {
			return Vector3.zero;
		}
			
		Vector3 direction;
		float distance;
		bool  bDidCalc = Physics.ComputePenetration (collisionBody, transform.position, transform.rotation, 
			hit.collider, hit.collider.transform.position, hit.collider.transform.rotation, out direction, out distance);
		
		float penetrationDepth  = (distance > 0.0f ? distance : PENETRATE_ADDITIONAL_SPACING);
		return  direction.normalized * (penetrationDepth + PENETRATE_ADDITIONAL_SPACING);
	}


	private bool PerformStepUp() {
		return true;
	}


	/**
	 * Walks onto a slope. This is useful for overcoming peaks, when the angles may be incorrect. It also preserves 
	 * motion.
	 */
	private bool PerformWalkup(Vector3 moveDelta, Vector3 slopeNormal, out RaycastHit outHit) {
		
		slopeNormal.Normalize ();
		Vector3 flatMovement = new Vector3 (moveDelta.x, 0.0f, moveDelta.z);

		float groundFlatDeltaDot = Vector3.Dot (slopeNormal, flatMovement);
		Vector3 moveDirection = flatMovement;

		moveDirection.y = -(groundFlatDeltaDot / slopeNormal.y);
		Vector3 walkupDelta = moveDirection.normalized * moveDelta.magnitude;

		RaycastHit walkupHit;
		bool bCompleteMove = MoveAndResolve (walkupDelta, transform.rotation, out walkupHit);
		outHit = walkupHit;

		return bCompleteMove;
	}

	/**
	 * Performes a wall slide. Actaully does the movement and returns true if the full movement completes without
	 * interuption.
	 */
	private bool PerformWallSlide(Vector3 moveDelta, Vector3 wallNormal) {
		
		wallNormal.Normalize ();
		Vector3 travelDirection = Vector3.Cross (GroundPlane, wallNormal).normalized;

		Vector3 slideVector = moveDelta.magnitude * (Vector3.Dot(moveDelta.normalized, travelDirection)) * travelDirection;

		RaycastHit slideHit;

		bool bCompleteMove = MoveAndResolve (slideVector,transform.rotation, out slideHit);

		if (!bCompleteMove) {
			
			if (IsBottomHit (slideHit) && IsSlopeWalkable (slideHit.normal)) {
				Debug.Log ("\t+ walkup");
				Vector3 walkupDelta = slideVector * (1 - (slideHit.distance / slideVector.magnitude));
				RaycastHit walkupHit;
				PerformWalkup (walkupDelta, slideHit.normal, out walkupHit);
			}


		}


		return bCompleteMove;
	}


	/**
	 * Determines if a hit is one reporting an initual penetration.
	 */
	private bool IsStartPenetrating(RaycastHit hit) {
		return (hit.distance == 0 && hit.point == Vector3.zero);
	}


	/**
	 * Checks if the hit is touching the botttom hemisphere of the capsule collider
	 */
	private bool IsBottomHit(RaycastHit hit) {

		Vector3 vectorFromCenter = hit.point - transform.position;
		vectorFromCenter.y = 0;  

		float distanceFromCenter = vectorFromCenter.magnitude;
		return distanceFromCenter < collisionBody.radius - GROUND_DETECT_RADIUS_TOLERANCE;
	}


	/**
	 * Checks to see if a slope angle is not too slanted. Returns if a player can walk on it.
	 */
	private bool IsSlopeWalkable(Vector3 groundNormal) {
		return Vector3.Angle (groundNormal, Vector3.up) < maxWalkableSlope ;
	}
}
	