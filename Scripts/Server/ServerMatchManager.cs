using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using BackEnd.Tcp;
using Battlehub.Dispatcher;
using Protocol;
using UnityEngine;

public partial class ServerMatchManager : MonoBehaviour
{
    private static ServerMatchManager instance = null;
    // Lobby Scene
    #region LOBBY
    public List<MatchInfo> matchInfos { get; private set; } = new List<MatchInfo>();  // 콘솔에서 생성한 매칭 카드들의 리스트
    public List<SessionId> sessionIdList { get; private set; }  // 매치에 참가중인 유저들의 세션 목록
    public List<SessionId> sessionIdUserList { get; private set; }  // 매치에 참가중인 유저들의 세션 목록
    public Dictionary<SessionId, MatchUserGameRecord> gameRecords { get; private set; } = null;  // 매치에 참가중인 유저들의 매칭 기록
    public Dictionary<SessionId, CustomUserGameRecord> gameUserRecords;  // 매치에 참가중인 유저들의 매칭 기록
    public SessionId hostSession { get; private set; }  // 호스트 세션의 세션 아이디 저장
    private ServerInfo roomInfo = null;
    public bool isConnectMatchServer { get; private set; } = false;
    private string inGameRoomToken = string.Empty;  // 게임 룸 토큰 (인게임 접속 토큰)
    public bool isReconnectEnable { get; private set; } = false;
    private bool isConnectInGameServer = false;
    private bool isJoinGameRoom = false;
    public bool isReconnectProcess { get; private set; } = false;
    public bool isSandBoxGame { get; private set; } = false;
    private int numOfClient = 1; // 매칭 참가 유저 수
    private SessionId mySessionId;
    #endregion
    // Match Scene
    #region MATCH
    public MatchType nowMatchType { get; private set; } = MatchType.None;     // 현재 선택된 매치 타입
    public MatchModeType nowModeType { get; private set; } = MatchModeType.None; // 현재 선택된 매치 모드 타입

    private string NOTCONNECT_MATCHSERVER = "매치 서버에 연결되어 있지 않습니다.";
    private string RECONNECT_MATCHSERVER = "매치 서버에 접속을 시도합니다.";
    private string FAIL_CONNECT_MATCHSERVER = "매치 서버 접속 실패 : {0}";
    private string SUCCESS_CONNECT_MATCHSERVER = "매치 서버 접속 성공";
    private string SUCCESS_MATCHMAKE = "매칭 성공 : {0}";
    private string SUCCESS_REGIST_MATCHMAKE = "매칭 대기열에 등록되었습니다.";
    private string FAIL_REGIST_MATCHMAKE = "매칭 실패 : {0}";
    private string CANCEL_MATCHMAKE = "매칭 신청 취소 : {0}";
    private string INVAILD_MATCHTYPE = "잘못된 매치 타입입니다.";
    private string INVALID_MODETYPE = "잘못된 모드 타입입니다.";
    private string INVALID_OPERATION = "잘못된 요청입니다\n{0}";
    private string EXCEPTION_OCCUR = "Exception Occur : {0}\n다시 매칭을 시도합니다.";
    #endregion

    public static ServerMatchManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("ServerMatchManager instance does not exist.");
            return null;
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    public class MatchInfo // 매칭 카드 정보 (개인전, 단체전...) 개인전만 생성할 예정임.
    {
        public string title;                // 매칭 명
        public string inDate;               // 매칭 inDate (UUID)
        public MatchType matchType;         // 매치 타입
        public MatchModeType matchModeType; // 매치 모드 타입
        public string headCount;            // 매칭 인원
        public bool isSandBoxEnable;        // 샌드박스 모드 (AI매칭)
    }

    public class ServerInfo
    {
        public string host;
        public ushort port;
        public string roomToken;
    }

    /* public class MatchRecord // 매칭 기록 카드 -> 추후 개발? 아예안할듯 싶은데;
     {
         public MatchType matchType;
         public MatchModeType modeType;
         public string matchTitle;
         public string score = "-";
         public int win = -1;
         public int numOfMatch = 0;
         public double winRate = 0;
     }*/

    // Start is called before the first frame update
    void Start()
    {
        GameManager.OnGameReconnect += OnGameReconnect;
    }

