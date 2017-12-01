/**
 * Written by Daniel Mann.
 * created in 2017
 * DanielMannGames@outlook.com
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/**
 * The Command System:
 * This system implements fighting game sytle hitbox and frame defined moves. It is built upon a layer system. On the outer
 * layer is the Command object. This object is what is interacted with by command using characters. It is compossed of many 
 * commandStates. Each command state is roughly equivalent to a frame, or a block of frames. Each CommandState can have a functio attached
 * that is ticked by the Command object, and provides state based logic. States also are composed of many StateBoxes, which are hurt
 * boxes, hit boxes, or any sort of box. These define the in world gemometric description of the move. This system was created
 * based of the functionality of fighting games.
 * 
 * Commands basically define a state machine. They have power equal to that. 
 * 
 * Command using characters should be children of CommandCharacter, although they dont need to be.
 * 
 */

namespace CommandSpace
{
	public delegate void StateFunction(CommandState state, Command command, int stateFrame, int commandFrame);
	 

	/**
	 * Defines a state in the Command state machine. Can have attached function that is called every tick, or can simply
	 * run through a set of frames and transition to the next state. CommandStates are also composed of hitboxes which define
	 * the Command's geometry durring this state.
	 */
	public struct CommandState {
		/**
		 * AUTO states automatically progress to the next state after int frameLength is met.
		 * STOP states rely on the delegate function to tell them when to progress to the next state.
		 * TERM states are like auto, but instead of transitioning to a new state they terminate the command after the perscibed frames.
		 */
		public enum STATE_TYPE { STOP, AUTO, TERM };
		public STATE_TYPE stateType;

		// If using AUTO or TERM, this is how many frames the state will last.
		public int frameLength;

		// This is the next state to transition to if using AUTO.
		public int nextState;

		// Sound to use when this State's hit boxes collide with something.
		public AudioClip hitSound; 

		// This is the delgate to this States tick.
		public StateFunction stateFunction;

		// A reference to the created object is kept so that it can be destroid latter.
		GameObject commandGameObject;
		public GameObject CommandGameObject{ 
			get{ return commandGameObject; } 
		}

		// List of enemies we have already hit so far. This is used to ignore actors so one move doesnt hit more than once.
		LinkedList<GameObject> alreadyHit;
		public bool ShouldHit(GameObject obj) {
			if (alreadyHit.Find (obj) != null)
				return false;
			return true;
		}
		public void AddAlreadyHit(GameObject obj) {
			alreadyHit.AddFirst (obj);
		}
		public void ClearAlreadyHit(){
			alreadyHit.Clear ();
		}

		// Hashtable of stateBoxes that defines the hit/hurt/other boxes of this current state. Must be a hash table so specific
		// hitboxes can be accessed in order to be removed after being coppied over.
		Hashtable boxHashtable;


		// Collection of constructors.

		public CommandState( int inFrameLength, int inNextState, StateFunction inFunction, string inSoundName = null, bool bTerm = false) {
			stateType = STATE_TYPE.AUTO;
			if (bTerm)
				stateType = STATE_TYPE.TERM;
			
			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = inFunction;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
			alreadyHit = new LinkedList<GameObject> ();
			if (inSoundName != null) {
				hitSound = Resources.Load<AudioClip> (inSoundName);
			} else
				hitSound = null;
		}

		public CommandState( StateFunction inFunction, string inSoundName = null) {
			stateType = STATE_TYPE.STOP;
			frameLength = 0;
			nextState = 0;
			stateFunction = inFunction;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
			alreadyHit = new LinkedList<GameObject> ();
			if (inSoundName != null) {
				hitSound = Resources.Load<AudioClip> (inSoundName);
			} else
				hitSound = null;
		}

		public CommandState( int inFrameLength, int inNextState, string inSoundName = null, bool bTerm = false) {
			stateType = STATE_TYPE.AUTO;
			if (bTerm)
				stateType = STATE_TYPE.TERM;

			frameLength = inFrameLength;
			nextState = inNextState;
			stateFunction = null;
			commandGameObject = null;
			boxHashtable = new Hashtable ();
			alreadyHit = new LinkedList<GameObject> ();

			if (inSoundName != null) {
				hitSound = Resources.Load<AudioClip> (inSoundName);
			} else
				hitSound = null;

		}
			
