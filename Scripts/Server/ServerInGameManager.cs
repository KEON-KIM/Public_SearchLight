using BackEnd;
using BackEnd.Tcp;
using Protocol;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterIndex { SpacePirate, SpacePolice, Specialist };
public partial class ServerMatchManager : MonoBehaviour
{
    private MatchGameResult matchGameResult;
    #region INGAME
    private string FAIL_ACCESS_INGAME = "인게임 접속 실패 : {0} - {1}";
    private string SUCCESS_ACCESS_INGAME = "유저 인게임 접속 성공 : {0}";
    private string NUM_INGAME_SESSION = "인게임 내 세션 갯수 : {0}";

    private bool isHost = false;                    // 호스트 여부 (서버에서 설정한 SuperGamer 정보를 가져옴)
    private bool isSetHost = false;
    private bool isSetSelf = false;

    private Queue<KeyMessage> localQueue = null;

    //private CustomUserGameRecord myUserCard;
    #endregion
    public void OnGameReady()
    {
        if (isSetHost == false)
        {
            // 호스트가 설정되지 않은 상태이면 호스트 설정
            isSetHost = SetHostSession();
        }
        Debug.Log("호스트 설정 완료");

        if (isSandBoxGame == true && IsHost() == true)
        {
            SetAIPlayer();
        }

        if (IsHost() == true)
        {
            // 0.5초 후 ReadyToLoadRoom 함수 호출
            Invoke("ReadyToLoadRoom", 0.5f);
        }
    }

    private void GameSetup()
    {
        Debug.Log("게임 시작 메시지 수신. 게임 설정 시작");
        // 게임 시작 메시지가 오면 게임을 레디 상태로 변경
        if (GameManager.GetInstance().GetGameState() != GameManager.GameState.Ready)
        {
            isHost = false;
            isSetHost = false;
            SendSetUserCharacterIndex();
            OnGameReady();
        }
    }

    private void SetAIPlayer()
    {
        int aiCount = numOfClient - sessionIdList.Count;
        int numOfTeamOne = 0;
        int numOfTeamTwo = 0;

        Debug.Log("AI 플레이어 설정 : aiCount : " + aiCount);
        int index = 0;
        for (int i = 0; i < aiCount; ++i)
        {
            CustomUserGameRecord aiRecord = new CustomUserGameRecord(Random.Range(0, 3)); // 새로운 ai record 카드 생성 캐릭터 값 0~3사이
            aiRecord.m_nickname = "AIPlayer" + index;
            aiRecord.m_sessionId = (SessionId)index;
            aiRecord.m_numberOfMatches = 0;
            aiRecord.m_numberOfWin = 0;
            aiRecord.m_numberOfDefeats = 0;
            aiRecord.m_numberOfDraw = 0;

            if (nowMatchType == MatchType.MMR)
            {
                aiRecord.m_mmr = 1000;
            }

            else if (nowMatchType == MatchType.Point)
            {
                aiRecord.m_points = 1000;
            }

            if (nowModeType == MatchModeType.TeamOnTeam)
            {
                if (numOfTeamOne > numOfTeamTwo)
                {
                    aiRecord.m_teamNumber = 1;
                    numOfTeamTwo += 1;
                }
                else
                {
                    aiRecord.m_teamNumber = 0;
                    numOfTeamOne += 1;
                }
            }
            sessionIdList.Add((SessionId)index);
            sessionIdUserList.Add((SessionId)index);
            gameRecords.Add((SessionId)index, aiRecord);
            gameUserRecords.Add((SessionId)index, aiRecord);
            index += 1;
        }
    }

    private void ReadyToLoadRoom()
    {
        if (ServerMatchManager.GetInstance().isSandBoxGame == true)
        {
            Debug.Log("샌드박스 모드 활성화. AI 정보 송신");
            // 샌드박스 모드면 ai 정보 송신
            foreach (var tmp in gameUserRecords)
            {
                if ((int)tmp.Key > (int)SessionId.Reserve)
                {
                    continue;
                }
                Debug.Log("ai정보 송신 : " + (int)tmp.Key);
                Debug.Log("ai 인덱스 정보 송신 : " + tmp.Value.m_characterIdx);
                SendDataToInGame(new Protocol.UserPlayerInfo(tmp.Value, tmp.Value.m_characterIdx));
            }
        }

        Debug.Log("1초 후 룸 씬 전환 메시지 송신");
        Invoke("SendChangeRoomScene", 1f);
    }

