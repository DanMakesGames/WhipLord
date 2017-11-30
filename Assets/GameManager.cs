using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	PlayerController playerController;
	CommandController enemyController;
	bool bGameOver = false;

	// Use this for initialization
	void Start () {
		playerController = GameObject.FindGameObjectWithTag ("Player").GetComponent<PlayerController> ();
		enemyController = GameObject.FindGameObjectWithTag ("Enemy").GetComponent<CommandController> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (!bGameOver) {
			Debug.Log (playerController.getHealth () + " :: " + enemyController.getHealth ());
			if (playerController.getHealth () <= 0) {
				StartCoroutine (EndGame (false));
			} else if (enemyController.getHealth () <= 0) {
				StartCoroutine (EndGame (true));
		
			}
		}
	}

	IEnumerator EndGame(bool bPlayerWin){
		//
		Debug.Log(bPlayerWin);

		if (bPlayerWin) {
			playerController.PlayerWin ();
		} else {
			playerController.EnemyWin ();
		}

		yield return new WaitForSeconds (15);
		Application.Quit ();
	}
	//make end game function on another thread.
}