		/**
		 * Adds a hit box onto the state
		 */
		public void AddBox(StateBox inBox) {
			inBox.State = this;
			boxHashtable.Add (inBox.tag, inBox);
		}

		/**
		 * Removes a hitbox by name.
		 */
		public void RemoveBox(string removeTag) {
			boxHashtable.Remove (removeTag);
		}


		/** 
		 * This is important. States are defined with this compy ability so that the hit boxes can be carried over from 
		 * state to state. You most likely  will copy over every new 
		 */
		public void CopyOverBoxes(CommandState otherState) {
			boxHashtable = (Hashtable) otherState.boxHashtable.Clone ();
		}


		// Creates a game object from the stateBoxArray
		public GameObject GenerateGameObject(Vector3 basePosition, Quaternion baseRotation) {
			if (boxHashtable.Count == 0)
				return null;
			
			GameObject returnObject = new GameObject ("CommandGameObject");

			// loop through every state box and Create a new state box and attach to a containg gameobject.
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

		/**
		 * Destroys the gameObject created by GenerateGameObject.
		 */
		public void DestroyGameObject() {
			if (commandGameObject == null)
				return;
			
			GameObject.Destroy (commandGameObject);
		}
	}


	/**
	 * This defines a gemometry of the state. Can generate a GameObject representation and responds when that 
	 * representation is touched.
	 */
	public abstract class StateBox {
		// Identity tag. Used as the key when stored in CommandState.
		public string tag;

		// parent commandState
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

		// Rotation of the box in realtion to the command user's rotation.
		private Quaternion localRotation;
		public Quaternion LocalRotation { get; set; }

		// Defines the size of the box.
		private Vector3 size;
		public Vector3 Size { 
			get{
				return size;
			}
			set{ size = value; }
		}	

		/**
		 * This function is attached to the gameObject generated by CreateGameObject. Called when that actor is triggered.
		 */
		public abstract void OnTrigger(Collider hit);

		/**
		 * Generates a gameObject with a collider based on this objects specifications.
		 */
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


		/**
		 * On trigger 
		 */
		public override void OnTrigger(Collider hit) {

			//Only hit if this object has not already been hit. This is done so a move only hits once per state.
			if (!State.ShouldHit (hit.gameObject)) {
				return;
			}

			PawnController enemyController = hit.gameObject.GetComponent<PawnController> ();

			if (enemyController == null) {
				return;
			}

			// Play hit sound.
			GameObject soundObject = GameObject.Instantiate (Resources.Load<GameObject> ("SoundObject"), hit.transform.position, hit.transform.rotation);
			soundObject.GetComponent<AudioSource> ().clip = State.hitSound;

			// Create hitspark.
			GameObject.Instantiate (Resources.Load<GameObject> ("HitSpark"), hit.transform.position, hit.transform.rotation);

			// Hurt opponent.
			enemyController.Hurt (damage, Vector3.zero);

			// Add this to the list of already hit GameObjects.
			State.AddAlreadyHit (hit.gameObject);

		}
	}

	/**
	 * Defines and encapsulates the State Machine that is a command move. Composed of CommandState objects this provides the
	 * interaction with Command users.
	 */
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

		// This is the current state that the command is in.
		private int currentState = 0;
		public int CurrentState {
			get { 
				return currentState;
			}
			set {
				// Setting the commandState cleans up the boxes GameObject created by the last state.
				cmdStates [currentState].DestroyGameObject ();

				// Reset the state frame count.
				stateFrames = 0;
				currentState = value;
			}
		}

		// Name of sound to use for hits. THis is passed to COmmandStates on construction.
		protected string hitSoundName;

		/**
		 * Called by Command Object users. Sets up the command and tells the caller if the command can run.
		 */
		public bool Initialize(CommandController inOwner) {
			if (owner == null) {
				owner = inOwner;
			}

			return CanCommandExecute ();
		}