    private void OnGameReconnect()
    {
        isHost = false;
        localQueue = null;
        Debug.Log("재접속 프로세스 진행중... 호스트 및 로컬 큐 설정 완료");
    }
    // 호스트 세션 정하기
    private bool SetHostSession()
    {

        // 각 클라이언트가 모두 수행 (호스트 세션 정하는 로직은 모두 같으므로 각각의 클라이언트가 모두 로직을 수행하지만 결과값은 같다.)
        Debug.Log("Enter to Host Session Setting.");
        // 호스트 세션 정렬 (각 클라이언트마다 입장 순서가 다를 수 있기 때문에 정렬)
        sessionIdList.Sort();
        isHost = false;
        // 내가 호스트 세션인지
        foreach (var record in gameRecords)
        {
            if (record.Value.m_isSuperGamer == true)
            {
                if (record.Value.m_sessionId.Equals(Backend.Match.GetMySessionId()))
                {
                    isHost = true;
                }
                hostSession = record.Value.m_sessionId;
                break;
            }
        }

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

        // 호스트 설정까지 끝나면 매치서버와 접속 끊음
        LeaveMatchServer();
        return true;
    }

    private void GameHandler()
    {
        Debug.Log("Successfully Connencted GameHandler on MatchManager");
        Backend.Match.OnSessionJoinInServer += (args) =>
        {
            Debug.Log("OnSessionJoinInServer : " + args.ErrInfo);
            mySessionId = Backend.Match.GetMySessionId();
            // 인게임 서버에 접속하면 호출
            if (args.ErrInfo != ErrorInfo.Success)
            {
                if (isReconnectProcess)
                {
                    if (args.ErrInfo.Reason.Equals("Reconnect Success"))
                    {
                        //재접속 성공
                        GameManager.GetInstance().ChangeState(GameManager.GameState.Reconnect);
                        Debug.Log("Reconnect Success");
                    }
                    else if (args.ErrInfo.Reason.Equals("Fail To Reconnect"))
                    {
                        Debug.Log("Fail To Reconnect");
                        JoinMatchServer();
                        isConnectInGameServer = false;
                    }
                }
                return;
            }
            if (isJoinGameRoom)
            {
                return;
            }
            if (inGameRoomToken == string.Empty)
            {
                Debug.LogError("Connecnted to InGame Server But, Have not Room Token.");
                return;
            }
            Debug.Log("Successfully connected to the InGame server.");
            isJoinGameRoom = true;
            AccessInGameRoom(inGameRoomToken);
        };

        Backend.Match.OnSessionListInServer += (args) =>
        {
            // 세션 리스트 호출 후 조인 채널이 호출됨
            // 현재 같은 게임(방)에 참가중인 플레이어들 중 나보다 먼저 이 방에 들어와 있는 플레이어들과 나의 정보가 들어있다.
            // 나보다 늦게 들어온 플레이어들의 정보는 OnMatchInGameAccess 에서 수신됨
            Debug.Log("OnSessionListInServer : " + args.ErrInfo);

            ProcessMatchInGameSessionList(args); //~Ang
        };

        Backend.Match.OnMatchInGameAccess += (args) =>
        {
            Debug.Log("OnMatchInGameAccess : " + args.ErrInfo);
            // 세션이 인게임 룸에 접속할 때마다 호출 (각 클라이언트가 인게임 룸에 접속할 때마다 호출됨)
            ProcessMatchInGameAccess(args); // [None] : test
        };

        //여기 체크 포인트 check point
        Backend.Match.OnMatchInGameStart += () =>
        {
            // 서버에서 게임 시작 패킷을 보내면 호출
            // 서버에서 인게임 셋업이 필요하면 호출
            GameSetup();
        };

        Backend.Match.OnMatchResult += (args) =>
        {
            // 게임 결과값 업로드 결과
            Debug.Log("The result of uploading the game result : " + string.Format("{0} : {1}", args.ErrInfo, args.Reason));
            // 서버에서 게임 결과 패킷을 보내면 호출
            // 내가(클라이언트가) 서버로 보낸 결과값이 정상적으로 업데이트 되었는지 확인

            if (args.ErrInfo == BackEnd.Tcp.ErrorCode.Success)
            {
                //InGameUiManager.instance.SetGameResult();
                GameManager.GetInstance().ChangeState(GameManager.GameState.Result);
            }
            else if (args.ErrInfo == BackEnd.Tcp.ErrorCode.Match_InGame_Timeout)
            {
                Debug.Log("Failed to Enter the game : " + args.ErrInfo);
                LobbyUIManager.GetInstance().MatchCancelCallback();
            }
            else
            {
                //게임 결과 업로드 실패
                Debug.Log("Failed to uploading of game result : " + args.ErrInfo);
            }
            // 세션리스트 초기화
            sessionIdList = null;
            sessionIdUserList = null;
        };

        Backend.Match.OnMatchRelay += (args) =>
        {
            // 각 클라이언트들이 서버를 통해 주고받은 패킷들
            // 서버는 단순 브로드캐스팅만 지원 (서버에서 어떠한 연산도 수행하지 않음)

            // 게임 사전 설정 ( AI 생성 요청 )
            if (PrevGameMessage(args.BinaryUserData) == true)
            {
                // 게임 사전 설정을 진행하였으면 바로 리턴
                return;
            }

            if (InGameManager.instance == null)
            {
                // 월드 매니저가 존재하지 않으면 바로 리턴
                return;
            }
            // 만약 사전 검사 및 월드매니저가 존재한다면 OnRecieve를 통해 브로드 캐스트 된 메세지를 읽는다.
            InGameManager.instance.OnRecieve(args);
        };

        Backend.Match.OnMatchChat += (args) =>
        {
            // 채팅기능은 구현되지 않았습니다. -> 추후 개발 예정 (룸안에서만)
        };

        Backend.Match.OnLeaveInGameServer += (args) =>
        {
            Debug.Log("OnLeaveInGameServer : " + args.ErrInfo + " : " + args.Reason);
            if (args.Reason.Equals("Fail To Reconnect"))
            {
                JoinMatchServer();
            }
            isConnectInGameServer = false;
        };

        Backend.Match.OnSessionOnline += (args) =>
        {
            // 다른 유저가 재접속 했을 때 호출
            var nickName = Backend.Match.GetNickNameBySessionId(args.GameRecord.m_sessionId);
            Debug.Log(string.Format("[{0}] 온라인되었습니다. - {1} : {2}", nickName, args.ErrInfo, args.Reason));
            ProcessSessionOnline(args.GameRecord.m_sessionId, nickName);
        };

        Backend.Match.OnSessionOffline += (args) =>
        {
            // 다른 유저 혹은 자기자신이 접속이 끊어졌을 때 호출
            Debug.Log(string.Format("[{0}] 오프라인되었습니다. - {1} : {2}", args.GameRecord.m_nickname, args.ErrInfo, args.Reason));
            // 인증 오류가 아니면 오프라인 프로세스 실행
            if (args.ErrInfo != ErrorCode.AuthenticationFailed)
            {
                ProcessSessionOffline(args.GameRecord.m_sessionId);
            }
            else
            {
                // 잘못된 재접속 시도 시 인증오류가 발생
            }
        };

        Backend.Match.OnChangeSuperGamer += (args) =>
        {
            Debug.Log(string.Format("이전 방장 : {0} / 새 방장 : {1}", args.OldSuperUserRecord.m_nickname, args.NewSuperUserRecord.m_nickname));
            // 호스트 재설정
            SetSubHost(args.NewSuperUserRecord.m_sessionId);
            if (isHost)
            {
                // 만약 서브호스트로 설정되면 다른 모든 클라이언트에 싱크메시지 전송
                Invoke("SendGameSyncMessage", 1.0f);
            }
        };
    }

