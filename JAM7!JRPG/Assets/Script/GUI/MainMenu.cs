﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : GUIWindow
{
    [Header("References")]
    [SerializeField] private Transform list;

    private int maxIndex;
    private int choosenIndex;


    private int ChoosenIndex
    {
        set
        {
            if (value != choosenIndex)
            {
                list.GetChild(choosenIndex).GetComponent<Text>().color = Color.white;

                choosenIndex = value;

                list.GetChild(choosenIndex).GetComponent<Text>().color = Color.green;
                //AudioManager.Instance.PlaySoundEffect("Click", pitch: false);
                FMOD.Studio.EventInstance SelectedSound = FMODUnity.RuntimeManager.CreateInstance("event:/Interface/Selected");
                SelectedSound.start();
            }
        }
    }


    public override void OnOpen(params object[] args)
    {
        maxIndex = list.childCount;
        ChoosenIndex = 0;

        StartToCheckForPlayerInput();
    }

    private void StartToCheckForPlayerInput()
    {
        StartCoroutine(CheckForPlayerInput());
    }

    private IEnumerator CheckForPlayerInput()
    {
        yield return null;

        for (;;)
        {
            if (Input.GetButtonDown("Submit"))
            {
                //AudioManager.Instance.PlaySoundEffect("Confirm");
                FMOD.Studio.EventInstance confirmSound = FMODUnity.RuntimeManager.CreateInstance("event:/Interface/Confirm");
                confirmSound.start();
                switch (choosenIndex)
                {
                    case 0:
                       
                        GameManager.Singleton.StartGame();
                        break;


                    case 1:

                        //TODO: Load saves
                        break;


                    case 2:

                        GameManager.Singleton.QuitGame();
                        break;
                }

                yield break;
            }
            else if (Input.GetButtonDown("Up"))
            {
                ChoosenIndex = (maxIndex + choosenIndex - 1) % maxIndex;                
            }
            else if (Input.GetButtonDown("Down"))
            {
                ChoosenIndex = (maxIndex + choosenIndex + 1) % maxIndex;
                
            }

                yield return null;
        }
    }
}
