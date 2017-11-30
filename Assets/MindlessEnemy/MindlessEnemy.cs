using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandSpace;


public class MindlessEnemy : CommandController {
	
	GameObject player;
	CommandController playerCommand;

	const float maxOutBoxRange = 10;

	// 6 so i can let you make the mistake of coming in and so i can make the mistake of fuckin up.
	const float minOutBoxrange = 6;

	const float minDistToRopes = 8;
	const float escapeAngle = 120;

	const int framesBeforeCounter = 13;
	int framesSinceAttack = 0;

	enum ESCAPE_HOME {NORTH, SOUTH, EAST, WEST};
	ESCAPE_HOME escHome;


	enum AI_STATE { APPROACH, RETREAT, OUTBOX, MOVE_IN_ATTACK, MOVE_IN_FAKE_OUT, MOVE_IN_FAKE_OUT_R, ATTACK, STRAFE_ESCAPE}


	AI_STATE AIState = AI_STATE.APPROACH;
	float timeTillNextAction;
	float timePassed = 0;
	AI_STATE nextAction;

	int strafeDir = 0;
	Vector3 center;
	Vector3 startOffset;

	//Random random;

	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player");
		playerCommand = player.GetComponent<CommandController> ();
			
			MoveCont = GetComponent<MovementController> ();
			MoveCont.CastLayerMask = 1 << 9;
			MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 10;
			MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 11;
			MoveCont.CastLayerMask = ~MoveCont.CastLayerMask;