    public void AddMsgToLocalQueue(KeyMessage message)
    {
        if (isHost == false || localQueue == null)
        {
            return;
        }

        localQueue.Enqueue(message);
    }

    private void AccessInGameRoom(string roomToken)
    {
        Backend.Match.JoinGameRoom(roomToken);
    }

    // 재접속 처리 함수
    private void ProcessSessionOnline(SessionId sessionId, string nickName)
    {
        /* InGameUiManager.GetInstance().SetReconnectBoard(nickName);*/
        // 호스트가 아니면 아무 작업 안함 (호스트가 해줌) -> 호스트 최신화 후 비호스트가 동기화
        if (isHost)
        {
            // 재접속 한 클라이언트가 인게임 씬에 접속하기 전 게임 정보값을 전송 시 nullptr 예외가 발생하므로 조금
            // 2초정도 기다린 후 게임 정보 메시지를 보냄
            Invoke("SendGameSyncMessage", 2.0f);
        }
    }

    private void ProcessSessionOffline(SessionId sessionId)
    {
        if (hostSession.Equals(sessionId))
        {
            // 호스트 연결 대기를 띄움
            /*InGameUiManager.GetInstance().SetHostWaitBoard();*/
        }
        else
        {
            // 호스트가 아니면 단순히 UI 만 띄운다.
        }
    }

