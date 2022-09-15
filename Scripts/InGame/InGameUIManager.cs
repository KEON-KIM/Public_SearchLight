using System.Collections;
using System.Collections.Generic;
using TMPro;
using BackEnd;
using BackEnd.Tcp;
using UnityEngine;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    private static InGameUIManager instance = null;

    [SerializeField] Text killScore;
    [SerializeField] Text timeLimit;
    [SerializeField] Text MagazineAmmo;
    [SerializeField] Text PouchAmmo;
    [SerializeField] Text survivedPlayerCount;
    [SerializeField] Image mainSlotImage;
    [SerializeField] Image subSlotImage;
    [SerializeField] Sprite pistolSprite;
    [SerializeField] Sprite rifleSprite;

    [SerializeField] GameObject statusBarPrefab;
    [SerializeField] Canvas mainCanvas;

    public GameObject startCountObject; // 로딩 후 인게임 시작 카운트 오브젝트
    public GameObject gameResultObject; // 게임 결과창
    public GameObject meleeResultContents;
    //public GameObject meleeResultObject; // userMessage창 -> 필요없을듯?
    public GameObject LoadingObject; // 로딩창
    public GameObject returnLobbyObject; // 컨티뉴 버튼

    [SerializeField]
    private GameObject[] meleeResultObject; // 밀리 오브젝트

    private TextMeshProUGUI startCountText;

    private const int MAXPLAYERSIZE = 4;
    private const int USERNAME = 0;
    private const int USERINDEX = 1;
    private const int USERKILL = 2;
    public static InGameUIManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("InGameUIManager instance does not exist.");
            return null;
        }
        return instance;
    }
    const string PlayerReconnectMsg = "{0} 플레이어 재접속중...";
    const string HostOfflineMsg = "호스트와의 연결이 끊어졌습니다.\n연결 대기중";

    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;

        startCountText = startCountObject.GetComponentInChildren<TextMeshProUGUI>();
        startCountObject.SetActive(true);
    }

    void Start()
    {
        LoadingObject.SetActive(false);
        gameResultObject.SetActive(false);
        meleeResultContents.SetActive(false);
    }

    public void setCharacterStatusBar(GameObject player)
    {
        GameObject statusBar = Instantiate(statusBarPrefab);
        statusBar.transform.SetParent(mainCanvas.transform);
        statusBar.GetComponent<HealthBar>().InitStatusBar(player);
    }

    public void Init()
    {
        var hud = InGameManager.GetInstance().owner_player.GetComponent<PlayerHUD>();
        hud.InitializeHUD_Text(killScore, timeLimit, MagazineAmmo, PouchAmmo, survivedPlayerCount,
            mainSlotImage, subSlotImage, pistolSprite, rifleSprite);
    }

    public void SetStartCount(int time, bool isEnable = true)
    {
        startCountObject.SetActive(isEnable);
        if (isEnable)
        {
            if (time == 0)
            {
                startCountText.text = "Game Start!";
            }
            else
            {
                startCountText.text = string.Format("{0}", time);
            }
        }
    }

    public void SetGameResult(List<string[]> matchGameResult)
    {
        gameResultObject.SetActive(true); // BackGround
        LoadingObject.SetActive(true); 
        returnLobbyObject.SetActive(false); // Button;
        //meleeResultObject.SetActive(false); // Contents
        int messageCnt = 0;
        for(int i = 0; i < MAXPLAYERSIZE; i++) // i는 플레이어 최대치만큼 준비되어있음
        {
            messageCnt++;
            meleeResultObject[i].SetActive(false);
        }
        var healthlist = mainCanvas.transform.GetComponentsInChildren<HealthBar>();
        foreach(var bar in healthlist)
        {
            bar.gameObject.SetActive(false);
        }

        var matchInstance = ServerMatchManager.GetInstance();
        if (matchInstance == null)
        {
            returnLobbyObject.SetActive(true);
            return;
        }

        if (matchInstance.nowModeType == MatchModeType.Melee)
        {

            if (messageCnt == 0)
            {
                Debug.LogError("Result_Melee UI 불러오기 실패");
                return;
            }

            //string winner = "";
            for(int i = 0; i < matchGameResult.Count; i++)
            {
                TextMeshProUGUI[] userlist = meleeResultObject[i].transform.GetComponentsInChildren<TextMeshProUGUI>();
                userlist[USERNAME].text = matchGameResult[i][USERNAME];
                userlist[USERINDEX].text = matchGameResult[i][USERINDEX];
                userlist[USERKILL].text = matchGameResult[i][USERKILL];
                //winner += matchInstance.GetNickNameBySessionId(user) + "\n\n";
            }

            //data[2].text = winner;
            Invoke("ShowResultMelee", 0.8f);
        }
    }

    private void ShowResultBase()
    {
        LoadingObject.SetActive(false);
        //baseResultObject.SetActive(true);
        //meleeResultObject.SetActive(false);
        returnLobbyObject.SetActive(true);
    }

    private void ShowResultMelee()
    {
        LoadingObject.SetActive(false);
        for (int i = 0; i < MAXPLAYERSIZE; i++) // i는 플레이어 최대치만큼 준비되어있음
        {
            meleeResultObject[i].SetActive(true);
        }
        meleeResultContents.SetActive(true);
        //baseResultObject.SetActive(false);
        returnLobbyObject.SetActive(true);
    }

    public void ReturnToMatchRobby()
    {
        GameManager.GetInstance().ChangeState(GameManager.GameState.MatchLobby);
    }

}
