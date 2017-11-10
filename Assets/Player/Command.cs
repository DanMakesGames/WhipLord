using System;

namespace CommandSpace
{
	public delegate void StateFunction(CommandState state, Command command, int stateFrame, int commandFrame);

	public struct CommandState {
		public StateFunction stateFunction;
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

		/**
		 * THis is used by the character to create a command instance of this commands type. This function also keeps track
		 * of the requirements that must be fufilled by the calling character in order to start this move. For instance 
		 * if this move can cancel a current move, or if the player must be standing still, or if the player must be WALKING
		 */
		//public abstract Command CreateCommand();

		/**
		 * This function determines if this Command should execute. Can be called individually or recieved in return of 
		 * Initialize. 
		 */
		public abstract bool CanCommandExecute ();

		/**
		 * This function is called by the CommandController. It ticks the move, counting of one frame worth of 
		 * calculation. It calls the current state's stateFunction, calls the 
		 */
		public void Tick() {
			cmdStates [currentState].stateFunction (cmdStates[CurrentState], this,stateFrames,commandFrames);
			stateFrames++;
			commandFrames++;
		}

		public void Terminate() {
			bTerminated = true;
		}

		public bool IsTerminated() {
			return bTerminated;
		}
	}


	public class WhipCommand : Command {
		/*
		public override Command CreateCommand() {
			
			WhipCommand newCmd = new WhipCommand();
			newCmd.Initialize (owner);
			return newCmd;
		}
		*/
		public WhipCommand() {
			cmdStates = new CommandState[3];
			cmdStates[0] = new CommandState();
			cmdStates [0].stateFunction = whipStartUp;
			cmdStates [1].stateFunction = whipActive;
			cmdStates [2].stateFunction = whipEnd;
		}

		public override bool CanCommandExecute() {
			//if(owner.CurrentCmd != null )
			//	return false;
			if (owner.CurrentCmd != null) {
				if (owner.CurrentCmd.CurrentState == 2)
					return true;
				else
					return false;
			}
				

			return true;
		}


		static void whipStartUp(CommandState state, Command command, int stateFrame, int commandFrame) {
			if (stateFrame > 10) {
				command.CurrentState = 1;
				return;
			}
			command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,10));
				
		}

		static void whipActive(CommandState state, Command command, int stateFrame, int commandFrame) {
			if (stateFrame > 10) {
				command.CurrentState = 2;
				return;
			}
			//command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,0));

		}
		static void whipEnd(CommandState state, Command command, int stateFrame, int commandFrame) {
			if (stateFrame > 60) {
				command.Terminate ();
				//command.CurrentState = 2;
				return;
			}
			command.Owner.MoveCont.AddMovementInput (new UnityEngine.Vector3(0,0,-5));

		}

	}
		

}