    public void SetPlayerSessionList(List<SessionId> sessions)
    {
        sessionIdList = sessions;
    }
    // 로딩 전환 메세지 송신
    private void SendChangeRoomScene()
    {
        Debug.Log("룸 씬 전환 메시지 송신");
        SendDataToInGame(new Protocol.LoadRoomSceneMessage());
    }
    private void SendChangeGameScene()
    {
        Debug.Log("게임 씬 전환 메시지 송신");
        SendDataToInGame(new Protocol.LoadGameSceneMessage());
    }

    private void SendSetUserCharacterIndex()
    {
        Debug.Log("케릭터 정보 메세지 송신" + mySessionId);
        UserPlayerInfo msg = new UserPlayerInfo(gameRecords[mySessionId], LobbyUIManager.GetInstance().GetCharacterIdx());
        SendDataToInGame(msg);
    }

    // 서버로 데이터 패킷 전송
    // 서버에서는 이 패킷을 받아 모든 클라이언트(패킷 보낸 클라이언트 포함)로 브로드캐스팅 해준다.
    public void SendDataToInGame<T>(T msg)
    {
        var byteArray = DataParser.DataToJsonData<T>(msg);
        Backend.Match.SendDataToInGameRoom(byteArray);
    }

    public void LeaveInGameRoom()
    {
        isConnectInGameServer = false;
        Backend.Match.LeaveGameServer();
    }

    // 게임 설정 시 사전 작업 패킷 검사 ( 게임 핸들러 호출)
    // Relay로 받아 읽어옴.
    public bool PrevGameMessage(byte[] BinaryUserData)
    {
        Protocol.Message msg = DataParser.ReadJsonData<Protocol.Message>(BinaryUserData);
        if (msg == null)
        {
            return false;
        }

        switch (msg.type)
        {
            case Protocol.Type.AIPlayerInfo:
                Protocol.AIPlayerInfo aiPlayerInfo = DataParser.ReadJsonData<Protocol.AIPlayerInfo>(BinaryUserData);
                //ProcessAIDate(aiPlayerInfo);
                return true;

            case Protocol.Type.UserPlayerInfo:
                Debug.Log("수신 양호");
                Protocol.UserPlayerInfo userPlayerInfo = DataParser.ReadJsonData<Protocol.UserPlayerInfo>(BinaryUserData);
                ProcessUserDate(userPlayerInfo);
                return true;

            case Protocol.Type.LoadRoomScene:
                LobbyUIManager.GetInstance().ChangeRoomLoadScene();
                if (IsHost() == true)
                {
                    Debug.Log("5초 후 게임 씬 전환 메시지 송신");
                    Invoke("SendChangeGameScene", 5f);
                }
                return true;

            case Protocol.Type.LoadGameScene:
                GameManager.GetInstance().ChangeState(GameManager.GameState.Start);
                return true;
        }
        return false;
    }

    // AI레코드 수신대로 게임레코드 최신화
    private void ProcessAIDate(Protocol.AIPlayerInfo aIPlayerInfo)
    {
        MatchInGameSessionEventArgs args = new MatchInGameSessionEventArgs();
        args.GameRecord = aIPlayerInfo.GetMatchRecord();

        ProcessMatchInGameAccess(args);
    }

    private void ProcessUserDate(Protocol.UserPlayerInfo userPlayerInfo)
    {

        if (isReconnectProcess)
        {
            // 재접속 프로세스 인 경우
            Debug.Log("재접속 프로세스 진행중....\n");
            return;
        }


        var record = userPlayerInfo.GetMatchRecord();
        Debug.Log(string.Format("FUCKING : {0}, {1}", record.m_sessionId, record.m_characterIdx));
        if (!sessionIdUserList.Contains(record.m_sessionId))
        {
            sessionIdUserList.Add(record.m_sessionId);
            gameUserRecords.Add(record.m_sessionId, record);
        }
        isSetSelf = true;
    }
    public void MatchGameOver(Stack<SessionId> record)
    {
        List<string[]> matchGameResultList = new List<string[]>();
        if (nowModeType == MatchModeType.OneOnOne)
        {
            matchGameResult = OneOnOneRecord(record, matchGameResultList);
        }
        else if (nowModeType == MatchModeType.Melee)
        {
            matchGameResult = MeleeRecord(record, matchGameResultList);
        }
        // else if (nowModeType == MatchModeType.TeamOnTeam)
        // {
        //     matchGameResult = TeamRecord(record);
        // }
        else
        {
            Debug.LogError("게임 결과 종합 실패 - 알수없는 매치모드타입입니다.\n" + nowModeType);
            return;
        }

        InGameUIManager.GetInstance().SetGameResult(matchGameResultList);
        RemoveAISessionInGameResult();
        Backend.Match.MatchEnd(matchGameResult);
    }

