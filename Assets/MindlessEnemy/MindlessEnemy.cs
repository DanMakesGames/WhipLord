using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MindlessEnemy : PawnController {
	
	GameObject player;

	public int state = 0;
	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player");


			MoveCont = GetComponent<MovementController> ();
			MoveCont.CastLayerMask = 1 << 9;
			MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 10;
			MoveCont.CastLayerMask = MoveCont.CastLayerMask | 1 << 11;
			MoveCont.CastLayerMask = ~MoveCont.CastLayerMask;

		
	}
	
	// Update is called once per frame
	void Update () {
		if (state == 0) {
			Vector3 heading = player.transform.position - transform.position;
			heading.y = 0;
			heading.Normalize ();


			//Quaternion newRot =  Quaternion.Euler (new Vector3 (0,Vector3.Angle(Vector3.forward, heading),0)) ;
			Vector3 newRot =  Quaternion.FromToRotation (Vector3.forward, heading).eulerAngles;
			transform.rotation =Quaternion.Euler( new Vector3(0,newRot.y,0));
			//MoveCont.AddRotationInput (newRot);

			heading = Quaternion.Inverse (transform.rotation) * heading;
			MoveCont.AddMovementInput (heading);
		}
		else if (state == 1) {
			MoveCont.AddMovementInput (new Vector3(0,0,-1));
		}


	}

	public override void Hurt (float damage, Vector3 impact)
	{
		if (state == 0) {
			StartCoroutine (StartKnockbackState (impact));
		}
	}

	IEnumerator StartKnockbackState(Vector3 impact) {
		state = 1;
		yield return new WaitForSeconds (3);
		state = 0;
	}

}
