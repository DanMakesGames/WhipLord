using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MovementController : MonoBehaviour {
	// Needs the ability to keep track of the ground, not only in terms of floor angle, but also in termms of object, so
	// that elevators and other sorts of moving platforms could be implemented.

	//Constants

	// Distance to place stop before a hit object durring a move. This helps prevent moving inside of objects.
	private const float MOVE_HIT_SPACE = 0.001f;
	private const float RESOLVE_STRICTNESS = 0.1f;
	private const float PENETRATE_ADDITIONAL_SPACING = 0.125f;
	//private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.15f;
	private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.05f;
	private const float MAX_FLOOR_DIST = 2.4f;
	private const float MIN_FLOOR_DIST = 1.0f;


	enum MOVE_STATE { WALKING, FALLING, HALTED };
	private MOVE_STATE moveState;

	[SerializeField]
	private float maxMoveSpeed = 1;
	[SerializeField]
	private float gravity = -10;

	// In degrees, the max angle from Up vector that hte player can walk
	[SerializeField]
	private float maxWalkableSlope = 60;
	

	// Current player velocities.
	private Vector3 velocity;
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
		collisionBody = GetComponent<CapsuleCollider> ();
		moveState = MOVE_STATE.FALLING;

		bIgnoreInitPenetration = false;
	}
	

	void Update () {
		/*
		if (moveState == MOVE_STATE.WALKING) {
			Debug.Log ("Walking");
		} else {
			Debug.Log ("FALLING");
		}
		*/
		//Do not run movement if there is no body.
		if (collisionBody == null)
			return;

		ProcessInput ();

		if(moveState != MOVE_STATE.HALTED)
			PerformMovement ();
	}


	/**
	 * <summary>
	 * Method that is the head of all movement logic. Calling this each update what moves the player.
	 * </summary>
	 * 
	 */
	void PerformMovement() {
		//Velocity Update
		if (moveState == MOVE_STATE.WALKING) {
			
		}
		else if (moveState == MOVE_STATE.FALLING) {
			velocity += new Vector3 (0, gravity, 0) * Time.deltaTime;
		}

		Vector3 moveDelta = velocity * Time.deltaTime;
		Quaternion rotDelta = Quaternion.Euler(rotVelocity.eulerAngles * Time.deltaTime);

		RaycastHit hit;
		bool bCompleteMove = MoveAndResolve(moveDelta, rotDelta,out  hit);

		//Handle Blocking collision
		if (!bCompleteMove) {
			
			//If the Hit is a bottom hit
			if (IsBottomHit (hit)) {
				Debug.Log ("Bottom Hit");
				if (moveState == MOVE_STATE.FALLING) {
					moveState = MOVE_STATE.WALKING;
				} else if (moveState == MOVE_STATE.WALKING) {
					// Handle Walk Up here.
				}
			}
			// Hit is on side of body.
			else {
				//Handle wall slide here.
			}

		}

		//Peform Ground Check and update ground state
		if (moveState == MOVE_STATE.WALKING) {
			// Fist sweep downward and check for ground
			//RaycastHit[] hits = Physics.CapsuleCastAll (transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0),
			//	transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius, moveDelta.normalized, 
			//	moveDelta.magnitude);
			RaycastHit[] hits = Physics.CapsuleCastAll(transform.position + new Vector3(0,collisionBody.height / 2.0f - collisionBody.radius, 0), 
				transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
				collisionBody.radius, Vector3.down, MAX_FLOOR_DIST);

			bool bGroundFound = false;
			//Loop through the hits looking for a valid ground
			for(int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {
				if (IsBottomHit (hits [hitIndex])) {

					//Set Ground plane
					if( IsSlopeWalkable(hit.normal)) {
						GroundPlane = hit.normal;
					}

					//Floor Magentism
					if( hits[hitIndex].distance > MIN_FLOOR_DIST) {
						Debug.Log ("Standing adjust");
						RaycastHit adjustHit;
						MoveAndResolve (new Vector3(0.0f, MIN_FLOOR_DIST - hits[hitIndex].distance, 0.0f ),transform.rotation,out adjustHit);
						
					}

					bGroundFound = true;
					break;
				}
			
			}

			if (!bGroundFound) {
				moveState = MOVE_STATE.FALLING;
			}
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
	 * This handles interface with player controller for controlling player rotation. Can only effect pitch. Unlike 
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
		if (moveState == MOVE_STATE.WALKING) {
			// This is gonna need to change for sloped floor. Change after finishing MOVE system.
			//velocity += transform.rotation * inputVelocity * maxMoveSpeed;
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
	 * Moves player , stops movement if it collides with something it it's way.
	 * 
	 * @return Returns true if move completes with no collision.
	 */
	bool Move(Vector3 moveDelta, Quaternion rotDelta, out RaycastHit outHit, LinkedList<GameObject> ignoreObjects) {
		outHit = new RaycastHit();
		//Start with rotation update.
		transform.rotation = transform.rotation * rotVelocity;

		// Start by doing collision sweep.
		RaycastHit[] hits = Physics.CapsuleCastAll (transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0),
			transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius, moveDelta.normalized, 
			moveDelta.magnitude);



		bool bCompleteMove = true;
		//Process hits. If a valid hit is found, then blockingHit is set to it. If not then no hit occured.
		for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {

			bool bIgnored = false;
			if(hits[hitIndex].collider.gameObject == gameObject) {
				bIgnored = true;
			}
			// Test if the hit is a start penetrating hit. If so, return the function.
			if(!bIgnored && hits[hitIndex].distance == 0 && hits[hitIndex].point == Vector3.zero && !bIgnoreInitPenetration) {
				Debug.Log ("Start Penetrating");
				outHit = hits [hitIndex];
				return false;
			}

			//Testing if this actor is ignored
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
			// If the hit is not ignored, and is a potential real one.
			if( !bIgnored && Vector3.Dot(hits[hitIndex].normal, moveDelta.normalized) < 0) {
				bCompleteMove = false;
				outHit = hits [hitIndex];
				break;
			}
		}

		if (bCompleteMove) {
			//Debug.Log ("Complete Move");
			transform.position += moveDelta;

			return true;
		}
		else {
			//Debug.Log ("Blocked Move");
			if (bIgnoreInitPenetration)
				transform.position += moveDelta * (outHit.distance / moveDelta.magnitude);
			else
				transform.position += moveDelta * (outHit.distance / moveDelta.magnitude) + (outHit.normal * MOVE_HIT_SPACE);
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
	 * Attempts to move the player out of a penetration.
	 */
	private bool ResolvePenetration(Vector3 proposedAdjustment, RaycastHit hit, Quaternion newRotation) {

		//Frist overlap check and see if the location we want to move to is clean
		Collider[] overlaps = Physics.OverlapCapsule(transform.position + proposedAdjustment + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
			transform.position + proposedAdjustment - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius +  RESOLVE_STRICTNESS);


		//Loop and find the first overlap that is not hitting gameObject
		bool bValidOverlapOccured = false;
		for (int index = 0; index < overlaps.Length; index++) {
			if( overlaps[index].gameObject != gameObject) {
				bValidOverlapOccured = true;
			}
		}

		//No overlap occured, so the player can just be teleported
		if (!bValidOverlapOccured) {
			transform.position += proposedAdjustment;
		} 
		// If we cannot teleport out try the two methods of resolution.
		else {
			// First try to move out of the penetration.
			RaycastHit moveOutHit = new RaycastHit();
			bool bCompleteMove = Move (proposedAdjustment, newRotation, out moveOutHit);

			//Dual Penetration solver
			if(!bCompleteMove && IsStartPenetrating(moveOutHit)) {
				Vector3 otherAdjustment = CalcPenetrationAdjustment(moveOutHit);
				Vector3 comboAdjustment = otherAdjustment + proposedAdjustment;

				if(otherAdjustment != proposedAdjustment && comboAdjustment != Vector3.zero) {
					bIgnoreInitPenetration = true;
					bCompleteMove = Move (comboAdjustment, transform.rotation,out moveOutHit);
					bIgnoreInitPenetration = false;
				}

				// Add The Other solver here


			}
		
		}

		//Check if still penetrating and return false if we are.


		//Now Sweep out

	
		return true;

	}


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
		return  direction * (penetrationDepth + PENETRATE_ADDITIONAL_SPACING);
	}


	private bool PerformStepUp() {
		return true;
	}


	private bool PerformWallSilde() {
		return true;
	}

	/**
	 * Walks onto a slope. This is useful for overcoming peaks, when the angles may be incorrect. It also preserves 
	 * motion.
	 */
	private bool PerformWalkup(Vector3 moveDelta, RaycastHit slopeHit, out RaycastHit outHit) {
		

		Vector3 flatMovement = new Vector3 (moveDelta.x, 0, moveDelta.z);
		float groundFlatDeltaDot = Vector3.Dot (slopeHit.normal, flatMovement);
		Vector3 moveDirection = flatMovement;
		moveDirection.y = -(groundFlatDeltaDot / slopeHit.normal.z);
		Vector3 walkupDelta = moveDirection.normalized * (moveDelta * (slopeHit.distance / moveDelta.magnitude)).magnitude;

		RaycastHit walkupHit;
		bool bCompleteMove = MoveAndResolve (walkupDelta, transform.rotation, out walkupHit);
		outHit = walkupHit;

		return bCompleteMove;
	}


	private bool IsStartPenetrating(RaycastHit hit) {
		return (hit.distance == 0 && hit.point == Vector3.zero);
	}


	/**
	 * Checks if the hit is touching the botttom hemisphere of the capsule collider
	 */
	private bool IsBottomHit(RaycastHit hit) {
		Debug.DrawRay (hit.point, new Vector3(0,1,0));
		Vector3 vectorFromCenter = hit.point - transform.position;
		vectorFromCenter.y = 0;
		/*
		float distanceFromCenter = vectorFromCenter.sqrMagnitude;
		Debug.Log ("Distance: " + distanceFromCenter + " Radius: " + Mathf.Pow (collisionBody.radius - GROUND_DETECT_RADIUS_TOLERANCE,2));
		return distanceFromCenter < Mathf.Pow (collisionBody.radius - GROUND_DETECT_RADIUS_TOLERANCE,2);
		*/

		float distanceFromCenter = vectorFromCenter.magnitude;
		//Debug.Log ("Point"+ vectorFromCenter + "Distance: " + distanceFromCenter + " Radius: " + (collisionBody.radius  - GROUND_DETECT_RADIUS_TOLERANCE));
		return distanceFromCenter < collisionBody.radius - GROUND_DETECT_RADIUS_TOLERANCE;
	}


	/**
	 * Checks to see if a slope angle is not too slanted. Returns if a player can walk on it.
	 */
	private bool IsSlopeWalkable(Vector3 groundNormal) {
		return Vector3.Angle (groundNormal, Vector3.up) < maxWalkableSlope ;
	}


}
	