using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MovementController : MonoBehaviour {
	// Needs the ability to keep track of the ground, not only in terms of floor angle, but also in termms of object, so
	// that elevators and other sorts of moving platforms could be implemented.

	//Constants

	// Distance to place stop before a hit object durring a move. This helps prevent moving inside of objects.
	//private const float MOVE_HIT_SPACE = 0.001f;
	private const float MOVE_HIT_SPACE = 0.02f;
	//private const float MOVE_HIT_SPACE = 0.1f;
	//private const float MOVE_HIT_SPACE = 0.0f;
	private const float RESOLVE_STRICTNESS = 0.001f;
	private const float PENETRATE_ADDITIONAL_SPACING = 0.125f;
	//private const float PENETRATE_ADDITIONAL_SPACING = 0.001f;
	//private const float PENETRATE_ADDITIONAL_SPACING = 0.425f;
	//private const float PENETRATE_ADDITIONAL_SPACING = 0.025f;
	//private const float PENETRATE_ADDITIONAL_SPACING = 0.0001f;
	//private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.15f;
	//private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.05f;
	private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.005f;
	//private const float GROUND_DETECT_RADIUS_TOLERANCE = 0.001f;
	//private const float MAX_FLOOR_DIST = 2.4f;

	private const float MAX_FLOOR_DIST = 0.03f;
	//private const float MIN_FLOOR_DIST = 1.0f;
	private const float MIN_FLOOR_DIST = 0.01f;


	enum MOVE_STATE { WALKING, FALLING, HALTED };
	private MOVE_STATE moveState;

	[SerializeField]
	private float maxMoveSpeed = 1;
	[SerializeField]
	private float gravity = -10;

	// In degrees, the max angle from Up vector that hte player can walk
	[SerializeField]
	private float maxWalkableSlope = 60;
	private int castLayerMask;

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
		castLayerMask = 1 << 8;
		castLayerMask = ~castLayerMask;
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
		//Debug.Log (moveDelta);
		RaycastHit hit;
		bool bCompleteMove = MoveAndResolve(moveDelta, rotDelta,out  hit);

		//Handle Blocking collision
		if (!bCompleteMove) {
			
			//If the Hit is a bottom hit
			if (IsBottomHit (hit)) {
				
				if (moveState == MOVE_STATE.FALLING) {
					if (IsSlopeWalkable (hit.normal)) {
						GroundPlane = hit.normal;
						moveState = MOVE_STATE.WALKING;
					}
				} 
				else if (moveState == MOVE_STATE.WALKING) {
					
					// Handle Walk Up here.

					if (IsSlopeWalkable (hit.normal)) {
						
						RaycastHit walkupHit;
						Vector3 walkupDelta = moveDelta * (1 - (hit.distance / moveDelta.magnitude));
						bool bCompleteWalkup = PerformWalkup (walkupDelta, hit.normal, out walkupHit);

						//Walkup into wallslide
						if (!bCompleteWalkup) {
							Vector3 walkupWallSlideDelta = walkupDelta * (1 - (walkupHit.distance / walkupDelta.magnitude));
							PerformWallSlide (walkupWallSlideDelta, walkupHit.normal);
						}
					}
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


		//Peform Ground Check and update ground state
		if (moveState == MOVE_STATE.WALKING) {
			// Fist sweep downward and check for ground
			//RaycastHit[] hits = Physics.CapsuleCastAll (transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0),
			//	transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius, moveDelta.normalized, 
			//	moveDelta.magnitude);
			RaycastHit[] hits = Physics.CapsuleCastAll(transform.position + new Vector3(0,collisionBody.height / 2.0f - collisionBody.radius, 0), 
				transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
				collisionBody.radius, Vector3.down, MAX_FLOOR_DIST, castLayerMask);

			bool bGroundFound = false;
			//Loop through the hits looking for a valid ground
			for(int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {
				if (IsBottomHit (hits [hitIndex]) && IsSlopeWalkable(hits[hitIndex].normal)) {

					//Set Ground plane
					GroundPlane = hits[hitIndex].normal;
				

					//Floor Magentism
					if( hits[hitIndex].distance > MIN_FLOOR_DIST || hits[hitIndex].distance < MIN_FLOOR_DIST) {
						//Debug.Log ("Standing adjust");
						RaycastHit adjustHit;
						MoveAndResolve (new Vector3(0.0f, MIN_FLOOR_DIST - hits[hitIndex].distance, 0.0f ),transform.rotation,out adjustHit);
					}

					bGroundFound = true;
					break;
				}
			
			}

			if (!bGroundFound) {
				
				Debug.Log ("NO GROUND FOUND SET FALLING");
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
	public bool Move(Vector3 moveDelta, Quaternion rotDelta, out RaycastHit outHit, LinkedList<GameObject> ignoreObjects) {
		outHit = new RaycastHit();
		//Start with rotation update.
		transform.rotation = transform.rotation * rotVelocity;

		// Start by doing collision sweep.
		RaycastHit[] hits = Physics.CapsuleCastAll (transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0),
			transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius, moveDelta.normalized, 
			moveDelta.magnitude, castLayerMask);

		/*
		Debug.Log ("HIT OBJECTS: ");
		for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {
			Debug.Log (hits[hitIndex].collider.gameObject.name + " :: " + hits[hitIndex].distance);
		}
		*/

		bool bCompleteMove = true;
		//Process hits. If a valid hit is found, then blockingHit is set to it. If not then no hit occured.
		//for(int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {
		for(int hitIndex = hits.Length - 1; hitIndex >= 0; hitIndex--) {
			if (hits[hitIndex].collider.gameObject != gameObject && hits [hitIndex].distance == 0 && hits [hitIndex].point == Vector3.zero) {
				bool bIgnored = false;

				if (ignoreObjects != null) {
					LinkedListNode<GameObject> currentNode = ignoreObjects.First;
					for (int ignoreIndex = 0; ignoreIndex < ignoreObjects.Count; ignoreIndex++) {
						//If this actor is one of the ignored actors.
						if (currentNode.Value == hits [hitIndex].collider.gameObject) {
							//Debug.Log ("\tIGNORING: " + hits [hitIndex].collider.gameObject.name);
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


		double smallest = -1;
		for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++) {
		//for (int hitIndex = hits.Length - 1; hitIndex >= 0; hitIndex--) {

			//Debug.Log ("name: " + hits[hitIndex].collider.gameObject.name);
			bool bIgnored = false;


			if(hits[hitIndex].collider.gameObject == gameObject) {
				//Debug.Log ("Self");
				bIgnored = true;
			}


			
			//Testing if this actor is ignored
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

			// Test if the hit is a start penetrating hit. If so, return the function.
			if (!bIgnored && hits [hitIndex].distance == 0 && hits [hitIndex].point == Vector3.zero) {
				if (bIgnoreInitPenetration == true) {
					//Debug.Log ("Ignoring start penetrating collision");
					bIgnored = true;
				} else {
					//Debug.Log ("Start Penetrating");
					outHit = hits [hitIndex];
					return false;
				}
			}


			// If the hit is not ignored, and is a potential real one.
			if( !bIgnored && Vector3.Dot(hits[hitIndex].normal, moveDelta.normalized) < 0) {
				//Debug.Log ("REAL HIT");
				bCompleteMove = false;

				if (hits[hitIndex].distance < smallest || smallest == -1) {
					outHit = hits [hitIndex];
					smallest = hits [hitIndex].distance;
				}

			}

		}



		if (bCompleteMove) {
			//Debug.Log ("Complete Move: " + moveDelta);
			transform.position += moveDelta;

			return true;
		}
		else {
			//Debug.Log ("Blocked Move: "+ outHit.collider.gameObject.name + " : " + outHit.distance);
			if (bIgnoreInitPenetration)
				transform.position += moveDelta * ((outHit.distance / moveDelta.magnitude));
				//transform.position += moveDelta * (1 -(outHit.distance / moveDelta.magnitude));
			else
				//transform.position += moveDelta * ((outHit.distance / moveDelta.magnitude)) + (outHit.normal * MOVE_HIT_SPACE);
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
	 * Attempts to move the player out of a penetration.
	 */
	private bool ResolvePenetration(Vector3 proposedAdjustment, RaycastHit hit, Quaternion newRotation) {
		
		//Frist overlap check and see if the location we want to move to is clean
		Collider[] overlaps = Physics.OverlapCapsule(transform.position + proposedAdjustment + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
			transform.position + proposedAdjustment - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius +  RESOLVE_STRICTNESS, castLayerMask);

		Debug.Log ("PENETRATION RESOLVE: " + hit.collider.gameObject.name + " :: " +hit.normal + "::" + proposedAdjustment);
		//Loop and find the first overlap that is not hitting gameObject
		bool bValidOverlapOccured = false;
		for (int index = 0; index < overlaps.Length; index++) {
			if( overlaps[index].gameObject != gameObject) {
				bValidOverlapOccured = true;
			}
		}

		//No overlap occured, so the player can just be teleported
		if (!bValidOverlapOccured) {
			Debug.Log ("\tTeleport Resolution");
			transform.position += proposedAdjustment;
		} 
		// If we cannot teleport out try the two methods of resolution.
		else {
			Debug.Log ("\tMove out Resolution: wall");
			// First try to move out of the penetration.
			RaycastHit moveOutHit;
			//bIgnoreInitPenetration = true;


			LinkedList<GameObject> ignoreList = new LinkedList<GameObject> ();
			ignoreList.AddFirst ( new LinkedListNode<GameObject>(hit.collider.gameObject));
			bool bCompleteMove = Move (proposedAdjustment, newRotation, out moveOutHit, ignoreList);


			//bIgnoreInitPenetration = false;
			Debug.Log ("Distance Traveled: " + moveOutHit.distance + "::point:: " + moveOutHit.point);

			//Dual Penetration solver
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

		//Check if still penetrating and return false if we are.
		Collider[] overlapss = Physics.OverlapCapsule(transform.position + new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), 
			transform.position - new Vector3(0, collisionBody.height / 2.0f - collisionBody.radius, 0), collisionBody.radius);
		if (overlapss.Length > 0) {
			Debug.Log ("FAAAAAAAAAAAIAIIAIAIAIAIAIALILAIHLSFIHL:SYFDGAS");
		}

		//Now Sweep out

		bIgnoreInitPenetration = false;
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
		//Debug.Log ("Distance::: " + distance);
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
		
		Debug.Log ("Perform Walkup called.");
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


	private bool PerformWallSlide(Vector3 moveDelta, Vector3 wallNormal) {
		
		wallNormal.Normalize ();
		Vector3 travelDirection = Vector3.Cross (GroundPlane, wallNormal).normalized;

		Vector3 slideVector = moveDelta.magnitude * (Vector3.Dot(moveDelta.normalized, travelDirection)) * travelDirection;
		//Debug.DrawLine (wallHit.point, wallHit.point + slideVector * 100);
		RaycastHit slideHit;

		//bool bCompleteMove = MoveAndResolve (slideVector,transform.rotation, out slideHit);
		Debug.Log ("Wall Slide: " + slideVector.x + ", " + slideVector.y + ", " + slideVector.z);
		bool bCompleteMove = MoveAndResolve (slideVector,transform.rotation, out slideHit);
		//Debug.Log ("Wall Slide: " + slideVector + " :: " + slideHit.distance);
		//Debug.Log ("\t" + IsBottomHit (slideHit) + " :: " + IsSlopeWalkable (slideHit.normal));
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
	