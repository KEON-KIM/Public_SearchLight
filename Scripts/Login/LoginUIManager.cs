using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BackEnd;
using static BackEnd.SendQueue;
using Battlehub.Dispatcher;
using TMPro;

public class LoginUIManager : MonoBehaviour
{
    private static LoginUIManager instance = null;

    public GameObject mainTitle;
    public GameObject customLoginObject;
    public GameObject signUpObject;
    public GameObject errorObject; // Error Message Window
    public GameObject touchStart; // Press screen to play
    public GameObject versionObject;

    [SerializeField]
    private GameObject loadingObject;
    
    [SerializeField]
    private FadeAnimation fadeObject;
    private TMP_InputField[] loginField; // Login Field / 0 : ID, 1 : PW
    private TMP_InputField[] signUpField;

    [SerializeField]
    private TextMeshProUGUI errorText;
    private string tempUserNickname = null;

    private const byte ID_INDEX = 0;
    private const byte PW_INDEX = 1;
    private const string VERSION_STR = "Ver {0}";
    public static LoginUIManager GetInstance()
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
    }

    void Start()
    {
        touchStart.SetActive(true);
        mainTitle.SetActive(true);
        versionObject.SetActive(true);

        loadingObject.SetActive(false);
        signUpObject.SetActive(false); 
        errorObject.SetActive(false);
        customLoginObject.SetActive(false);
        

        loginField = customLoginObject.GetComponentsInChildren<TMP_InputField>(); // ID : 0  / PW : 1
        signUpField = signUpObject.GetComponentsInChildren<TMP_InputField>(); // ID : 0  / PW : 1

        var fade = GameObject.FindGameObjectWithTag("Fade");
        if (fade != null)
        {
            fadeObject = fade.GetComponent<FadeAnimation>();
        }
    }

    public void TouchStart()
    {
        //ServerManager.GetInstance().CustomSignUp(); // 테스트 - 성공
        loadingObject.SetActive(true); // 로딩 동작
        ServerManager.GetInstance().BackendTokenLogin((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (result)
                {
                    ChangeLobbyScene(); // 토큰 로그인의 경우-> 로비로 접속
                    ServerMatchManager.GetInstance().SettingHanddler(); // 매치 서버를 위한 핸들러 셋팅
                    Debug.Log("토큰 로그인 성공");
                    return;
                }
                loadingObject.SetActive(false); // 로딩 종료
                if (!error.Equals(string.Empty)) // 서버에 접속 불가능 에러
                {
                    errorText.text = "Failed to get User Data\n\n" + error;
                    errorObject.SetActive(true);
                    Debug.Log("토큰 로그인 실패");
                    return;
                }
                
                Debug.Log("로그인 필요 : 로그인 창 실행");
                touchStart.SetActive(false);
                customLoginObject.SetActive(true);
            });
        });
    }
    public void Login() // 로그인 버튼 클릭 시
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = loginField[ID_INDEX].text;
        string pw = loginField[PW_INDEX].text;

        if (id.Equals(string.Empty) || pw.Equals(string.Empty)) // InputField가 둘 중 하나라도 비어있을 경우
        {
            errorText.text = "ID 혹은 PW 를 먼저 입력해주세요.";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        ServerManager.GetInstance().CustomLogin(id, pw, (bool result, string error) => // 서버에 커스텀 로그인 요청 (서버에 저장되어있는 회원 정보기반)
        {
            Dispatcher.Current.BeginInvoke(() => // 응답 받을 때까지 비동기화 처리
            {
                if (!result) // 로그인 에러
                {
                    loadingObject.SetActive(false);
                    errorText.text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                Debug.Log("로그인 성공 !!");
                ChangeLobbyScene();
                ServerMatchManager.GetInstance().SettingHanddler();
            });
        });
    }

    public void SignIn() // 회원가입(SignIn) 버튼 클릭 시
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = signUpField[ID_INDEX].text;
        string pw = signUpField[PW_INDEX].text;
        tempUserNickname = id; // 아이디로 기본 닉네임 설정

        if (id.Equals(string.Empty) || pw.Equals(string.Empty))
        {
            errorText.text = "Please Checking your ID or Password";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        ServerManager.GetInstance().CustomSignIn(id, pw, (bool result, string error) => // 서버에 커스텀 회원가입 요청
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorText.text = "Error : You can't sign up\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }


    public void ActiveLoginObject() // 성공적으로 회원가입 되었을 때 또는 회원가입 취소 버튼을 누를 경우 로그인창 실행
    {
        Dispatcher.Current.BeginInvoke(() =>
        {
            mainTitle.SetActive(false);
            touchStart.SetActive(false);
            customLoginObject.SetActive(true);
            signUpObject.SetActive(false);
            errorObject.SetActive(false);
            loadingObject.SetActive(false);
        });
    }

    public void ActiveSignUpObject() // 회원가입(Sign up) 버튼을 누를 경우 로그인창 실행
    {
        Dispatcher.Current.BeginInvoke(() =>
        {
            mainTitle.SetActive(false);
            touchStart.SetActive(false);
            customLoginObject.SetActive(false);
            signUpObject.SetActive(true);
            errorObject.SetActive(false);
        });
    }

    void ChangeLobbyScene()
    {
        GameManager.GetInstance().ChangeState(GameManager.GameState.MatchLobby);
    }


}
