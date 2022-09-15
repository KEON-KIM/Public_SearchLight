using BackEnd.Tcp;
using UnityEngine;
using System.Collections.Generic;

namespace Protocol
{
    // 이벤트 타입
    public enum Type : sbyte
    {
        Key = 0,        // 키(가상 조이스틱) 입력
        PlayerMove,     // 플레이어 이동
        PlayerRotate,   // 플레이어 회전
        PlayerAttack,   // 플레이어 공격
        PlayerStopAttack, // 플레이어 공격 멈춤
        PlayerDamaged,  // 플레이어 데미지 받음
        PlayerNoMove,   // 플레이어 이동 멈춤
        PlayerNoRotate, // 플레이어 회전 멈춤
        PlayerReload,   // 플레이어 장전
        PlayerSwitchWeapon, // 플레이어 상호작용
        PlayerAcquireItem,
        bulletInfo,

        UserPlayerInfo, // User가 존재하는 경우 User의 추가 정보 및 게임 레코드
        //PlayerIndex, // User가 존재하는 경우 User의 추가 정보
        AIPlayerInfo,   // AI가 존재하는 경우 AI 정보, 아마 사용안할걸?
        LoadRoomScene,      // 룸 씬으로 전환
        LoadGameScene,      // 인게임 씬으로 전환
        StartCount,     // 시작 카운트
        GameStart,      // 게임 시작
        GameEnd,        // 게임 종료
        GameSync,       // 플레이어 재접속 시 게임 현재 상황 싱크
        Max
    }
    // 위치 동기화 이벤트 메세지들만
    public static class KeyEventCode
    {
        public const int NONE = 0;
        public const int MOVE = 1;      // 이동 메시지
        public const int NO_MOVE = 2;   // 이동 멈춤 메시지
        public const int ATTACK = 3;    // 공격 메시지
        public const int STOP_ATTACK = 4;    // 공격 메시지
        public const int RELOAD = 5;    // 재장전 메세지 
        public const int SWITCH = 6;    // 무기변경 메세지
    }


    public class Message
    {
        public Type type;

        public Message(Type type)
        {
            this.type = type;
        }
    }

    public class KeyMessage : Message
    {
        public int keyData;
        public float x;
        public float y;
        public float z;

        public KeyMessage(int data, Vector3 pos) : base(Type.Key)
        {
            this.keyData = data;
            this.x = pos.x;
            this.y = pos.y;
            this.z = pos.z;
        }
    }

    public class PlayerMoveMessage : Message
    {
        public SessionId playerSession;
        public float xPos;
        public float yPos;
        public float zPos;
        public float xDir;
        public float yDir;
        public float zDir;
        public PlayerMoveMessage(SessionId session, Vector3 pos, Vector3 dir) : base(Type.PlayerMove)
        {
            this.playerSession = session;
            this.xPos = pos.x;
            this.yPos = pos.y;
            this.zPos = pos.z;
            this.xDir = dir.x;
            this.yDir = dir.y;
            this.zDir = dir.z;
        }
    }

    public class PlayerNoMoveMessage : Message
    {
        public SessionId playerSession;
        public float xPos;
        public float yPos;
        public float zPos;
        public PlayerNoMoveMessage(SessionId session, Vector3 pos) : base(Type.PlayerNoMove)
        {
            this.playerSession = session;
            this.xPos = pos.x;
            this.yPos = pos.y;
            this.zPos = pos.z;
        }
    }

    public class PlayerAttackMessage : Message
    {
        public SessionId playerSession;
        public float dir_x;
        public float dir_y;
        public float dir_z;
        public PlayerAttackMessage(SessionId session, Vector3 pos) : base(Type.PlayerAttack)
        {
            this.playerSession = session;
            dir_x = pos.x;
            dir_y = pos.y;
            dir_z = pos.z;
        }
    }

    public class PlayerStopAttackMessage : Message
    {
        public SessionId playerSession;
        public PlayerStopAttackMessage(SessionId session) : base(Type.PlayerStopAttack)
        {
            this.playerSession = session;
        }
    }

