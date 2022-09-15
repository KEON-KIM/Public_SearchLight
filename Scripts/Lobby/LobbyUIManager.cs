using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BackEnd.Tcp;
using Battlehub.Dispatcher;
using BackEnd;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    private static LobbyUIManager instance = null;
    Camera viewCamera;
    LobbyAnimationPlayer selectedAnimPlayer;
    #region LOBBY
    //public GameObject userCardObject; // 유저 카드
    //public TextMeshProUGUI matchInfoText;
    //public GameObject reconnectObject; // 리커넥션 오브젝트
    public GameObject titleObject;
    public GameObject modelObject; // 모델오브젝트 추후에 리스트로 변경할 예정
    public GameObject errorObject; // 에러 오브젝트
    public TextMeshProUGUI errorText; // 에러 메세지 (에러 오브젝트 안)
    public GameObject nickNameObject; // 로비 닉네임 오브젝트
    public GameObject loadingObject; // 로딩 오브젝트
    

    public GameObject cancelBtnObject;

    [SerializeField]
    public int modelCharacterIndex = 2; // 생성 될 케릭터 인덱스 : LobbyManager에서 관리할 것
    private MatchCard[] matchInfotabList; // 매칭카드 리스트 -> 추후에 리스트 삭제하고 MatchCard하나로 변경가능하게 굳이 토글탭 만들어야함?
    private MatchCard[] matchRecordTabList;
    #endregion
    #region ROOM
    public GameObject readyUserListParent; // 레디한 유저 카드들 저장 오브젝트
    public GameObject userCardPrefab; // 유저 카드 프리팹

    // public GameObject friendEmptyObject; // 친구 리스트 오브젝트 -> 추후 방생성할 때 추가될 것

    private List<string> readyUserList = null;
    #endregion
    #region MATCH
    public GameObject requestProgressObject; // 요청시 윈도우
    public GameObject matchingObject; // 매칭 텍스트 오브젝트
    public GameObject matchDoneObject; // 매칭던텍스트 오브젝트
    public GameObject matchBtnObject; // 매칭 버튼
    public ToggleGroup matchCardObecjt;
    #endregion
    public static LobbyUIManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("LobbyManager instance does not exist.");
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
        ServerMatchManager.GetInstance().IsMatchGameActivate();
    }
    // Start is called before the first frame update
    void Start()
    {
        viewCamera = Camera.main;

        if (ServerMatchManager.GetInstance() != null)
        {
            SetNickName();
        }
        loadingObject.SetActive(false);

        //modelObject.SetActive(true);
        errorObject.SetActive(false);
        requestProgressObject.SetActive(false);
        matchDoneObject.SetActive(false);

        matchInfotabList = matchCardObecjt.GetComponentsInChildren<MatchCard>(); // 매칭카드 탭
        //matchRecordTabList = recordObject.GetComponentsInChildren<TabUI>();
        int index = 0;
        foreach (var info in ServerMatchManager.GetInstance().matchInfos) // 현재 매칭카드는 한개만.
        {
            matchInfotabList[index].SetTabText(info.title);
            matchInfotabList[index].index = index;
            //matchRecordTabList[index].SetTabText(info.title); // 매칭 기록은 현재 기능에 없으므로 제거
            //matchRecordTabList[index].index = index;
            index += 1;
        }

        for (int i = ServerMatchManager.GetInstance().matchInfos.Count; i < matchInfotabList.Length; ++i)
        {
            matchInfotabList[i].gameObject.SetActive(false);
            Debug.Log("Disabled");
        }
        ChangeTab();

    }
    #region LOBBY
    // 매칭 버튼 클릭
    public void OpenRoomUI()
    {
        // 매치 서버에 대기방 생성 요청

        matchBtnObject.SetActive(false);
        Debug.Log("Pushed OpenRoomUI Button");
        if (ServerMatchManager.GetInstance().CreateMatchRoom())
        {
            SetLoadingObjectActive(true);
            matchBtnObject.SetActive(false);
            Debug.Log("Success create matching room");
        }
    }

    private void SetNickName()
    {
        var name = ServerManager.GetInstance().userNickName;
        if (name.Equals(string.Empty))
        {
            Debug.LogError("닉네임 불러오기 실패");
            name = "test123";
        }
        TextMeshProUGUI nickname = nickNameObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = nickNameObject.GetComponent<RectTransform>();

        nickname.text = name;
        rect.sizeDelta = new Vector2(nickname.preferredWidth, nickname.preferredHeight);
    }

    public void RequestCancel()
    {
        if (loadingObject.activeSelf || errorObject.activeSelf)
        {
            return;
        }
        ServerMatchManager.GetInstance().CancelRegistMatchMaking();
    }
   
    #endregion
    #region ROOM
    public void CreateRoomResult(bool isSuccess, List<MatchMakingUserInfo> userList = null)
    {
        if (isSuccess == true)
        {
            Debug.Log("Create Room Object");
            //readyRoomObject.SetActive(true); // 레디 방 생성 UI키기

            if (userList == null)
            {
                Debug.Log("새로운 유저 리스트 생성");
                SetReadyUserList(ServerManager.GetInstance().userNickName);
            }
            else
            {
                Debug.Log("기존 유저 리스트로 접속");
                SetReadyUserList(userList);
            }
           
            SetLoadingObjectActive(false);
            RequestMatch(); // 자신의 유저카드만 생성 후 매칭 시작
        }
        // 대기 방 생성에 실패 시 에러를 띄움
        else
        {
            SetLoadingObjectActive(false);
            SetErrorObject("대기방 생성에 실패했습니다.\n\n잠시 후 다시 시도해주세요.");
        }
    }
    public void SetReadyUserList(List<MatchMakingUserInfo> userList)
    {
        ClearReadyUserList();
        if (userList == null)
        {
            Debug.LogError("ready user list is null");
            return;
        }
        if (userList.Count <= 0)
        {
            Debug.LogError("ready user list is empty");
            return;
        }

        foreach (var user in userList)
        {
            InsertReadyUserPrefab(user.m_nickName);
            Debug.Log("NAME : " + user.m_nickName);
        }
    }
    public void SetReadyUserList(string nickName)
    {
        ClearReadyUserList();
        if (string.IsNullOrEmpty(nickName))
        {
            Debug.LogError("ready user list is empty");
            return;
        }
        Debug.Log("NAME : " + nickName);
        InsertReadyUserPrefab(nickName);
    }

    public void InsertReadyUserPrefab(string nickName)
    {
        if (readyUserList == null)
        {
            return;
        }

        if (readyUserList.Contains(nickName))
        {
            return;
        }
        Debug.Log("새로운 유저 카드 생성");
        GameObject user = GameObject.Instantiate(userCardPrefab, Vector3.zero, Quaternion.identity, readyUserListParent.transform);
        user.GetComponentInChildren<TextMeshProUGUI>().text = nickName;
        user.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
    }

    private void ClearReadyUserList()
    {
        readyUserList = new List<string>();
        var parent = readyUserListParent.transform;

        while (parent.childCount > 0)
        {
            var child = parent.GetChild(0);
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    public void LeaveReadyRoom()
    {
        ServerMatchManager.GetInstance().LeaveMatchLoom();
        // lobbyObject.SetActive(true);
        // readyRoomObject.SetActive(false);
    }

    public void ChangeRoomLoadScene()
    {
        GameManager.GetInstance().ChangeState(GameManager.GameState.Ready);
    }

    #endregion
    #region MATCH
    public void ChangeTab()
    {
        int index = 0;
        foreach (var tab in matchInfotabList)
        {
            if (tab.isOn == true)
            {
                break;
            }
            index += 1;
        }

        var matchInfo = ServerMatchManager.GetInstance().matchInfos[index];
    }


    public void RequestMatch()
    {
        if (loadingObject.activeSelf || errorObject.activeSelf || requestProgressObject.activeSelf || matchDoneObject.activeSelf)
        {
            return;
        }
        if (matchInfotabList.Equals(null)) Debug.Log("활성화된 탭이 존재하지 않습니다.");
        else
        {
            foreach (var tab in matchInfotabList)
            {
                if (tab.isOn == true)
                {
                    ServerMatchManager.GetInstance().RequestMatchMaking(tab.index);
                    return;
                }
            }
            Debug.Log("활성화된 탭이 존재하지 않습니다.");
        }

    }

    // 매칭 요청 실패시 콜백
    public void MatchRequestCallback(bool result)
    {
        if (!result) // 실패시 요청 오브젝트 비활성화
        {
            Debug.Log("요청오브젝트 실패");
            LeaveReadyRoom();
            requestProgressObject.SetActive(false);

            modelObject.SetActive(true);
            nickNameObject.SetActive(true);
            titleObject.SetActive(true);
            matchBtnObject.SetActive(true);
            return;
        }
        Debug.Log("매칭 프로세스 시작");
        requestProgressObject.SetActive(true);
        modelObject.SetActive(false);
        nickNameObject.SetActive(false);
        titleObject.SetActive(false);
    }

    // 매칭 완료 콜백
    public void MatchDoneCallback()
    {
        matchingObject.SetActive(false);
        matchDoneObject.SetActive(true);
        cancelBtnObject.SetActive(false);
    }

    // 매치 취소할 경우 콜백 함수
    public void MatchCancelCallback()
    {
        requestProgressObject.SetActive(false);
        matchingObject.SetActive(true);
        matchDoneObject.SetActive(false);
    }

    #endregion
    public void SetLoadingObjectActive(bool isActive)
    {
        loadingObject.SetActive(isActive);
    }

    public void SetErrorObject(string error)
    {
        errorObject.SetActive(true);
        errorText.text = error;
    }

    public void EnableReconnectObject()
    {
        Dispatcher.Current.BeginInvoke(() =>
        {
            loadingObject.SetActive(true);
            Invoke("SetReconnectObject", 1.0f);
        });
    }
    public int GetCharacterIdx()
    {
        return modelCharacterIndex;
    }


    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Debug.Log("마우스눌림");
            OnTrySelectCharacter();
        }
    }

    //선택창에서 마우스 눌렀을때
    public void OnTrySelectCharacter()
    {
        RaycastHit hit;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("레이캐스트성공");
            //Debug.DrawLine(ray.origin, hit.point, Color.red);
            SelectCharacter(hit);
        }
    }

    //캐릭터 선택되었을 시
    public void SelectCharacter(RaycastHit hit)
    {
        var animPlayer = hit.collider.gameObject.GetComponent<LobbyAnimationPlayer>();

        if (animPlayer != null)
        {
            if (selectedAnimPlayer != null && (animPlayer.gameObject == selectedAnimPlayer.gameObject))
                return;

            if (selectedAnimPlayer != null)
                selectedAnimPlayer.Cancel();

            selectedAnimPlayer = animPlayer;
            modelCharacterIndex = (int)selectedAnimPlayer.ClassType;
            animPlayer.Select();
        }
    }

}