    // Update is called once per frame
    void Update()
    {
        if (isConnectInGameServer || isConnectMatchServer)
        {
            Backend.Match.Poll();

            // 호스트의 경우 로컬 큐가 존재 `
            // 큐에 있는 패킷을 로컬에서 처리
            if (localQueue != null)
            {
                while (localQueue.Count > 0)
                {
                    Debug.Log("로컬 큐의 메세지를 확입합니다.");
                    var msg = localQueue.Dequeue();
                    InGameManager.instance.OnRecieveForLocal(msg);
                }
            }
        }
    }
    public void SettingHanddler()
    {
        MatchMakingHandler();
        GameHandler();
        ExceptionHandler();
    }
    public bool IsHost()
    {
        return isHost;
    }
    void OnApplicationQuit()
    {
        if (isConnectMatchServer)
        {
            LeaveMatchServer();
            Debug.Log("ApplicationQuit - LeaveMatchServer");
        }
    }

    public void IsMatchGameActivate() // 매칭 서버 접속
    {
        roomInfo = null;
        isReconnectEnable = false;

        JoinMatchServer();
    }
    #region MATCH

    // 매칭 취소
    public void CancelRegistMatchMaking()
    {
        Backend.Match.CancelMatchMaking();
    }
    // 매칭 서버 접속
    public void JoinMatchServer()
    {
        if (isConnectMatchServer)
        {
            return;
        }
        ErrorInfo errorInfo;
        isConnectMatchServer = true;
        if (!Backend.Match.JoinMatchMakingServer(out errorInfo))
        {
            var errorLog = string.Format(FAIL_CONNECT_MATCHSERVER, errorInfo.ToString());
            Debug.Log(errorLog);
        }
    }

    // 매칭 서버 접속종료
    public void LeaveMatchServer()
    {
        isConnectMatchServer = false;
        Backend.Match.LeaveMatchMakingServer();
    }

    // 매칭 룸 생성하기, 만약에 생성되지 않을 경우 매칭 불가.
    public bool CreateMatchRoom()
    {
        // 매청 서버에 연결되어 있지 않으면 매칭 서버 접속
        if (!isConnectMatchServer)
        {
            Debug.Log(NOTCONNECT_MATCHSERVER);
            Debug.Log(RECONNECT_MATCHSERVER);
            JoinMatchServer();
            return false;
        }
        Debug.Log("방 생성 요청을 서버로 보냄");
        Backend.Match.CreateMatchRoom();
        return true;
    }

    // 매칭 정보 불러오기
    public MatchInfo GetMatchInfo(string indate)
    {
        var result = matchInfos.FirstOrDefault(x => x.inDate == indate);
        if (result.Equals(default(MatchInfo)) == true)
        {
            return null;
        }
        return result;
    }

    // 매칭 요청하기
    public void RequestMatchMaking(int index)
    {
        // 매청 서버에 연결되어 있지 않으면 매칭 서버 접속
        if (!isConnectMatchServer)
        {
            Debug.Log(NOTCONNECT_MATCHSERVER);
            Debug.Log(RECONNECT_MATCHSERVER);
            JoinMatchServer();
            return;
        }
        // 변수 초기화
        isConnectInGameServer = false;

        Backend.Match.RequestMatchMaking(matchInfos[index].matchType, matchInfos[index].matchModeType, matchInfos[index].inDate);
        if (isConnectInGameServer)
        {
            Backend.Match.LeaveGameServer(); //인게임 서버 접속되어 있을 경우를 대비해 인게임 서버 리브 호출
        }
    }

    // 매칭서버접속
    private void ProcessAccessMatchMakingServer(ErrorInfo errInfo)
    {
        if (errInfo != ErrorInfo.Success)
        {
            // 접속 실패
            isConnectMatchServer = false;
        }

        if (!isConnectMatchServer)
        {
            var errorLog = string.Format(FAIL_CONNECT_MATCHSERVER, errInfo.ToString());
            // 접속 실패
            Debug.Log(errorLog);
        }
        else
        {
            //접속 성공
            Debug.Log(SUCCESS_CONNECT_MATCHSERVER);
        }
    }