		//random = new Random (Time.time);

		
	}
	

	void FixedUpdate () {
		//Debug.Log ((player.transform.position - transform.position).magnitude);
		if(getHealth() <= 0)
		{
			Destroy (gameObject);
		}


		ProcessCommand ();
		if(player == null)
			player = GameObject.FindGameObjectWithTag ("Player");

		ProcessState ();

		Vector3 heading = player.transform.position - transform.position;
		heading.y = 0;
		heading.Normalize ();


		//Quaternion newRot =  Quaternion.Euler (new Vector3 (0,Vector3.Angle(Vector3.forward, heading),0)) ;

		Vector3 newRot = Quaternion.FromToRotation (Vector3.forward, heading).eulerAngles;
		transform.rotation = Quaternion.Euler (new Vector3 (0, newRot.y, 0));

		if (State == CHAR_STATE.NEUTRAL) {

			if (AIState == AI_STATE.APPROACH) {
				if ((player.transform.position - transform.position).magnitude <= (maxOutBoxRange + minOutBoxrange) / 2) {
					AIState = AI_STATE.OUTBOX;
				}
				else if (CanMove) {
					
					heading = Quaternion.Inverse (transform.rotation) * heading;
					MoveCont.AddMovementInput (heading);

				}
			}
			if (AIState == AI_STATE.RETREAT) {
				if ((player.transform.position - transform.position).magnitude > (maxOutBoxRange + minOutBoxrange) / 2) {
					AIState = AI_STATE.OUTBOX;
				} 
				else if (ShouldEscape ()) {
				} 
				else {
					if (CanMove) {
						heading = Quaternion.Inverse (transform.rotation) * heading;
						MoveCont.AddMovementInput (-heading);
					}
				}
			}




			if(AIState == AI_STATE.OUTBOX) {
				float distFromPlayer = (player.transform.position - transform.position).magnitude;

				if (distFromPlayer > maxOutBoxRange) {
					AIState = AI_STATE.APPROACH;
					timeTillNextAction = -1;
				} else if (distFromPlayer < minOutBoxrange) {
					AIState = AI_STATE.RETREAT;
					timeTillNextAction = -1;

				} 
				else if(playerCommand.getCurrentCommand() == "LongPoke") {
					framesSinceAttack++;

					if (framesSinceAttack > 10) {
						//Debug.Log ("DUDE");
						AIState = AI_STATE.MOVE_IN_ATTACK;
					}
				}
				else if (ShouldEscape ()) {
				}

				else {
					// pick next action to do and when to do it
					//MoveCont.AddMovementInput(new Vector3(1,0,0));

					if (timeTillNextAction == -1) {
						
						timeTillNextAction = 2f * Random.value;
						timePassed = 0;


						if (Random.value > 0.5f && distFromPlayer > 7 && distFromPlayer < 8.5) {
							nextAction = AI_STATE.MOVE_IN_FAKE_OUT;
						} else {
							nextAction = AI_STATE.MOVE_IN_ATTACK;
						}
						//nextAction = AI_STATE.MOVE_IN_FAKE_OUT;
						//nextAction = AI_STATE.MOVE_IN_ATTACK;
					}

					if(timePassed > timeTillNextAction){
						Debug.Log ("NextAction");
						AIState = nextAction;
						timeTillNextAction = -1;
					}

					//(6 + (Random.value - 0.5))
					if ((player.transform.position - transform.position).magnitude < 6 + Random.value ) {

						Command newCmd = new LongPoke ();
						if (newCmd.Initialize (this)) {
							newCmd.OnStart ();
							CurrentCmd = newCmd;
						}
					}
					timePassed += Time.deltaTime;
				}
			
			}


			if (AIState == AI_STATE.STRAFE_ESCAPE) {
				if (Vector3.Angle (startOffset, transform.position - player.transform.position) > escapeAngle) {
					AIState = AI_STATE.OUTBOX;
				} 




				else {
					ShouldEscape ();
					if (CanMove) {
						MoveCont.AddMovementInput (Vector3.right * strafeDir);
					}
					if ((player.transform.position - transform.position).magnitude < 7) {

						Command newCmd = new LongPoke ();
						if (newCmd.Initialize (this)) {
							newCmd.OnStart ();
							CurrentCmd = newCmd;
						}
					}
				}
			
			}

			if (AIState == AI_STATE.MOVE_IN_FAKE_OUT) {
				
				float distFromPlayer = (player.transform.position - transform.position).magnitude;

				if(playerCommand.getCurrentCommand() == "LongPoke") {
					framesSinceAttack++;

					if (framesSinceAttack > framesBeforeCounter) {
						//Debug.Log ("DUDE");
						AIState = AI_STATE.MOVE_IN_ATTACK;
					}
				}
				else if (distFromPlayer < 6) {
					AIState = AI_STATE.RETREAT;
				} 

				else {
					if (CanMove) {
						heading = Quaternion.Inverse (transform.rotation) * heading;
						MoveCont.AddMovementInput (heading);
					}
				}
			}

			if (AIState == AI_STATE.MOVE_IN_ATTACK) {
				Debug.Log ("ATTACKING");
				framesSinceAttack = 0;
				float distFromPlayer = (player.transform.position - transform.position).magnitude;

				if (distFromPlayer < 4) {
					AIState = AI_STATE.RETREAT;
				} else {
					if (CanMove) {
						heading = Quaternion.Inverse (transform.rotation) * heading;
						MoveCont.AddMovementInput (heading);
					}
					if ((player.transform.position - transform.position).magnitude < 6) {

						Command newCmd = new LongPoke ();
						if (newCmd.Initialize (this)) {
							newCmd.OnStart ();
							CurrentCmd = newCmd;
							AIState = AI_STATE.RETREAT;
						}
					}
				}
			
			}
			if (AIState == AI_STATE.MOVE_IN_FAKE_OUT_R) {

			}

			/*
			if ((player.transform.position - transform.position).magnitude < 5) {

				Command newCmd = new LongPoke ();
				if (newCmd.Initialize (this)) {
					newCmd.OnStart ();
					CurrentCmd = newCmd;
				}
			}
			*/

		}

			
	}

	public override void Hurt (float damage, Vector3 impact)
	{
		base.Hurt (damage, impact);
		if (State == CHAR_STATE.NEUTRAL || State == CHAR_STATE.HIT_STUN)
			setHealth(getHealth() - damage);
		Debug.DrawRay (transform.position, Vector3.up, Color.green);
	}


	protected override void OnStartHitStun ()
	{
		base.OnStartHitStun ();
		AIState = AI_STATE.RETREAT;
	}

	private bool ShouldEscape() {
		
		if (transform.position.x > (20 - minDistToRopes)) {
			if (player.transform.position.z >= transform.position.z) {
				strafeDir = -1;
			} else
				strafeDir = 1;
			escHome = ESCAPE_HOME.EAST;
			AIState = AI_STATE.STRAFE_ESCAPE;
			center = player.transform.position;
			startOffset = transform.position - center;
			return true;
		}
		else if (transform.position.x < (-20 + minDistToRopes)) {
			if (player.transform.position.z >= transform.position.z) {
				strafeDir = 1;
			} else
				strafeDir = -1;
			escHome = ESCAPE_HOME.WEST;
			AIState = AI_STATE.STRAFE_ESCAPE;
			center = player.transform.position;
			startOffset = transform.position - center;
			return true;
		}

		else if (transform.position.z > (20 - minDistToRopes)) {
			if (player.transform.position.x >= transform.position.x) {
				strafeDir = 1;
			} else
				strafeDir = -1;

			escHome = ESCAPE_HOME.NORTH;
			AIState = AI_STATE.STRAFE_ESCAPE;
			center = player.transform.position;
			startOffset = transform.position - center;
			return true;
		}
		else if (transform.position.z < (-20 + minDistToRopes)) {
			if (player.transform.position.x >= transform.position.x) {
				strafeDir = -1;
			} else
				strafeDir = 1;

			escHome = ESCAPE_HOME.SOUTH;
			AIState = AI_STATE.STRAFE_ESCAPE;
			center = player.transform.position;
			startOffset = transform.position - center;
			return true;
		}
		return false;
	}
}
