﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessBoss : MonoBehaviour {

    private bool isTriggered;
    public Animator chessAnim;

    public GameObject boss;

    private FinalBoss finalBoss;

    void Start(){
        isTriggered = false;
        if (!boss) Debug.Log("In chess: final boss not loaded");

        GameObject[] e = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var item in e) {
            if(item.name != "boss"){
                Destroy(item);
            }
        }
    }

    void Update(){
        if (isTriggered){
            StartCoroutine(SpawnBoss());
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Player"){
            //todo: freeze player movement

            chessAnim.SetBool("isOpened", true);
            isTriggered = true;
        }
    }

    //spawn boss after animation finished
    IEnumerator SpawnBoss(){
        yield return new WaitForSeconds(0.5f);

        //GameObject _boss = Instantiate(boss, gameObject.transform.position, Quaternion.identity);
        boss.transform.localScale = new Vector3(0.1f, 0.1f, 1.0f);
        boss.transform.position = gameObject.transform.position;  

        finalBoss = boss.GetComponent<FinalBoss>();

        finalBoss.isActive = true;

        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        foreach (var item in items) {
            Destroy(item);
        }

        Destroy(gameObject);
    }

}
