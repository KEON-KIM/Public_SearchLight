using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using static BackEnd.SendQueue; // 뒤끝 서버 비동기 메세지 저장 큐
using UnityEngine.SocialPlatforms;

public class ServerManager : MonoBehaviour
{
    private static ServerManager instance;

    public string userNickName { get; private set; } = string.Empty;
    public string userIndate { get; private set; } = string.Empty;    
    private Action<bool, string> loginSuccessFunc = null; 
    public bool isLogin { get; private set; }  //로그인 여부
    private const string BackendError = "statusCode : {0}\nErrorCode : {1}\nMessage : {2}"; // Error Message


    public static ServerManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("ServerManager instance does not exist.");
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
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        var bro = Backend.Initialize(true);

        if (bro.IsSuccess())
        {
            Debug.Log("Success to Initialize Backend : " + bro);
#if UNITY_ANDROID
                            Debug.Log("GoogleHash - " + Backend.Utils.GetGoogleHash());
#endif
/*
#if !UNITY_EDITOR
                            //안드로이드, iOS 환경에서만 작동
                            GetVersionInfo();
#endif
*/
        }
        else
        {
            Debug.LogError("Fail to Initialize Backend : " + bro);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Backend.AsyncPoll();
    }

    public void BackendTokenLogin(Action<bool, string> func)
    {
        Enqueue(Backend.BMember.LoginWithTheBackendToken, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("Success Token Login");
                loginSuccessFunc = func;

                OnPrevServerAuthorized(); // 토큰으로 뒤끝인증
                return;

            }

            Debug.Log("Failed Token Login\n" + callback.ToString());
            func(false, string.Empty);
        });
    }

    public void CustomLogin(string id, string pw, Action<bool, string> func)
    {
        Enqueue(Backend.BMember.CustomLogin, id, pw, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("Success Custom Login");
                loginSuccessFunc = func;

                OnPrevServerAuthorized();
                return;
            }

            Debug.Log("Failed Custom Login\n" + callback);
            func(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
        });
    }

    public void CustomSignIn(string id, string pw, Action<bool, string> func)
    {
        string tempNickName = id; // 기본 닉네임은 ID로 함
        Enqueue(Backend.BMember.CustomSignUp, id, pw, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("회원 가입 성공 !");
                loginSuccessFunc = func;
                return;
            }

            Debug.LogError(id + pw);
            Debug.LogError("Failed Custom Sign Up\n" + callback.ToString());
            func(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage())); // 에러 토큰 발행
        });

        //닉네임 세팅
        Enqueue(Backend.BMember.UpdateNickname, id, bro =>
        {
            // 닉네임이 없으면 매치서버 접속이 안됨
            if (!bro.IsSuccess())
            {
                Debug.LogError("Failed Create User Nickname\n" + bro.ToString());
                func(false, string.Format(BackendError,
                    bro.GetStatusCode(), bro.GetErrorCode(), bro.GetMessage()));
                return;
            }

            loginSuccessFunc = func;
            OnPrevServerAuthorized(); // Load UserInfo
        });
    }

    private void OnPrevServerAuthorized()
    {
        isLogin = true;
        OnServerAuthorized(); // 서버 회원 인증
    }

    // 실제 유저 정보 불러오기
    private void OnServerAuthorized() // 뒤끝 서버 회원 인증
    {                                                    
        Enqueue(Backend.BMember.GetUserInfo, callback => 
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError("Failed to get the user information.\n" + callback);
                loginSuccessFunc(false, string.Format(BackendError,
                    callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
                return;
            }

            Debug.Log("UserInfo\n" + callback);
            var info = callback.GetReturnValuetoJSON()["row"];
            if (info["nickname"] == null) // 닉네임 정보가 null일 경우 닉네임 업데이트 창 표시
            {
                //LoginUIManager.GetInstance().ActiveNickNameObject();
                Debug.Log("닉네임 정보를 찾을 수 없습니다.");
                return;
            }

            userNickName = info["nickname"].ToString();
            userIndate = info["inDate"].ToString();
            if (loginSuccessFunc != null) // 진행 중인 게임이 있는지 확인
            {
                Debug.Log("진행 중인 게임을 확인합니다.");
                ServerMatchManager.GetInstance().GetMatchList(loginSuccessFunc);
                return;
            }
        });
    }

    /*public void CustomSignUp() // 테스트
    {
        string id = "user1"; // 원하는 아이디
        string password = "1234"; // 원하는 비밀번호

        var bro = Backend.BMember.CustomSignUp(id, password);
        if (bro.IsSuccess())
        {
            Debug.Log("회원가입 성공!");
        }
        else
        {
            Debug.LogError("회원가입 실패!");
            Debug.LogError(bro); // 뒤끝의 리턴케이스를 로그로 보여줍니다.
        }
    }*/
}
