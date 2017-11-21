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
		 * TERM states are like auto, but instead of transitioning to a new state they terminate the command after the perscibed frames.
		 */
		public enum STATE_TYPE { STOP, AUTO, TERM };
		public STATE_TYPE stateType;
		public int frameLength;
		public int nextState;


		//public 

		public StateFunction stateFunction;

		// A reference to the created object is kept so that it can be destroid latter.
		GameObject commandGameObject;
		public GameObject CommandGameObject{ 
			get{ return commandGameObject; } 
		}

		// Array of stateBoxes that defines the hit/hurt/other boxes of this current state. 
		// public StateBox[] stateBoxArray;
		Hashtable boxHashtable;

		public CommandState( int inFrameLength, int inNextState, StateFunction inFunction, bool bTerm = false) {
			stateType = STATE_TYPE.AUTO;
			if (bTerm)
				stateType = STATE_TYPE.TERM;
			
			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = inFunction;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
		}

		public CommandState( StateFunction inFunction) {
			stateType = STATE_TYPE.STOP;
			frameLength = 0;
			nextState = 0;
			stateFunction = inFunction;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
		}

		public CommandState( int inFrameLength, int inNextState, bool bTerm = false) {
			stateType = STATE_TYPE.AUTO;
			if (bTerm)
				stateType = STATE_TYPE.TERM;

			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = null;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
		}
			

		public void AddBox(StateBox inBox) {
			inBox.State = this;
			boxHashtable.Add (inBox.tag, inBox);
		}

		public void RemoveBox(string removeTag) {
			boxHashtable.Remove (removeTag);
		}

		public void CopyOverBoxes(CommandState otherState) {
			boxHashtable = (Hashtable) otherState.boxHashtable.Clone ();
		}

		// Creates a game object from the stateBoxArray
		public GameObject GenerateGameObject(Vector3 basePosition, Quaternion baseRotation) {
			if (boxHashtable.Count == 0)
				return null;
			
			GameObject returnObject = new GameObject ("CommandGameObject");

			foreach (DictionaryEntry entry in boxHashtable) {
				GameObject stateObject = ((StateBox)entry.Value).CreateGameObject ();
				stateObject.transform.position = returnObject.transform.position;
				stateObject.transform.SetParent (returnObject.transform);
				stateObject.transform.localPosition = ((StateBox)entry.Value).LocalPosition;
				stateObject.transform.localRotation = ((StateBox)entry.Value).LocalRotation;

			}
				
			commandGameObject = returnObject;
			return returnObject;
		}

		public void DestroyGameObject() {
			if (commandGameObject == null)
				return;
			//UnityEngine.Debug.Log ("Destroy GameObject: " + commandGameObject.name);
			GameObject.Destroy (commandGameObject);
		}
	}


	public abstract class StateBox {
		public string tag;

		private CommandState state;
		public CommandState State {
			get { return state; }
			set { state = value;}
		}
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

			returnObject.GetComponent<BoxCollider> ().size = size;
			 
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
		public HitBox( Vector3 inLocalPosition, Vector3 inSize, String inTag, float inDamage) {
			LocalPosition = inLocalPosition;
			LocalRotation = Quaternion.identity;
			Size = inSize;
			tag = String.Copy (inTag);
			damage = inDamage;

		}

		public override void OnTrigger(Collider hit) {
			UnityEngine.Debug.Log ("HitBoxHit");
			PawnController enemyController = hit.gameObject.GetComponent<PawnController> ();
			//if(enemyController.gameObject == State.)

			if (enemyController == null) {
				return;
			}

			enemyController.Hurt (damage, Vector3.zero);

		}
	}


	public abstract class Command
	{
		public string commandID = "none";
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

		public virtual void OnStart() {}

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
					Collider[] stateColliders = stateObject.GetComponentsInChildren<Collider> ();
					Collider[] ownerColliders = Owner.gameObject.GetComponentsInChildren<Collider> ();
					for (int index0 = 0; index0 < stateColliders.Length; index0++) {
					
						for (int index1 = 0; index1 < ownerColliders.Length; index1++) {
							Physics.IgnoreCollision (stateColliders [index0], ownerColliders [index1]);
						}
					}


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
			else if (cmdStates[CurrentState].stateType == CommandState.STATE_TYPE.TERM && stateFrames >= cmdStates [CurrentState].frameLength ) {
				Terminate();
			}

		}


		public void Terminate() {
			bTerminated = true;
			OnTerminate ();
		}

		protected virtual void OnTerminate() {}

		public bool IsTerminated() {
			return bTerminated;
		}
	}

	public class KickCommand : Command {
		
		public KickCommand() {
			commandID = "Kick";
			cmdStates = new CommandState[3];
			cmdStates [0] = new CommandState (4, 1);
			cmdStates [1] = new CommandState (4, 2);
			cmdStates [1].AddBox (new HitBox(new Vector3(0,0,1.5f), new Vector3(2,1,2),"1",20));
			cmdStates [2] = new CommandState (11, -1, true);


		}
		public override bool CanCommandExecute ()
		{
			
			if (Owner.CurrentCmd != null) {
				if (Owner.CurrentCmd.commandID.Equals ("Whip") ) {
					if(Owner.CurrentCmd.CurrentState == 7)
						return true;
					else
						return false;
				} 
				if (Owner.CurrentCmd.commandID.Equals ("Kick")) {
					return false;
				}
			}
			return true;
		}
	}

	public class LongPoke : Command {
		static float range = 6;

		public LongPoke() {
			commandID = "LongPoke";
			cmdStates = new CommandState[3];
			//Startup
			cmdStates[0] = new CommandState(9,1);

			//Active
			cmdStates[1] = new CommandState(4,2, WhipCommand.drawWhip);
			cmdStates [1].AddBox (new HitBox(new Vector3(0,0,range / 2),new Vector3(1,1,range),"pokeBox",10));

			//Recovery 14
			cmdStates [2] = new CommandState (30, -1, true);
		}

		public override void OnStart ()
		{
			Owner.CanMove = false;
			Animator anim = Owner.GetComponent<Animator> ();
			if (anim != null) {
				anim.SetTrigger ("Whip");
			}
		}

		protected override void OnTerminate ()
		{
			Owner.CanMove = true;
		}
		/*
		static void Start(CommandState state, Command command, int stateFrame, int commandFrame) {
			command.Owner.CanMove = false;
		}

		static void End(CommandState state, Command command, int stateFrame, int commandFrame) {
			command.Owner.CanMove = true;
		}
		*/

		public override bool CanCommandExecute() {
			if (Owner.CurrentCmd == null) {
				return true;
			}

			switch (Owner.CurrentCmd.commandID) {
			case "LongPoke":
				return false;
			case "Kick":
				return false;
			}

			return true;
		}
	}

	public class WhipCommand : Command {
		
		public WhipCommand() {
			commandID = "Whip";
			cmdStates = new CommandState[8];
			// Start up
			cmdStates [0] = new CommandState (10, 1);

			cmdStates [1] = new CommandState (2, 2,drawWhip);
			cmdStates [1].AddBox (new HitBox(new Vector3(0,0,0),Quaternion.identity,new Vector3(1,1,1),"1",10));

			cmdStates [2] = new CommandState (2, 3, drawWhip);
			cmdStates [2].CopyOverBoxes (cmdStates[1]);
			cmdStates [2].AddBox (new HitBox(new Vector3(0,0,1),Quaternion.identity,new Vector3(1,1,1),"2",10));

			cmdStates [3] = new CommandState (2, 4, drawWhip);
			cmdStates [3].CopyOverBoxes (cmdStates[2]);
			cmdStates [3].AddBox (new HitBox(new Vector3(0,0,2),Quaternion.identity,new Vector3(1,1,1),"3",10));

			cmdStates [4] = new CommandState (2, 5, drawWhip);
			cmdStates [4].CopyOverBoxes (cmdStates[3]);
			cmdStates [4].AddBox (new HitBox(new Vector3(0,0,3),Quaternion.identity,new Vector3(1,1, 1),"4",10));

			cmdStates [5] = new CommandState ( 2, 6,drawWhip);
			cmdStates [5].CopyOverBoxes (cmdStates[4]);
			cmdStates [5].AddBox (new HitBox(new Vector3(0,0,4),Quaternion.identity,new Vector3(1,1,1),"5",10));

			cmdStates [6] = new CommandState (2, 7, drawWhip);
			cmdStates [6].CopyOverBoxes (cmdStates[5]);
			cmdStates [6].AddBox (new HitBox(new Vector3(0,0,5),Quaternion.identity,new Vector3(1,1,1),"6",10));

			cmdStates [7] = new CommandState (whipEnd);
		}

		public override bool CanCommandExecute() {
			if (owner.CurrentCmd != null) {
				if (owner.CurrentCmd.CurrentState == 7)
					return true;
				else
					return false;
			}

			return true;
		}

		 public static void drawWhip(CommandState state, Command command, int stateFrame, int commandFrame) {
			Debug.DrawLine (command.Owner.transform.position, command.Owner.transform.position + command.Owner.transform.rotation * new Vector3 (0, 0, 5.5f), Color.red);
		}

		static void whipStartUp(CommandState state, Command command, int stateFrame, int commandFrame) {
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
			Debug.Log ("IN: " + stateFrame);
			if (stateFrame > 17) {
				command.Terminate ();
				//command.CurrentState = 2;
				return;
			}
			//command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,-5));
		}

	}
		

}