    public class PlayerSwitchMessage : Message
    {
        public SessionId playerSession;
        public PlayerSwitchMessage(SessionId session) : base(Type.PlayerSwitchWeapon)
        {
            this.playerSession = session;
        }
    }

    public class PlayerReloadMessage : Message
    {
        public SessionId playerSession;
        public PlayerReloadMessage(SessionId session) : base(Type.PlayerReload)
        {
            this.playerSession = session;
        }
    }

    public class PlayerDamegedMessage : Message
    {
        public SessionId playerSession; // 맞은 플레이어
        public SessionId shotterSession; // 쏜 플레이어
        public float pos_x; // 동기화 위치
        public float pos_y;
        public float pos_z;
        public float hit_damage;
        // hitDir, hitPoint의 경우 총알 맞는 부분
        public PlayerDamegedMessage(SessionId session, SessionId shotter, Vector3 playerPos, float damage) : base(Type.PlayerDamaged)
        {
            this.playerSession = session;
            this.shotterSession = shotter;
            this.pos_x = playerPos.x; // 동기화 위치
            this.pos_y = playerPos.y;
            this.pos_z = playerPos.z;
            this.hit_damage = damage;
        }
    }

    public class PlayerAcquireMessage : Message
    {
        public SessionId playerSession;
        public ItemCategory Item;
        public int grade;
        public int effectAmount;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public PlayerAcquireMessage(SessionId session, ItemCategory ItemIdx, int grade, int effectAmount, Vector3 playerPos) : base(Type.PlayerAcquireItem)
        {
            this.playerSession = session;
            this.Item = ItemIdx;
            this.grade = grade;
            this.effectAmount = effectAmount;
            this.pos_x = playerPos.x;
            this.pos_y = playerPos.y;
            this.pos_z = playerPos.z;
        }
    }


    public class CustomUserGameRecord : MatchUserGameRecord
    {
        public int m_characterIdx;
        public CustomUserGameRecord(int idx)
        {
            this.m_characterIdx = idx;
        }
    }

    // 새로운 유저 정보 카드, gameUserList에 저장될 것
    public class UserPlayerInfo : Message
    {
        public SessionId m_sessionId;
        public string m_nickname;
        public byte m_teamNumber;
        public int m_numberOfMatches;
        public int m_numberOfWin;
        public int m_numberOfDraw;
        public int m_numberOfDefeats;
        public int m_points;
        public int m_mmr;
        public int m_characterIdx;
        public UserPlayerInfo(MatchUserGameRecord gameRecord, int character_index) : base(Type.UserPlayerInfo)
        {
            this.m_sessionId = gameRecord.m_sessionId;
            this.m_nickname = gameRecord.m_nickname;
            this.m_teamNumber = gameRecord.m_teamNumber;
            this.m_numberOfWin = gameRecord.m_numberOfWin;
            this.m_numberOfDraw = gameRecord.m_numberOfDraw;
            this.m_numberOfDefeats = gameRecord.m_numberOfDefeats;
            this.m_points = gameRecord.m_points;
            this.m_mmr = gameRecord.m_mmr;
            this.m_numberOfMatches = gameRecord.m_numberOfMatches;
            this.m_numberOfDefeats = gameRecord.m_numberOfDefeats;
            this.m_characterIdx = character_index;
        }

        public CustomUserGameRecord GetMatchRecord()
        {
            CustomUserGameRecord gameRecord = new CustomUserGameRecord(this.m_characterIdx);
            gameRecord.m_sessionId = this.m_sessionId;
            gameRecord.m_nickname = this.m_nickname;
            gameRecord.m_numberOfMatches = this.m_numberOfMatches;
            gameRecord.m_numberOfWin = this.m_numberOfWin;
            gameRecord.m_numberOfDraw = this.m_numberOfDraw;
            gameRecord.m_numberOfDefeats = this.m_numberOfDefeats;
            gameRecord.m_mmr = this.m_mmr;
            gameRecord.m_points = this.m_points;
            gameRecord.m_teamNumber = this.m_teamNumber;

            return gameRecord;
        }
    }
    public class AIPlayerInfo : Message
    {
        public SessionId m_sessionId;
        public string m_nickname;
        public byte m_teamNumber;
        public int m_numberOfMatches;
        public int m_numberOfWin;
        public int m_numberOfDraw;
        public int m_numberOfDefeats;
        public int m_points;
        public int m_mmr;
        public int m_characterIdx;