    /*
     * 매칭 신청에 대한 리턴값 (호출되는 종류)
     * 매칭 신청 성공했을 때
     * 매칭 성공했을 때
     * 매칭 신청 실패했을 때
    */
    private void ProcessMatchMakingResponse(MatchMakingResponseEventArgs args)
    {
        string debugLog = string.Empty;
        bool isError = false;
        switch (args.ErrInfo)
        {
            case ErrorCode.Success:
                // 매칭 성공했을 때
                debugLog = string.Format(SUCCESS_MATCHMAKE, args.Reason);
                LobbyUIManager.GetInstance().MatchDoneCallback();
                ProcessMatchSuccess(args);
                break;

            case ErrorCode.Match_InProgress:
                // 매칭 신청 성공했을 때 or 매칭 중일 때 매칭 신청을 시도했을 때

                // 매칭 신청 성공했을 때
                if (args.Reason == string.Empty)
                {
                    debugLog = SUCCESS_REGIST_MATCHMAKE;
                    LobbyUIManager.GetInstance().MatchRequestCallback(true);
                }
                break;

            case ErrorCode.Match_MatchMakingCanceled:
                // 매칭 신청이 취소되었을 때
                debugLog = string.Format(CANCEL_MATCHMAKE, args.Reason);
                LobbyUIManager.GetInstance().MatchRequestCallback(false);
                break;

            case ErrorCode.Match_InvalidMatchType:
                isError = true;
                // 매치 타입을 잘못 전송했을 때
                debugLog = string.Format(FAIL_REGIST_MATCHMAKE, INVAILD_MATCHTYPE);
                LobbyUIManager.GetInstance().MatchRequestCallback(false);
                break;

            case ErrorCode.Match_InvalidModeType:
                isError = true;
                // 매치 모드를 잘못 전송했을 때
                debugLog = string.Format(FAIL_REGIST_MATCHMAKE, INVALID_MODETYPE);
                LobbyUIManager.GetInstance().MatchRequestCallback(false);
                break;

            case ErrorCode.InvalidOperation:
                isError = true;
                // 잘못된 요청을 전송했을 때
                debugLog = string.Format(INVALID_OPERATION, args.Reason);
                LobbyUIManager.GetInstance().MatchRequestCallback(false);
                break;

            case ErrorCode.Match_Making_InvalidRoom:
                isError = true;
                // 잘못된 요청을 전송했을 때
                debugLog = string.Format(INVALID_OPERATION, args.Reason);
                LobbyUIManager.GetInstance().MatchRequestCallback(false);
                break;

            case ErrorCode.Exception:
                isError = true;
                // 매칭 되고, 서버에서 방 생성할 때 에러 발생 시 exception이 리턴됨
                // 이 경우 다시 매칭 신청해야 됨
                debugLog = string.Format(EXCEPTION_OCCUR, args.Reason);
                LobbyUIManager.GetInstance().RequestMatch();
                break;
        }

        if (!debugLog.Equals(string.Empty))
        {
            Debug.Log(debugLog);
            if (isError == true)
            {
                //LobbyUIManager.GetInstance().SetErrorObject(debugLog);
            }
        }
    }

    // 매칭 성공했을 때
    // 인게임 서버로 접속해야 한다.
    private void ProcessMatchSuccess(MatchMakingResponseEventArgs args)
    {
        ErrorInfo errorInfo;
        if (sessionIdList != null)
        {
            Debug.Log("이전 세션 저장 정보");
            sessionIdList.Clear();
        }

        if (!Backend.Match.JoinGameServer(args.RoomInfo.m_inGameServerEndPoint.m_address, args.RoomInfo.m_inGameServerEndPoint.m_port, false, out errorInfo))
        {
            var debugLog = string.Format(FAIL_ACCESS_INGAME, errorInfo.ToString(), string.Empty);
            Debug.Log(debugLog);
        }

        // 인자값에서 인게임 룸토큰을 저장해두어야 한다.
        // 인게임 서버에서 룸에 접속할 때 필요
        // 1분 내에 모든 유저가 룸에 접속하지 않으면 해당 룸은 파기된다.
        isConnectInGameServer = true;
        isJoinGameRoom = false;
        isReconnectProcess = false;
        inGameRoomToken = args.RoomInfo.m_inGameRoomToken;
        isSandBoxGame = args.RoomInfo.m_enableSandbox;
        var info = GetMatchInfo(args.MatchCardIndate);
        if (info == null)
        {
            Debug.LogError("매치 정보를 불러오는 데 실패했습니다.");
            return;
        }

        nowMatchType = info.matchType;
        nowModeType = info.matchModeType;
        numOfClient = int.Parse(info.headCount);
    }

    public void ProcessReconnect() // 재접속 프로세스 
    {
        Debug.Log("재접속 프로세스 진입");
        if (roomInfo == null)
        {
            Debug.LogError("재접속 할 룸 정보가 존재하지 않습니다.");
            return;
        }

        ErrorInfo errorInfo;

        if (sessionIdList != null)
        {
            Debug.Log("이전 세션 저장 정보 : " + sessionIdList.Count);
            sessionIdList.Clear();
        }

        if (!Backend.Match.JoinGameServer(roomInfo.host, roomInfo.port, true, out errorInfo))
        {
            var debugLog = string.Format(FAIL_ACCESS_INGAME, errorInfo.ToString(), string.Empty);
            Debug.Log(debugLog);
        }

        isConnectInGameServer = true;
        isJoinGameRoom = false;
        isReconnectProcess = true;
    }