    private void RemoveAISessionInGameResult()
    {
        string str = string.Empty;
        List<SessionId> aiSession = new List<SessionId>();
        if (matchGameResult.m_winners != null)
        {
            str += "승자 : ";
            foreach (var tmp in matchGameResult.m_winners)
            {
                if ((int)tmp < (int)SessionId.Reserve)
                {
                    aiSession.Add(tmp);
                }
                else
                {
                    str += tmp + " : ";
                }
            }
            str += "\n";
            matchGameResult.m_winners.RemoveAll(aiSession.Contains);
        }

        aiSession.Clear();
        if (matchGameResult.m_losers != null)
        {
            str += "패자 : ";
            foreach (var tmp in matchGameResult.m_losers)
            {
                if ((int)tmp < (int)SessionId.Reserve)
                {
                    aiSession.Add(tmp);
                }
                else
                {
                    str += tmp + " : ";
                }
            }
            str += "\n";
            matchGameResult.m_losers.RemoveAll(aiSession.Contains);
        }
        Debug.Log(str);
    }
    private MatchGameResult OneOnOneRecord(Stack<SessionId> record, List<string[]> matchGameResult)
    {
        MatchGameResult nowGameResult = new MatchGameResult();
        SessionId session = record.Pop();
        record.Pop();
        string[] list = new string[3];

        nowGameResult.m_winners = new List<SessionId>();
        nowGameResult.m_winners.Add(session);
        CharacterIndex chridx = (CharacterIndex)gameUserRecords[session].m_characterIdx;

        list[0] = gameRecords[session].m_nickname;
        list[1] = chridx.ToString();
        list[2] = InGameManager.GetInstance().players[session].killCnt.ToString();
        matchGameResult.Add(list);

        session = record.Pop();
        list = new string[3];

        nowGameResult.m_losers = new List<SessionId>();
        nowGameResult.m_losers.Add(session);

        list[0] = gameUserRecords[session].m_nickname;
        list[1] = gameUserRecords[session].m_characterIdx.ToString();
        list[2] = InGameManager.GetInstance().players[session].killCnt.ToString();
        matchGameResult.Add(list);

        nowGameResult.m_draws = null;

        return nowGameResult;
    }

    // 개인전 게임 결과
    private MatchGameResult MeleeRecord(Stack<SessionId> record, List<string[]> matchGameResult)
    {
        MatchGameResult nowGameResult = new MatchGameResult();
        nowGameResult.m_draws = null;
        nowGameResult.m_losers = new List<SessionId>();
        nowGameResult.m_winners = new List<SessionId>();
        Debug.Log("비호스트가 저장하고 있는 gameRecords" + gameRecords.Count);
        SessionId session = record.Pop();

        string[] list = new string[3];
        nowGameResult.m_winners.Add(session);
        CharacterIndex chridx = (CharacterIndex)gameUserRecords[session].m_characterIdx;

        list[0] = gameRecords[session].m_nickname;
        list[1] = chridx.ToString();
        list[2] = InGameManager.GetInstance().players[session].killCnt.ToString();
        matchGameResult.Add(list);
        Debug.Log("MatchGameRecord.Count : " + record.Count);
        int size = record.Count;
        for (int i = 0; i < size; ++i)
        {
            session = record.Pop();
            chridx = (CharacterIndex)gameUserRecords[session].m_characterIdx;
            Debug.Log("Session ID : " + session);
            list = new string[3];
            list[0] = gameUserRecords[session].m_nickname;
            list[1] = chridx.ToString();
            list[2] = InGameManager.GetInstance().players[session].killCnt.ToString();

            nowGameResult.m_losers.Add(session);
            matchGameResult.Add(list);
        }

        return nowGameResult;
    }

}
