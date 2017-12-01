/**
 * Written by Daniel Mann.
 * created in 2017
 * DanielMannGames@outlook.com
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Monitors the game and decides a winner. After decinding a winner, it displays a message showing who won and ends the 
 * game.
 */
public class GameManager : MonoBehaviour {
	PlayerController playerController;
	CommandController enemyController;
	bool bGameOver = false;


	void Start () {
		playerController = GameObject.FindGameObjectWithTag ("Player").GetComponent<PlayerController> ();
		enemyController = GameObject.FindGameObjectWithTag ("Enemy").GetComponent<CommandController> ();
	}
	

	void Update () {
		if (!bGameOver) {

			if (playerController.getHealth () <= 0) {
				StartCoroutine (EndGame (false));
			} else if (enemyController.getHealth () <= 0) {
				StartCoroutine (EndGame (true));
		
			} else if( playerController.transform.position.y < -3)
				StartCoroutine (EndGame (false));
			else if( enemyController.transform.position.y < -3)
				StartCoroutine (EndGame (true));
		}
	}
		
	IEnumerator EndGame(bool bPlayerWin){
		
		if (bPlayerWin) {
			playerController.PlayerWin ();
		} else {
			playerController.EnemyWin ();
		}

		yield return new WaitForSeconds (5);
		Application.Quit ();
	}
}
