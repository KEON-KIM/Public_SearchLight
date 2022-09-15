using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;
    private static bool isCreate = false;

    #region Scene_name
    private const string LOGIN = "Login";
    private const string LOBBY = "Lobby";
    private const string READY = "Loading";
    private const string INGAME = "InGame";


    private const string PROGRESS_PERCENTAGE = "{0:N1} %";
    #endregion

    #region Actions-Events
    public static event Action OnGameReady = delegate { };
    public static event Action InGame = delegate { }; // Decrease Heart point? or Player Action Data? // INIT IN GAME
    public static event Action LateInGame = delegate { }; // Result Window? // INIT IN GAME3
    public static event Action OnGameOver = delegate { };  // INIT IN GAME
    public static event Action OnGameResult = delegate { }; // INIT IN GAME
    public static event Action OnGameReconnect = delegate { };

    private string asyncSceneName = string.Empty;
    private IEnumerator InGameUpdateCoroutine; // Not Exist Update() Function!
    private IEnumerator LoadUpdateCoroutine; // Not Exist Update() Function!

    [SerializeField]
    private CanvasGroup canvasGroup;
    public enum GameState { Login, MatchLobby, Ready, Start, Over, Result, InGame, Reconnect };
    private GameState gameState;
    #endregion
    public static GameManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("GameManager instance does not exist.");
            return null;
        }

        return instance;
    }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
        Application.targetFrameRate = 60; // 60프레임 고정
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // 게임중 화면슬립모드 해제
        canvasGroup = GameObject.FindGameObjectWithTag("Canvas").GetComponent<CanvasGroup>();

        InGameUpdateCoroutine = InGameUpdate();
        LoadUpdateCoroutine = LoadUpdate();

        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        if (isCreate)
        {
            DestroyImmediate(gameObject, true);
            return;
        }
        gameState = GameState.Login;
        isCreate = true;
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    private void MatchLobby(Action<bool> func)
    {
        if (func != null)
        {
            ChangeSceneAsync(LOBBY, func);
        }
        else
        {
            ChangeScene(LOBBY);
        }
    }
    private void GameReady()
    {
        Debug.Log("loading game..");
        ChangeScene(READY);
        OnGameReady();

    }

    private void GameStart()
    {
        Debug.Log("prevChecking start game..");
        //delegate 초기화
        InGame = delegate { };
        LateInGame = delegate { };
        OnGameOver = delegate { };
        OnGameResult = delegate { };

        //ChangeScene(INGAME);
        ChangeLoadScene(INGAME);
    }

    private void GameOver()
    {
        OnGameOver();
    }
    private void GameResult()
    {
        OnGameResult();
    }

    public bool IsLobbyScene()
    {
        return SceneManager.GetActiveScene().name == LOBBY;
    }
    public void ChangeState(GameState state, Action<bool> func = null)
    {
        gameState = state;
        switch (gameState)
        {
            case GameState.Login:
                break;
            case GameState.MatchLobby:
                MatchLobby(func); // 각 스테이트 함수 이후 canvas 교체
                break;
            case GameState.Ready:
                StartCoroutine(LoadUpdateCoroutine); // 페이크 로딩 코루틴
                GameReady();
                break;
            case GameState.Start:
                GameStart();
                break;
            case GameState.Over:
                GameOver();
                break;
            
            case GameState.Result:
                 GameResult();
                 break;
            
            case GameState.InGame:
                StartCoroutine(InGameUpdateCoroutine);
                break;
            /* 
            case GameState.Reconnect:
                 GameReconnect();
                 break;*/
            default:
                Debug.Log("Unknown State. Please Confirm current state");
                break;
        }
    }
    IEnumerator InGameUpdate()
    {
        while (true)
        {
            if (gameState != GameState.InGame)
            {
                StopCoroutine(InGameUpdateCoroutine);
                yield return null;
            }
            InGame();
            LateInGame();
            yield return new WaitForSeconds(.1f); //1초 단위
        }
    }

    IEnumerator LoadUpdate()
    {
        while (true)
        {
            if (gameState != GameState.Ready)
            {
                StopCoroutine(LoadUpdateCoroutine);
                yield return null;
            }
            OnGameReady();
            yield return null;
        }
    }

    private void ChangeScene(string scene)
    {
        if (scene != LOGIN && scene != INGAME && scene != LOBBY && scene != READY)
        {
            Debug.Log("Unknown Scene");
            return;
        }
        Debug.Log("CURRENT SCENE :: " + scene);
        SceneManager.LoadScene(scene);
    }

    private void ChangeLoadScene(string scene)
    {
        asyncSceneName = string.Empty;
        if (scene != LOGIN && scene != INGAME && scene != LOBBY && scene != READY)
        {
            Debug.Log("Unknown Scene");
            return;
        }
        asyncSceneName = scene;
        StartCoroutine("LoadingScene");
    }

    private void ChangeSceneAsync(string scene, Action<bool> func)
    {
        asyncSceneName = string.Empty;
        if (scene != LOGIN && scene != INGAME && scene != LOBBY && scene != READY)
        {
            Debug.Log("Unknown Scene");
            return;
        }
        asyncSceneName = scene;
        Debug.Log("fade in / out Async Func");
        StartCoroutine("LoadScene", func);
    }

    private IEnumerator LoadScene(Action<bool> func)
    {
        var asyncScene = SceneManager.LoadSceneAsync(asyncSceneName);
        asyncScene.allowSceneActivation = true;

        bool isCallFunc = false;
        while (asyncScene.isDone == false)
        {
            if (asyncScene.progress <= 0.9f)
            {
                func(false);
            }
            else if (isCallFunc == false)
            {
                isCallFunc = true;
                func(true);
            }
            yield return null;
        }
    }

    private IEnumerator LoadingScene()
    {
        var asyncScene = SceneManager.LoadSceneAsync(asyncSceneName);
        asyncScene.allowSceneActivation = false;

        if (gameState == GameState.Start)
        {
            if (LoadUIManager.instance == null)
            {
                Debug.Log("인게임에 잘못된 접근 방법입니다.");
                yield break;
            }

            var progressBar = LoadUIManager.instance.progressBar;
            var progressPer = LoadUIManager.instance.progressText;

            float timer = 0.0f;
            float startPoint = progressBar.fillAmount;
            while (!asyncScene.isDone)
            {
                yield return null;
                if (asyncScene.progress < 0.9f)
                {
                    progressBar.fillAmount = startPoint + ((progressBar.fillAmount - startPoint) / asyncScene.progress);
                    progressPer.text = string.Format(PROGRESS_PERCENTAGE, progressBar.fillAmount * 100);
                }
                else
                {
                    timer += Time.unscaledDeltaTime;
                    progressBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);
                    progressPer.text = string.Format(PROGRESS_PERCENTAGE, progressBar.fillAmount * 100);
                    if (progressBar.fillAmount >= 1.0f)
                    {
                        asyncScene.allowSceneActivation = true;
                        yield break;
                    }
                }
            }
        }
    }

}