        public AIPlayerInfo(MatchUserGameRecord gameRecord) : base(Type.AIPlayerInfo)
        {
            this.m_sessionId = gameRecord.m_sessionId;
            this.m_nickname = gameRecord.m_nickname;
            this.m_teamNumber = gameRecord.m_teamNumber;
            this.m_numberOfWin = gameRecord.m_numberOfWin;
            this.m_numberOfDraw = gameRecord.m_numberOfDraw;
            this.m_numberOfDefeats = gameRecord.m_numberOfDefeats;
            this.m_points = gameRecord.m_points;
            this.m_mmr = gameRecord.m_mmr;
            this.m_numberOfMatches = gameRecord.m_numberOfMatches;
        }

        public MatchUserGameRecord GetMatchRecord()
        {
            MatchUserGameRecord gameRecord = new MatchUserGameRecord();
            gameRecord.m_sessionId = this.m_sessionId;
            gameRecord.m_nickname = this.m_nickname;
            gameRecord.m_numberOfMatches = this.m_numberOfMatches;
            gameRecord.m_numberOfWin = this.m_numberOfWin;
            gameRecord.m_numberOfDraw = this.m_numberOfDraw;
            gameRecord.m_numberOfDefeats = this.m_numberOfDefeats;
            gameRecord.m_mmr = this.m_mmr;
            gameRecord.m_points = this.m_points;
            gameRecord.m_teamNumber = this.m_teamNumber;

            return gameRecord;
        }
    }


    public class StartCountMessage : Message
    {
        public int time;
        public StartCountMessage(int time) : base(Type.StartCount)
        {
            this.time = time;
        }
    }
    /*public class SendSetUserCharacterIndex : Message
    {
        CustomUserGameRecord gameRecord = new CustomUserGameRecord();
        public SendSetUserCharacterIndex(int m_characterIdx) : base(Type.UserPlayerInfo)
        {
            Debug.Log("FUCKIN!" + m_characterIdx);
            gameRecord.m_characterIdx = m_characterIdx;
        }
    }*/

    public class LoadRoomSceneMessage : Message
    {
        public LoadRoomSceneMessage() : base(Type.LoadRoomScene)
        {

        }
    }

    public class LoadGameSceneMessage : Message
    {
        public LoadGameSceneMessage() : base(Type.LoadGameScene)
        {

        }
    }

    public class GameStartMessage : Message
    {
        public GameStartMessage() : base(Type.GameStart) { }
    }
    /*
    public class GameEndMessage : Message
    {
        public int count;
        public KeyValuePair<SessionId, int>[] sessionList;
        public GameEndMessage(Stack<KeyValuePair<SessionId, int>> result) : base(Type.GameEnd)
        {
            count = result.Count;
            sessionList = new KeyValuePair<SessionId, int>[count];
            for (int i = 0; i < count; ++i)
            {
                sessionList[i] = result.Pop();
            }
            foreach(var tmp in sessionList)
            {
                Debug.Log(string.Format("Protocol : Session id = {0} / Kill = {1}", tmp.Key, tmp.Value));
            }
        }
    }*/

    public class GameEndMessage : Message
    {
        public int count;
        public int[] sessionList;
        public GameEndMessage(Stack<SessionId> result) : base(Type.GameEnd)
        {
            count = result.Count;
            sessionList = new int[count];
            for (int i = 0; i < count; ++i)
            {
                sessionList[i] = (int)result.Pop();
            }
        }
    }
}