    #endregion MATCH
    #region LOBBY

    private void SetSubHost(SessionId hostSessionId)
    {
        Debug.Log("Enter to Sub Session Setting.");
        // 누가 서브 호스트 세션인지 서버에서 보낸 정보값 확인
        // 서버에서 보낸 SuperGamer 정보로 GameRecords의 SuperGamer 정보 갱신
        foreach (var record in gameRecords)
        {
            if (record.Value.m_sessionId.Equals(hostSessionId))
            {
                record.Value.m_isSuperGamer = true;
            }
            else
            {
                record.Value.m_isSuperGamer = false;
            }
        }
        // 내가 호스트 세션인지 확인
        if (hostSessionId.Equals(Backend.Match.GetMySessionId()))
        {
            isHost = true;
        }
        else
        {
            isHost = false;
        }

        hostSession = hostSessionId;

        Debug.Log("Are you Host? " + isHost);
        // 호스트 세션이면 로컬에서 처리하는 패킷이 있으므로 로컬 큐를 생성해준다
        if (isHost)
        {
            localQueue = new Queue<KeyMessage>();
        }
        else
        {
            localQueue = null;
        }

        Debug.Log("Complete Host setting.");
    }
    // 뒤끝 콘솔에서 매칭 카드 정보 가져오기.
    public void GetMatchList(Action<bool, string> func)
    {
        matchInfos.Clear();
        Backend.Match.GetMatchList(callback =>
        {
            // 요청 실패하는 경우 재요청
            if (callback.IsSuccess() == false)
            {
                Debug.Log("Failed to load Matching card list\n" + callback);
                Dispatcher.Current.BeginInvoke(() =>
                {
                    GetMatchList(func);
                });
                return;
            }
            // matchinfo = 매칭 카드 개수
            foreach (LitJson.JsonData row in callback.Rows())
            {
                MatchInfo matchInfo = new MatchInfo();
                matchInfo.title = row["matchTitle"]["S"].ToString();
                matchInfo.inDate = row["inDate"]["S"].ToString();
                matchInfo.headCount = row["matchHeadCount"]["N"].ToString();
                matchInfo.isSandBoxEnable = row["enable_sandbox"]["BOOL"].ToString().Equals("True") ? true : false;
                foreach (MatchType type in Enum.GetValues(typeof(MatchType)))
                {
                    if (type.ToString().ToLower().Equals(row["matchType"]["S"].ToString().ToLower()))
                    {
                        matchInfo.matchType = type;
                    }
                }

                foreach (MatchModeType type in Enum.GetValues(typeof(MatchModeType)))
                {
                    if (type.ToString().ToLower().Equals(row["matchModeType"]["S"].ToString().ToLower()))
                    {
                        matchInfo.matchModeType = type;
                    }
                }
                matchInfos.Add(matchInfo);
            }

            Debug.Log("매칭 카드 개수 : " + matchInfos.Count);
            func(true, string.Empty);
        });
    }

    private void MatchMakingHandler() // 대기방 핸들러
    {
        Debug.Log("Successfully Connencted MatchMakingHandler on MatchManager");
        // 매칭 서버에 접속하면 호출
        Backend.Match.OnJoinMatchMakingServer += (args) =>
        {
            Debug.Log("OnJoinMatchMakingServer : " + args.ErrInfo);
            // 매칭 접속 상태 호출
            ProcessAccessMatchMakingServer(args.ErrInfo);
        };
        // 매치 신청 관련 작업 호출
        Backend.Match.OnMatchMakingResponse += (args) =>
        {
            Debug.Log("OnMatchMakingResponse : " + args.ErrInfo + " : " + args.Reason);
            // 매칭 응답 호출
            ProcessMatchMakingResponse(args);
        };

        // 매칭 서버에서 접속 종료할 때 호출
        Backend.Match.OnLeaveMatchMakingServer += (args) =>
        {
            Debug.Log("OnLeaveMatchMakingServer : " + args.ErrInfo);
            isConnectMatchServer = false;

            if (args.ErrInfo.Category.Equals(ErrorCode.DisconnectFromRemote) || args.ErrInfo.Category.Equals(ErrorCode.Exception)
                || args.ErrInfo.Category.Equals(ErrorCode.NetworkTimeout))
            {
                // 서버에서 강제로 끊은 경우 -> 대기시간이 길어질 경우.
                if (LobbyUIManager.GetInstance())
                {
                    LobbyUIManager.GetInstance().MatchRequestCallback(false);
                    /*LobbyUIManager.GetInstance().CloseRoomUIOnly();*/
                    LobbyUIManager.GetInstance().SetErrorObject("Connection with Server is Disconnected.\n\n" + args.ErrInfo.Reason);
                }
            }
            Debug.Log("매칭 서버 종료 완료");
        };

        Backend.Match.OnMatchMakingRoomUserList += (args) =>
        {
            Debug.Log(string.Format("OnMatchMakingRoomUserList : {0} : {1}", args.ErrInfo, args.Reason));
            List<MatchMakingUserInfo> userList = null;
            if (args.ErrInfo.Equals(ErrorCode.Success))
            {
                userList = args.UserInfos;
                Debug.Log("Ready room user count : " + userList.Count);
            }
            LobbyUIManager.GetInstance().CreateRoomResult(args.ErrInfo.Equals(ErrorCode.Success) == true, userList);
        };

        // 대기 방 생성/실패 여부
        Backend.Match.OnMatchMakingRoomCreate += (args) =>
        {
            Debug.Log("OnMatchMakingRoomCreate : " + args.ErrInfo + " : " + args.Reason);

            LobbyUIManager.GetInstance().CreateRoomResult(args.ErrInfo.Equals(ErrorCode.Success) == true);
        };


    }
    public void LeaveMatchLoom()
    {
        Backend.Match.LeaveMatchRoom();
    }
    #endregion