		public virtual void OnStart() {}


		/**
		 * Determines when the command can be called. This can be used to define move canceling, and allows for chain combos.
		 */
		protected abstract bool CanCommandExecute ();


		/**
		 * This function is called by the CommandController. It ticks the move, counting of one frame worth of 
		 * calculation. It calls the current state's stateFunction, calls the 
		 */
		public void Tick() {

			// If this is the first time tick has been run for this state. 
			if (cmdStates [currentState].CommandGameObject == null) {

				//Generate thehit/hurtboxes.
				GameObject stateObject = cmdStates [currentState].GenerateGameObject (Owner.transform.position, Owner.transform.rotation);

				if (stateObject != null) {
					
					stateObject.transform.SetParent (Owner.transform);
					stateObject.transform.localPosition = new Vector3 (0, 0, 0);
					stateObject.transform.localRotation = Quaternion.identity;

					//Set these hitboxes to not hit the owning character.
					Collider[] stateColliders = stateObject.GetComponentsInChildren<Collider> ();
					Collider[] ownerColliders = Owner.gameObject.GetComponentsInChildren<Collider> ();
					for (int index0 = 0; index0 < stateColliders.Length; index0++) {
					
						for (int index1 = 0; index1 < ownerColliders.Length; index1++) {
							Physics.IgnoreCollision (stateColliders [index0], ownerColliders [index1]);
						}
					}
				}
			}

			// Run the current State's function.
			if(cmdStates [currentState].stateFunction != null)
				cmdStates [currentState].stateFunction (cmdStates[CurrentState], this,stateFrames,commandFrames);

			// Increment frame counts.
			stateFrames++;
			commandFrames++;

			// Move to next state if this is a AUTO state and the proper number of frames have passed.
			if (cmdStates[CurrentState].stateType == CommandState.STATE_TYPE.AUTO && stateFrames >= cmdStates [CurrentState].frameLength) {
				CurrentState = cmdStates [CurrentState].nextState;
			}
			// terminate this command if the proper number of frames have passed and and this is a TERM state.
			else if (cmdStates[CurrentState].stateType == CommandState.STATE_TYPE.TERM && stateFrames >= cmdStates [CurrentState].frameLength ) {
				Terminate();
			}

		}

		// Sets this Command to be terminated.
		public void Terminate() {
			bTerminated = true;
			OnTerminate ();
		}

		protected virtual void OnTerminate() {}

		// Returns if this Command has been terminated.
		public bool IsTerminated() {
			return bTerminated;
		}
	}


	/**
	 * Bellow is an example of how you would implement a move using the command system.
	 */

	public class LongPoke : Command {
		static float range = 6;

		public LongPoke() {
			commandID = "LongPoke";
			cmdStates = new CommandState[3];
			//Startup
			cmdStates[0] = new CommandState(9, 1);

			//cmdStates [1] = new CommandState (1, 2, WhipSound);
			//Active
			cmdStates[1] = new CommandState(4, 2,"HitSound");
			cmdStates [1].AddBox (new HitBox(new Vector3(0,0,range / 2),new Vector3(1,1,range),"pokeBox",10));

			//Recovery 14
			cmdStates [2] = new CommandState (30, -1,null, true);
		}

		public override void OnStart ()
		{
			Owner.CanMove = false;
			Animator anim = Owner.GetComponent<Animator> ();
			if (anim != null) {
				anim.SetTrigger ("Whip");
			}
			AudioSource makeSound = Owner.GetComponent<AudioSource> ();
			if( makeSound != null) {
				makeSound.Play ();
			}
		}

		protected override void OnTerminate ()
		{
			Owner.CanMove = true;
		}

		static void WhipSound(CommandState state, Command command, int stateFrame, int commandFrame) {
			AudioSource makeSound = command.Owner.GetComponent<AudioSource> ();
			if( makeSound != null) {
			//	makeSound.Play ();
			}
		}

	
		protected override bool CanCommandExecute() {
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




}

