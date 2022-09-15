using BackEnd;
using BackEnd.Tcp;
using Protocol;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameManager : MonoBehaviour
{
    public static InGameManager instance = null;
    public delegate void PlayerDie(SessionId die, SessionId kill);
    public PlayerDie dieEvent;
    public GameObject owner_player { get; private set; } // 플레이어의 Player 오브젝트

    [SerializeField] // 플레이어 생성 프리팹 저장 위치
    private List<GameObject> playerPrefebs = new List<GameObject>();
    private Stack<SessionId> gameRecord; // 죽은 플레이어 저장 스택
    [SerializeField] // 플레이어 생성 트랜스폼
    private Transform playerPool;

    [SerializeField] // 플레이어 생성 될 위치 저장 리스트
    private List<GameObject> statringPoints;
    public Dictionary<SessionId, Player> players { get; private set; }
    public int survivedPlayerCount = 0;
    public List<Player> playersTest;

    const int START_COUNT = 5;
    private const int MAXPLAYER = 4;
    private SessionId userPlayerIndex; // 유저의 세션 아이디
    /*
     * TODO:
        서버 차원에서 클라이언트 오너 플레이어 지정해서 owner_player 객체 초기화 후

        위 코드 호출시 이벤트를 통해 정상적으로 오너 플레이어 초기화 작업이 수행됨
        아래 InitializeOwnerPlayer()를 적절하게 활용해주면 좋을듯
     */
    #region Initialize
    public void InitializeGame()
    {
        //owner_player = 오너 플레이어 객체 (반드시 월드에 인스턴스된 Player프리팹일것)
        GameManager.OnGameOver += OnGameOver; // 게임 종료 이벤트
        GameManager.OnGameResult += OnGameResult; // 게임 종료 결과 이벤트
        dieEvent += PlayerDieEvent;
        SetPlayerInfo();
    }

    public static InGameManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("InGameManager instance does not exist.");
            return null;
        }
        return instance;
    }
    public void SetPlayerInfo()
    {
        if (ServerMatchManager.GetInstance().sessionIdList == null)
        {
            // 현재 세션ID 리스트가 존재하지 않으면, 0.5초 후 다시 실행
            Invoke("SetPlayerInfo", 0.5f);
            return;
        }

        var gamers = ServerMatchManager.GetInstance().gameUserRecords;

        int size = gamers.Count;
        if (size <= 0)
        {
            Debug.Log("No Player Exist!");
            return;
        }

        if (size > MAXPLAYER)
        {
            Debug.Log("Player Pool Exceed!");
            return;
        }

        int index = 0;
        players = new Dictionary<SessionId, Player>();
        //playersTest = new List<Player>();
        foreach (var record in gamers.OrderByDescending(x => x.Key))
        {
            survivedPlayerCount += 1;
            GameObject player = Instantiate(playerPrefebs[record.Value.m_characterIdx],
                            new Vector3(statringPoints[index].transform.position.x, statringPoints[index].transform.position.y, statringPoints[index].transform.position.z),
                            Quaternion.identity, playerPool.transform);

            Debug.Log("USER SESSION ID INFO : " + record.Value.m_sessionId);
            players.Add(record.Value.m_sessionId, player.GetComponent<Player>());
            player.GetComponent<Player>().mySessionId = record.Value.m_sessionId;
            //playersTest.Add(player.GetComponent<Player>());
            if (ServerMatchManager.GetInstance().IsMySessionId(record.Value.m_sessionId)) // 유저 세팅
            {
                owner_player = player;
                InGameUIManager.GetInstance().Init();
                InGameUIManager.GetInstance().setCharacterStatusBar(player);
                players[record.Value.m_sessionId].SetAsOwner();
                userPlayerIndex = record.Value.m_sessionId;
            }
            else // 타 유저 세팅
            {
                InGameUIManager.GetInstance().setCharacterStatusBar(player);
            }
            //players[record.Value.m_sessionId].controller.Initialize();
            index += 1;
        }

        Debug.Log("Num Of Current Player : " + size);
        players[userPlayerIndex].OnAlivePlayerCountUpdate(survivedPlayerCount);
        // StartCount (로딩 동기화 / 호스트 일 경우에만 카운트를 던짐)
        if (ServerMatchManager.GetInstance().IsHost())
        {
            Debug.Log("Start Couroutine Counting");
            gameRecord = new Stack<SessionId>();
            StartCoroutine("StartCount");
        }
    }
    private void PlayerDieEvent(SessionId diePlayer, SessionId killPlayer)
    {
        Player killer = players[killPlayer];
        survivedPlayerCount -= 1;
        killer.killCnt += 1;

        if (killer.isOwner)
        {
            killer.OnPlayerKillCountUpdate(killer.killCnt); // 죽인사람 killCount증가업데이트
        }
        if (players[userPlayerIndex].isOwner)
        {
            players[userPlayerIndex].OnAlivePlayerCountUpdate(survivedPlayerCount); // 게임 주인 HUD 업데이트
        }
       
        if(ServerMatchManager.GetInstance().IsHost() == false)
        {
            return;
        }

        gameRecord.Push(diePlayer);
        Debug.Log(string.Format("살아있는 유저 수 : {0} / 저장된 레코드 수 : {1}",survivedPlayerCount, gameRecord.Count));
        if (survivedPlayerCount <= 1)
        {
            Debug.Log("WHAT THE FUCK");
            SendGameEndOrder();
        }
    }
    #endregion

    public void OnGameOver()
    {
        Debug.Log("Game End");
        if (ServerMatchManager.GetInstance() == null)
        {
            Debug.LogError("매치매니저가 null 입니다.");
            return;
        }
        //Debug.Log("현재 저장 되어있는 레코드의 개수는 ? " + gameRecord.Count);
        ServerMatchManager.GetInstance().MatchGameOver(gameRecord);
    }

    public void OnGameResult()
    {
        Debug.Log("Game Result");

        if (GameManager.GetInstance().IsLobbyScene())
        {
            Debug.Log("Change State");
            GameManager.GetInstance().ChangeState(GameManager.GameState.MatchLobby);
        }
    }

    // dieEvent 게임 조건완료시 호출
    private void SendGameEndOrder()
    {
        // 게임 종료 전환 메시지는 호스트에서만 보냄
        Debug.Log("Make GameResult & Send Game End Order");
        foreach (SessionId session in ServerMatchManager.GetInstance().sessionIdList)
        {
            int killCount = players[session].killCnt;
            if (!gameRecord.Contains(session) && !players[session].isDead)
            {
                Debug.Log("아직 죽지 않은 플레이어가 존재합니다.");
                gameRecord.Push(session);
            }
        }
        Debug.Log("현재 게임레코드의 수는 ? " + gameRecord.Count);

        GameEndMessage message = new GameEndMessage(gameRecord);
        ServerMatchManager.GetInstance().SendDataToInGame<GameEndMessage>(message);
    }

    private void SetGameRecord(int count, int[] arr)
    {
        gameRecord = new Stack<SessionId>();
        for (int i = count - 1; i >= 0; i--)
        {
            gameRecord.Push((SessionId)arr[i]);
        }
    }


    // 리시버 핸들러
    #region handler
    public void OnRecieve(MatchRelayEventArgs args)
    {

        if (args.BinaryUserData == null)
        {
            Debug.LogWarning(string.Format("빈 데이터가 브로드캐스팅 되었습니다.\n{0} - {1}", args.From, args.ErrInfo));
            // 데이터가 없으면 그냥 리턴
            return;
        }
        Message msg = DataParser.ReadJsonData<Message>(args.BinaryUserData);
        if (msg == null)
        {
            //Debug.Log("CHECKING_POINT#1");
            return;
        }

        // Host가 아닐 때 내가 보내는 패킷은 받지 않는다.
        if (!ServerMatchManager.GetInstance().IsHost() && args.From.SessionId == userPlayerIndex)
        {
            return;
        }

        if (players == null)
        {
            Debug.LogError("업데이트 해야 할 Players의 정보가 존재하지 않습니다.");
            return;
        }
        switch (msg.type)
        {
            case Protocol.Type.StartCount:
                StartCountMessage startCount = DataParser.ReadJsonData<StartCountMessage>(args.BinaryUserData);
                Debug.Log("wait second : " + (startCount.time));
                InGameUIManager.GetInstance().SetStartCount(startCount.time);
                break;

            case Protocol.Type.GameStart:
                InGameUIManager.GetInstance().SetStartCount(0, false); // SetStartCount 하자마자 개인 시간
                GameManager.GetInstance().ChangeState(GameManager.GameState.InGame);
                break;

            case Protocol.Type.GameEnd:
                GameEndMessage endMessage = DataParser.ReadJsonData<GameEndMessage>(args.BinaryUserData);
                Debug.Log("게임 시간 소요 혹은 게임 목표 달성, 게임 매치를 종료 합니다." + endMessage.count);
                
                foreach(var data in endMessage.sessionList)
                { 
                    Debug.Log(string.Format("Switch: Session id = {0} / Kill = {1}", data, players[(SessionId)data]));
                }
                SetGameRecord(endMessage.count, endMessage.sessionList);
                GameManager.GetInstance().ChangeState(GameManager.GameState.Over);
                break;

            // 모든 유저가 처음 보내는 비설정 매세지 
            case Protocol.Type.Key:
                KeyMessage keyMessage = DataParser.ReadJsonData<KeyMessage>(args.BinaryUserData);
                ProcessKeyEvent(args.From.SessionId, keyMessage); // 호스트만 초기화 후 재 브로드캐스팅
                break;

            // 이하 호스트가 보내고 비호스트만이 받을 설정 메세지
            case Protocol.Type.PlayerMove:
                PlayerMoveMessage moveMessage = DataParser.ReadJsonData<PlayerMoveMessage>(args.BinaryUserData);
                ProcessPlayerData(moveMessage);
                break;

            case Protocol.Type.PlayerNoMove:
                PlayerNoMoveMessage noMoveMessage = DataParser.ReadJsonData<PlayerNoMoveMessage>(args.BinaryUserData);
                ProcessPlayerData(noMoveMessage);
                break;

            case Protocol.Type.PlayerAttack: // 공격 GunController
                PlayerAttackMessage attackMessage = DataParser.ReadJsonData<PlayerAttackMessage>(args.BinaryUserData);
                ProcessPlayerData(attackMessage);
                break;

            case Protocol.Type.PlayerStopAttack: // 공격 GunController
                PlayerStopAttackMessage stopAttackMessage = DataParser.ReadJsonData<PlayerStopAttackMessage>(args.BinaryUserData);
                ProcessPlayerData(stopAttackMessage);
                break;

            case Protocol.Type.PlayerReload: // 공격 GunController
                PlayerReloadMessage reloadMessage = DataParser.ReadJsonData<PlayerReloadMessage>(args.BinaryUserData);
                ProcessPlayerData(reloadMessage);
                break;

            case Protocol.Type.PlayerSwitchWeapon: // 무기 변경 Player
                PlayerSwitchMessage switchMessage = DataParser.ReadJsonData<PlayerSwitchMessage>(args.BinaryUserData);
                ProcessPlayerData(switchMessage);
                break;

            case Protocol.Type.PlayerDamaged: // 피격 Player
                PlayerDamegedMessage damegedMessage = DataParser.ReadJsonData<PlayerDamegedMessage>(args.BinaryUserData);
                ProcessPlayerData(damegedMessage);
                break;

            case Protocol.Type.PlayerAcquireItem: // 장비 획득 Player
                PlayerAcquireMessage acquiredMessage = DataParser.ReadJsonData<PlayerAcquireMessage>(args.BinaryUserData);
                ProcessPlayerData(acquiredMessage);
                break;


            default:
                Debug.Log("Unknown protocol type");
                return;
        }
    }
    public void OnRecieveForLocal(KeyMessage keyMessage)
    {
        ProcessKeyEvent(userPlayerIndex, keyMessage);
    }

    /* // 추후 테스트 해봐야할 듯? 이거 아닌거 같음;
    public void OnRecieveForLocal(PlayerNoMoveMessage message)
    {
        ProcessPlayerData(message);
    }*/

    // KeyEvent -> 최신화 후 해당 값들 다시 비호스트에게 재 브로드캐스트
    private void ProcessKeyEvent(SessionId index, KeyMessage keyMessage)
    {
        Debug.Log("호스트의 메세지를 읽고 최신화 합니다.");
        if (ServerMatchManager.GetInstance().IsHost() == false)
        {
            return;
        }
        bool isMove = false;
        bool isNoMove = false;
        bool isAttack = false;
        bool isStopAttack = false;
        bool isReload = false;
        bool isSwitch = false;

        int keyData = keyMessage.keyData;

        Vector3 moveVector = Vector3.zero;
        Vector3 targetPos = Vector3.zero;
        Vector3 playerPos = Vector3.zero;

        //if((keyData & KeyEventCode.MOVE) == KeyEventCode.MOVE)
        if (keyData == KeyEventCode.MOVE)
        {
            moveVector = new Vector3(keyMessage.x, keyMessage.y, keyMessage.z); // 애초에 Vector, 방향값이 들어오지 않나..?
            moveVector = Vector3.Normalize(moveVector);
            isMove = true;
        }
        //if ((keyData & KeyEventCode.NO_MOVE) == KeyEventCode.NO_MOVE)
        else if (keyData == KeyEventCode.NO_MOVE)
        {
            playerPos = new Vector3(keyMessage.x, keyMessage.y, keyMessage.z);
            isNoMove = true;
        }

        //if ((keyData & KeyEventCode.ATTACK) == KeyEventCode.ATTACK)
        else if (keyData == KeyEventCode.ATTACK)
        {
            isAttack = true;
            targetPos = new Vector3(keyMessage.x, keyMessage.y, keyMessage.z); // 총 발사 방향 값 들어오 예정?
            //targetPos = Vector3.Normalize(targetPos);
        }

        //if ((keyData & KeyEventCode.STOP_ATTACK) == KeyEventCode.STOP_ATTACK)
        else if (keyData == KeyEventCode.STOP_ATTACK)
        {
            isStopAttack = true;
        }
        else if (keyData == KeyEventCode.RELOAD)
        {
            isReload = true;
        }
        else if (keyData == KeyEventCode.SWITCH)
        {
            isSwitch = true;
        }

        if (isMove) // 호스트 : 타 유저 플레이어의 움직임 처리
        {
            Debug.Log("플레이어는 움직여라!" + index);
            //Debug.Log("player info : " + players[index]+moveVector);
            players[index].controller.SetMoveVector(moveVector);
            PlayerMoveMessage msg = new PlayerMoveMessage(index, playerPos, moveVector);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerMoveMessage>(msg);

        }

        else if (isNoMove) // 호스트 : 타 유저 플레이어의 멈춤
        {
            Debug.Log("플레이어는 멈춰라!");
            //players[index].controller.Move(Vector3.zero);
            players[index].SetPosition(playerPos);
            players[index].controller.SetMoveVector(Vector3.zero); // Vector zero 값만 들어옴 -> zero로 만드는 건 클라에서 하고 Position 가져와서 동기화 처리
            PlayerNoMoveMessage msg = new PlayerNoMoveMessage(index, playerPos);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerNoMoveMessage>(msg);
        }

        else if (isAttack)
        {
            Debug.Log("플레이어는 공격해봐라!");
            players[index].guntroller.FireAction(targetPos);
            PlayerAttackMessage msg = new PlayerAttackMessage(index, targetPos);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerAttackMessage>(msg);
        }

        else if (isStopAttack)
        {
            Debug.Log("플레이어는 공격을 멈춰라!" + index);
            players[index].guntroller.FireStopAction();
            PlayerStopAttackMessage msg = new PlayerStopAttackMessage(index);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerStopAttackMessage>(msg);
        }

        else if (isReload)
        {
            Debug.Log("플레이어는 장전 해라!");
            players[index].guntroller.tryReload();
            PlayerReloadMessage msg = new PlayerReloadMessage(index);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerReloadMessage>(msg);
        }

        else if (isSwitch)
        {
            Debug.Log("플레이어는 무기를 변경 해라!"+index);
            players[index].guntroller.ChangeGun();
            Debug.Log("플레이어는 무기를 변경해라!!! "+index);
            PlayerSwitchMessage msg = new PlayerSwitchMessage(index);
            ServerMatchManager.GetInstance().SendDataToInGame<PlayerSwitchMessage>(msg);
        }

    }

    private void ProcessPlayerData(PlayerMoveMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        Vector3 moveVector = new Vector3(msg.xDir, msg.yDir, msg.zDir);
        if (!moveVector.Equals(players[msg.playerSession].controller.currVelocity))
        {
            players[msg.playerSession].controller.SetMoveVector(moveVector);
        }
    }
    private void ProcessPlayerData(PlayerNoMoveMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        // 케릭터 포지션 재 설정할 필요 있어 보임 -> 현재는 Position이 아닌 Vector3.zero
        Vector3 playerPos = new Vector3(msg.xPos, msg.yPos, msg.zPos);
        players[msg.playerSession].SetPosition(playerPos);
        players[msg.playerSession].controller.SetMoveVector(Vector3.zero); // Vector zero 값만 들어옴 -> zero로 만드는 건 클라에서 하고 Position 가져와서 동기화 처리

        //players[msg.playerSession].controller.SetMoveVector(playerPos);
    }
    private void ProcessPlayerData(PlayerAttackMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        Debug.Log("SessionID" + msg.playerSession);
        Vector3 targetPos = new Vector3(msg.dir_x, msg.dir_y, msg.dir_z);
        players[msg.playerSession].guntroller.FireAction(targetPos);
    }
    private void ProcessPlayerData(PlayerStopAttackMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        players[msg.playerSession].guntroller.FireStopAction();
    }

    private void ProcessPlayerData(PlayerReloadMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        players[msg.playerSession].guntroller.tryReload();
    }
    private void ProcessPlayerData(PlayerSwitchMessage msg)
    {
        if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }
        // 무기변경
        Debug.Log("무기변경");
        players[msg.playerSession].guntroller.ChangeGun();
    }
    private void ProcessPlayerData(PlayerDamegedMessage msg)
    {
        /*if (ServerMatchManager.GetInstance().IsHost() == true)
        {
            return;
        }*/
        Debug.Log("HIT : " + msg.playerSession);
        Vector3 playerPos = new Vector3(msg.pos_x, msg.pos_y, msg.pos_z);
        players[msg.playerSession].TakeHit(msg.hit_damage, msg.shotterSession);
        players[msg.playerSession].SetPosition(playerPos);
    }

    private void ProcessPlayerData(PlayerAcquireMessage msg)
    {
        ItemCategory Item = msg.Item;
        int grade = msg.grade;
        int effectAmount = msg.effectAmount;
        Vector3 playerPos = new Vector3(msg.pos_x, msg.pos_y, msg.pos_z);
        switch (Item)
        {
            case ItemCategory.HealPack:
                players[msg.playerSession].GetItem(ItemCategory.HealPack, effectAmount);
                Debug.Log("healpack");
                break;

            case ItemCategory.ShieldPack:
                players[msg.playerSession].GetItem(ItemCategory.ShieldPack, effectAmount);
                Debug.Log("ShieldPack");
                break;

            case ItemCategory.MainAmmo:
                players[msg.playerSession].GetItem(ItemCategory.MainAmmo, effectAmount);
                Debug.Log("MainAmmo");
                break;

            case ItemCategory.SubAmmo:
                players[msg.playerSession].GetItem(ItemCategory.SubAmmo, effectAmount);
                Debug.Log("SubAmmo");
                break;

            case ItemCategory.Pistol:
                players[msg.playerSession].GetWeapon(ItemCategory.Pistol, grade, effectAmount);
                Debug.Log("Pistol");
                break;

            case ItemCategory.Rifle:
                players[msg.playerSession].GetWeapon(ItemCategory.Rifle, grade, effectAmount);
                Debug.Log("Rifle");
                break;
        }
        players[msg.playerSession].SetPosition(playerPos);
        //players[msg.playerSession].(Item, msg.grade, msg.effectAmount);
    }
    #endregion


    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
    }

    void Start()
    {
        InitializeGame();
        var matchInstance = ServerMatchManager.GetInstance();
        if (matchInstance == null)
        {
            return;
        }
        if (matchInstance.isReconnectProcess)
        {
            InGameUIManager.GetInstance().SetStartCount(0, false);
        }
    }


    void Update()
    {

    }
    #region Coroutine
    IEnumerator StartCount()
    {
        StartCountMessage msg = new StartCountMessage(START_COUNT);

        // 카운트 다운
        for (int i = 0; i < START_COUNT + 1; i++)
        {
            msg.time = START_COUNT - i;
            ServerMatchManager.GetInstance().SendDataToInGame<StartCountMessage>(msg);
            yield return new WaitForSeconds(1); //1초 단위
        }
        // 게임 시작 메시지를 전송
        GameStartMessage gameStartMessage = new GameStartMessage();
        ServerMatchManager.GetInstance().SendDataToInGame<GameStartMessage>(gameStartMessage);
    }
    #endregion
    public bool IsMyPlayerMove()
    {
        return players[userPlayerIndex].controller.isMove;
    }
    public Vector3 GetMyPlayerPos()
    {
        return players[userPlayerIndex].GetPosition();
    }
}
