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



	enum MOVE_STATE { WALKING, FALLING, HALTED };
	private MOVE_STATE moveState;

	[SerializeField]
	private float maxMoveSpeed = 1;

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
			groundPlane = value;
		}
	}

	// If true, the movement controller ignores inintial penetrations durring the Move function. Used for penetration 
	// resolution.
	bool bIgnoreInitPenetration;


	void Start () {
		collisionBody = GetComponent<CapsuleCollider> ();
		moveState = MOVE_STATE.WALKING;

		bIgnoreInitPenetration = false;
	}
	

	void Update () {

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
		Vector3 moveDelta = velocity * Time.deltaTime;
		Quaternion rotDelta = Quaternion.Euler(rotVelocity.eulerAngles * Time.deltaTime);
		RaycastHit hit = new RaycastHit();
		Move (moveDelta,rotDelta,ref hit,null);
		velocity = Vector3.zero;
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
			velocity += transform.rotation * inputVelocity * maxMoveSpeed;
			//Debug.Log ("adding velocity " + velocity);
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
	bool Move(Vector3 moveDelta, Quaternion rotDelta, ref RaycastHit outHit, LinkedList<GameObject> ignoreObjects) {
		Debug.Log ("Start: " +moveDelta);
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
			Debug.Log ("Applying movement" + moveDelta);
			transform.position += moveDelta;

			return true;
		}
		else {
			Debug.Log ("Applying movement");
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
	bool Move(Vector3 moveDelta, Quaternion rotDelta, ref RaycastHit outHit) {
		return Move (moveDelta, rotDelta, ref outHit, null);
	}


	/**
	 * Performs the same action as Move, but if the move starts penetrating, it will attempt to resolve the move,
	 * then it trys the same move again.
	 * 
	 * @return Returns true if move fully completes with no collision.
	 */
	public bool MoveAndResolve() {
		return true;
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
		
		if (!bValidOverlapOccured) {
			transform.position += proposedAdjustment;

		} 
		// If we cannot teleport out try the two methods of resolution.
		else {
		
		}

		//Check if still penetrating and return false if we are.


		//Now Sweep out

	
		return true;

	}


	private Vector3 CalcPenetrationAdjustment() {
		//Physics.ComputePenetration......
		return Vector3.zero;
	}


	private bool PerformStepUp() {
		return true;
	}


	private bool PerformWallSilde() {
		return true;
	}


	private bool PerformWalkup() {
		return true;
	}


}
	