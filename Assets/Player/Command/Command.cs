using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommandSpace
{
	public delegate void StateFunction(CommandState state, Command command, int stateFrame, int commandFrame);
	 
	public struct CommandState {
		/**
		 * AUTO states automatically progress to the next state after int frameLength is met.
		 * STOP states rely on the delegate function to tell them when to progress to the next state.
		 */
		public enum STATE_TYPE { STOP,  AUTO};
		public STATE_TYPE stateType;
		public int frameLength;
		public int nextState;



		public StateFunction stateFunction;

		// A reference to the created object is kept so that it can be destroid latter.
		GameObject commandGameObject;
		public GameObject CommandGameObject{ 
			get{
				return commandGameObject;
			} 
		}

		// Array of stateBoxes that defines the hit/hurt/other boxes of this current state. 
		public StateBox[] stateBoxArray;
	

		public CommandState(int inFrameLength, int inNextState, StateFunction inFunction) {
			stateType = STATE_TYPE.AUTO;
			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = inFunction;
			commandGameObject = null;
			stateBoxArray = null;
		}
		public CommandState(StateFunction inFunction) {
			stateType = STATE_TYPE.STOP;
			frameLength = 0;
			nextState = 0;
			stateFunction = inFunction;
			commandGameObject = null;
			stateBoxArray = null;
		}

		public CommandState(int inFrameLength, int inNextState) {
			stateType = STATE_TYPE.AUTO;
			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = null;
			commandGameObject = null;
			stateBoxArray = null;
		}


		// Creates a game object from the stateBoxArray
		public GameObject GenerateGameObject(Vector3 basePosition, Quaternion baseRotation) {
			if (stateBoxArray == null)
				return null;
			GameObject returnObject = new GameObject ("CommandGameObject");
			for( int index = 0; index < stateBoxArray.Length; index++) {
				// Create the collision object
				GameObject stateObject = stateBoxArray [index].CreateGameObject ();

				// Set the parent of the collision object to returnObject
				stateObject.transform.position = returnObject.transform.position;

				stateObject.transform.SetParent (returnObject.transform);
				stateObject.transform.localPosition = stateBoxArray [index].LocalPosition;
				stateObject.transform.localRotation = stateBoxArray [index].LocalRotation;

				// Set the local position and rotation of the new collision object

			}

			commandGameObject = returnObject;
			return returnObject;
		}

		public void DestroyGameObject() {
			if (commandGameObject == null)
				return;
			UnityEngine.Debug.Log ("Destroy GameObject: " + commandGameObject.name);
			GameObject.Destroy (commandGameObject);
		}
	}


	public abstract class StateBox {
		public string tag;

		// Position of the Box in relation to the player and the player's rotation
		private Vector3 localPosition;
		public Vector3 LocalPosition { 
			get{ return localPosition; }
			set{ localPosition = value; }
		}

		private Quaternion localRotation;
		public Quaternion LocalRotation { get; set; }

		private Vector3 size;
		public Vector3 Size { 
			get{
				return size;
			}
			set{ size = value; }
		}	

		public abstract void OnTrigger(Collider hit);

		public GameObject CreateGameObject() {
			GameObject returnObject = (GameObject) GameObject.Instantiate (Resources.Load("HitBox"));
			returnObject.GetComponent<TriggerPasser> ().DestinationStateBox = this;
			//UnityEngine.Debug.Log ("size: " + size);
			returnObject.GetComponent<BoxCollider> ().size = size;
			//UnityEngine.Debug.Log ("boxsize: " + returnObject.GetComponent<BoxCollider> ().size); 

			return returnObject;
		}
	}

	// Damage Doing Box
	public class HitBox : StateBox {
		private float damage;
	
		public HitBox(Vector3 inLocalPosition, Quaternion inLocalRotation, Vector3 inSize, String inTag, float inDamage) {
			LocalPosition = inLocalPosition;
			LocalRotation = inLocalRotation;
			Size = inSize;
			tag = String.Copy (inTag);
			damage = inDamage;

		}
		public HitBox(Vector3 inLocalPosition, Vector3 inSize, String inTag, float inDamage) {
			LocalPosition = inLocalPosition;
			LocalRotation = Quaternion.identity;
			Size = inSize;
			tag = String.Copy (inTag);
			damage = inDamage;

		}

		public override void OnTrigger(Collider hit) {
			UnityEngine.Debug.Log ("HitBoxHit");
		}
	}


	public abstract class Command
	{
		protected CommandState[] cmdStates;
		protected CommandController owner;
		public CommandController Owner {
			get {
				return owner;
			}
		}

		// If True, then the command has ended.
		private bool bTerminated = false;

		// Frames completed since start of command
		private int commandFrames = 0;
		// Frames completed since start of current state. At inital state, stateFrames is 0.
		private int stateFrames = 0;

		private int currentState = 0;
		public int CurrentState {
			get { 
				return currentState;
			}
			set {
				
				cmdStates [currentState].DestroyGameObject ();
				stateFrames = 0;
				currentState = value;
			}
		}

		public bool Initialize(CommandController inOwner) {
			if (owner == null) {
				owner = inOwner;
			}
			return CanCommandExecute ();
		}


		public abstract bool CanCommandExecute ();

		/**
		 * This function is called by the CommandController. It ticks the move, counting of one frame worth of 
		 * calculation. It calls the current state's stateFunction, calls the 
		 */
		public void Tick() {
			if (cmdStates [currentState].CommandGameObject == null) {

				GameObject stateObject = cmdStates [currentState].GenerateGameObject (Owner.transform.position, Owner.transform.rotation);
				if (stateObject != null) {
					stateObject.transform.SetParent (Owner.transform);
					stateObject.transform.localPosition = new Vector3 (0, 0, 0);
					stateObject.transform.localRotation = Quaternion.identity;
					//Physics.IgnoreCollision (owner.gameObject,stateObject, true);
				}
					
			}
			if (cmdStates [currentState].CommandGameObject != null) {
				//cmdStates[currentState].CommandGameObject
			}

			if(cmdStates [currentState].stateFunction != null)
				cmdStates [currentState].stateFunction (cmdStates[CurrentState], this,stateFrames,commandFrames);

			stateFrames++;
			commandFrames++;

			if (cmdStates[CurrentState].stateType == CommandState.STATE_TYPE.AUTO && stateFrames >= cmdStates [CurrentState].frameLength) {
				CurrentState = cmdStates [CurrentState].nextState;
			}

		}


		public void Terminate() {
			bTerminated = true;
		}


		public bool IsTerminated() {
			return bTerminated;
		}
	}


	public class WhipCommand : Command {
		
		public WhipCommand() {
			cmdStates = new CommandState[10];
			//cmdStates[0] = new CommandState();

			cmdStates [0].stateFunction = whipStartUp;
			cmdStates [1].stateFunction = whipActive;
			//cmdStates[1].stateBoxArray = new HitBox(new Vector3(0,0,10),Quaternion.identity,new Vector3(10,10,10),"",10)};
			cmdStates[1].stateBoxArray = new StateBox[1];
			cmdStates [1].stateBoxArray [0] = new HitBox (new Vector3 (0, 0, 0), Quaternion.identity, new Vector3 (1, 1, 1), "", 10);

			cmdStates [2] = new CommandState (10, 3);
			cmdStates [2].stateBoxArray = new StateBox [] { new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10) };
			//cmdStates [2].stateBoxArray =
			cmdStates [3] = new CommandState (10, 4);
			cmdStates [3].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
														new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10) };

			cmdStates [4] = new CommandState (10, 5);
			cmdStates [4].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 3), new Vector3 (1, 1, 1), "", 10) };
			
			cmdStates [5] = new CommandState (10, 6);
			cmdStates [5].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 3), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 4), new Vector3 (1, 1, 1), "", 10)};
			
			cmdStates [6] = new CommandState (10, 7);
			cmdStates [6].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 3), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 4), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 5), new Vector3 (1, 1, 1), "", 10)};

			cmdStates [7] = new CommandState (10, 8);
			cmdStates [7].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 3), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 4), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 5), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 6), new Vector3 (1, 1, 1), "", 10)};
			
			cmdStates [8] = new CommandState (10, 9);
			cmdStates [8].stateBoxArray = new StateBox [] {new HitBox (new Vector3 (0, 0, 1), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 2), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 3), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 4), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 5), new Vector3 (1, 1, 1), "", 10),
				new HitBox (new Vector3 (0, 0, 6), new Vector3 (1, 1, 1), "", 10)};

			//cmdStates [4].stateBoxArray.



			cmdStates [9].stateFunction = whipEnd;

		}

		public override bool CanCommandExecute() {
			if (owner.CurrentCmd != null) {
				if (owner.CurrentCmd.CurrentState == 2)
					return true;
				else
					return false;
			}

			return true;
		}


		static void whipStartUp(CommandState state, Command command, int stateFrame, int commandFrame) {
			
			//command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,10));
			//command.Owner.Instantiate (command.Owner);
			//GameObject.Instantiate(command.Owner.gameObject);
			//UnityEngine.Debug.Log("frame: " + stateFrame);
			if ((stateFrame + 1) >= 10) {
				command.CurrentState = 1;
				return;
			}
			return;

				
		}

		static void whipActive(CommandState state, Command command, int stateFrame, int commandFrame) {
			if (stateFrame > 10) {
				command.CurrentState = 2;
				return;
			}
		}
		static void whipEnd(CommandState state, Command command, int stateFrame, int commandFrame) {
			if (stateFrame > 60) {
				command.Terminate ();
				//command.CurrentState = 2;
				return;
			}
			//command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,-5));
		}

	}
		

}

