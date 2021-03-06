﻿/**
 * @author SerapH
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The state of the game
/// </summary>
public enum GameState : int
{
    Start = 1,
    MainMenu,
    Loading,
    InGame,
}

/// <summary>
/// A FSM for the whole game at the highest level
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// The unique instance
    /// </summary>
    public static GameManager Singleton { get; private set; }

    /// <summary>
    /// An event triggered whenever the state of the game changes
    /// </summary>
    public EventOnDataChange2<GameState> OnCurrentGameStateChange { get; private set; }

    [SerializeField] private GameState initialState = GameState.Start;

    private GameState currentGameState;

    private Dictionary<int, PlayerExplorationController> playerExplorationControllers = new Dictionary<int, PlayerExplorationController>();
    private Dictionary<int, PlayerCombatController> playerCombatControllers = new Dictionary<int, PlayerCombatController>();

    private int numOngoingCombats = 0;

    private int maxNumCombat = 0;
    private Stack<int> combatIDs = new Stack<int>(2);

    FMODUnity.StudioEventEmitter emitter;

    /// <summary>
    /// The current state of the game
    /// </summary>
    public GameState CurrentGameState
    {
        get
        {
            return currentGameState;
        }

        private set
        {
            // Reset the current state
            if (value == currentGameState)
            {
#if UNITY_EDITOR
                Debug.Log(LogUtility.MakeLogStringFormat("GameManager", "{0} --> {0}", value));
#endif

                //switch (currentGameState)
                //{
                //}
            }
            else
            {
                // Before leaving the previous state
                switch (currentGameState)
                {
                    case GameState.MainMenu:
                        GUIManager.Singleton.Close("MainMenu");
                        break;
                }

                GameState previousGameState = CurrentGameState;
                currentGameState = value;

#if UNITY_EDITOR
                Debug.Log(LogUtility.MakeLogStringFormat("GameManager", "{0} --> {1}", previousGameState, currentGameState));
#endif

                OnCurrentGameStateChange.Invoke(previousGameState, currentGameState);

                // After entering the new state
                switch (currentGameState)
                {
                    case GameState.Start:
                        Debug.Assert(DataTableManager.singleton != null);
                        break;


                    case GameState.MainMenu:
                        GUIManager.Singleton.Open("MainMenu");
                        break;


                    case GameState.Loading:
                        LoadPlayer(0);
                        CurrentGameState = GameState.InGame;
                        break;


                    case GameState.InGame:
                        //GUIManager.Singleton.Open("HUD", Player.GetPlayer(0));
                        break;
                }
            }
        }
    }

    private GameManager() { }

    public PlayerExplorationController GetPlayerExplorationController(int id)
    {
        return playerExplorationControllers[id];
    }

    public PlayerCombatController GetPlayerCombatController(int id)
    {
        return playerCombatControllers[id];
    }

    public CombatManager CreateCombat(string name)
    {
        if (combatIDs.Count == 0)
            combatIDs.Push(numOngoingCombats);

        int id = combatIDs.Pop();

        CombatManager combat = Instantiate(ResourceUtility.GetPrefab<CombatManager>(name), new Vector3((id + 1) * 1000, 0, 0), Quaternion.identity);
        combat.ID = id;
        ++numOngoingCombats;

        if (name == "Level/FinalLevel")
        {
            emitter.SetParameter("FinalBossBattle", 1);
            emitter.SetParameter("DarkDimension", 0);
            emitter.SetParameter("Forest", 0);
        }
        else
        {
            emitter.SetParameter("NormalBattle", 1);
            emitter.SetParameter("DarkDimension", 0);
            emitter.SetParameter("Forest", 0);
        }
        return combat;
    }

    public void EndCombat(CombatManager combat)
    {
        combatIDs.Push(combat.ID);

        Destroy(combat);

        --numOngoingCombats;

        if(combat.enemyProxy.gameObject.transform.position.x > 50)
        {
            if (combat.gameObject.name.Contains("BossLevel"))
            {
                emitter.SetParameter("Forest", 0.99f);
                emitter.SetParameter("NormalBattle", 0);
                emitter.SetParameter("Dusk", 1);
            }
            else
            {
                emitter.SetParameter("Forest", 1);
                emitter.SetParameter("NormalBattle", 0);
            }
        }
        else if(combat.enemyProxy.gameObject.transform.position.x < 50)
        {
            emitter.SetParameter("DarkDimension", 1);
            emitter.SetParameter("FinalBossBattle", 0);
        }


        //MusicManager.Instance.inBattle = numOngoingCombats > 0;
    }

    public void StartGame()
    {
        CurrentGameState = GameState.Loading;
    }

    void OnEnable()
    {
        var target = GameObject.Find("BackgroundMusic");
        emitter = target.GetComponent<FMODUnity.StudioEventEmitter>();
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    private void LoadPlayer(int id)
    {

        Player.CreatePlayer(id, 3, 1, 1, 1);
        PlayerExplorationController explorationController = Instantiate(ResourceUtility.GetPrefab<PlayerExplorationController>("PlayerE"));
        explorationController.Initialize(id);
        playerExplorationControllers[id] = explorationController;
        PlayerCombatController combatController = Instantiate(ResourceUtility.GetPrefab<PlayerCombatController>("PlayerCombat"));
        combatController.Initialize(id);
        playerCombatControllers[id] = combatController;

    }

    private void Awake()
    {
        if (!Singleton)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);

            OnCurrentGameStateChange = new EventOnDataChange2<GameState>();
        }
        else if (this != Singleton)
            Destroy(gameObject);
    }

    private void Start()
    {
        CurrentGameState = initialState;
    }
}