    private void ExceptionHandler()
    {
        // 예외가 발생했을 때 호출
        Backend.Match.OnException += (e) =>
        {
            Debug.Log(e);
        };
    }

    private void ProcessMatchInGameAccess(MatchInGameSessionEventArgs args)
    {
        if (isReconnectProcess)
        {
            // 재접속 프로세스 인 경우
            Debug.Log("재접속 프로세스 진행중... 재접속 프로세스에서는 ProcessMatchInGameAccess 메시지는 수신되지 않습니다.\n" + args.ErrInfo);
            return;
        }

        Debug.Log(string.Format(SUCCESS_ACCESS_INGAME, args.ErrInfo));

        if (args.ErrInfo != ErrorCode.Success)
        {
            Debug.Log("FUCKING HELLYA");
            // 게임 룸 접속 실패
            var errorLog = string.Format(FAIL_ACCESS_INGAME, args.ErrInfo, args.Reason);
            Debug.Log(errorLog);
            LeaveInGameRoom(); // 솔로일 경우 방만들지 않음. 방 만들 경우의 콜백함수 생성해야함
            return;
        }

        // 게임 룸 접속 성공
        // 인자값에 방금 접속한 클라이언트(세션)의 세션ID와 매칭 기록이 들어있다.
        // 세션 정보는 누적되어 들어있기 때문에 이미 저장한 세션이면 건너뛴다.

        var record = args.GameRecord;
        Debug.Log(string.Format(string.Format("인게임 접속 유저 정보 [{0}] : {1}", args.GameRecord.m_sessionId, args.GameRecord.m_nickname)));
        if (!sessionIdList.Contains(args.GameRecord.m_sessionId))
        {
            // 세션 정보, 게임 기록 등을 저장
            sessionIdList.Add(record.m_sessionId);
            gameRecords.Add(record.m_sessionId, record);
            Debug.Log(string.Format(NUM_INGAME_SESSION, sessionIdList.Count));
        }
    }

    // 게임 핸들러의 OnSessionListInServer 응답 결과
    // 게임 레코드를 받고, 해당 게임리스트의 세션에게 메세지 전송
    private void ProcessMatchInGameSessionList(MatchInGameSessionListEventArgs args)
    {
        sessionIdList = new List<SessionId>();
        sessionIdUserList = new List<SessionId>();
        gameRecords = new Dictionary<SessionId, MatchUserGameRecord>();
        gameUserRecords = new Dictionary<SessionId, CustomUserGameRecord>();

        Debug.Log("모든 리스트 초기화 방안에 있는 모든 플레이어들 확인");
        foreach (var record in args.GameRecords)
        {
            Debug.Log("Annng~" + record.m_sessionId);
            sessionIdList.Add(record.m_sessionId);
            gameRecords.Add(record.m_sessionId, record);
        }

        //SendSetUserCharacterIndex(); // 현재 나의 로비 케릭터 정보 전송.
        sessionIdList.Sort();
    }

    public bool IsMySessionId(SessionId session)
    {
        return Backend.Match.GetMySessionId() == session;
    }

    public string GetNickNameBySessionId(SessionId session)
    {
        return gameRecords[session].m_nickname;
    }
}
