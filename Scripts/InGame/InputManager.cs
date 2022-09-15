using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Protocol;
using BackEnd;
using BackEnd.Tcp;

public class InputManager : MonoBehaviour
{
    private static InputManager instance = null;

    [SerializeField] Joystick joystick;
    //[SerializeField] Button fireButton;
    [SerializeField] ShotButton fireButton;

    Action startFireAction;
    Action stopFireAction;

    bool isMove = false;
    Action reloadAction;
    Action weaponChangeAction;
    private SessionId userPlayerIndex;

    public static InputManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("InputManager instance does not exist.");
            return null;
        }
        return instance;
    }

    void Awake()
    {
        if (instance != null)
            Destroy(instance);

        instance = this;
    }
    void Start()
    {
        GameManager.InGame += MoveInput;
        GameManager.LateInGame += SendNoMoveMessage;
        userPlayerIndex = Backend.Match.GetMySessionId();
    }

    void MoveInput()
    {
        if(!joystick)
        {
            Debug.Log("조이스틱을 찾을 수 없습니다.");
            return;
        }
        int keyCode = 0;
        isMove = false;


        keyCode |= KeyEventCode.MOVE;
        Vector3 moveVector = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        moveVector = Vector3.Normalize(moveVector);

        if(keyCode <= 0)
        {
            return; // NONE;
        }

        if(!joystick.isInputEnable)
        {
            isMove = false;
            return;
        }

        isMove = true;
        KeyMessage msg;
        msg = new KeyMessage(keyCode, moveVector);
        if(ServerMatchManager.GetInstance().IsHost())
        {
            Debug.Log("호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }
        
        else
        {
            Debug.Log("비 호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }
    }

    void SendNoMoveMessage()
    {
        int keyCode = 0;
        if (!isMove && InGameManager.instance.IsMyPlayerMove())
        {
            keyCode |= KeyEventCode.NO_MOVE;
        }
        if (keyCode == 0)
        {
            return;
        }
        //Debug.Log("멈춰라 메세지를 송신합니다.");
        Vector3 playerPos = InGameManager.instance.players[userPlayerIndex].GetPosition();
        KeyMessage msg = new KeyMessage(keyCode, playerPos);
        if (ServerMatchManager.GetInstance().IsHost())
        {
            ServerMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }
        else
        {
            ServerMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }
    }

    

    public float GetJoystickAxis(string axis)
    {
        if (axis == "Vertical")
            return joystick.Vertical;
        else if (axis == "Horizontal")
            return joystick.Horizontal;
        else
        {
            Debug.LogError("Wrong joystick axis call");
            return 0;
        }
    }
    // anim
     public void AddJoystickMoveAction(Action act)
     {
         joystick.OnJoystickMove += act;
     }

    public void AddJoystickStopAction(Action act)
    {
        joystick.OnJoystickStop += act;
    }

    public void AddFireAction(Action startAction, Action stopAction)
    {
        startFireAction += startAction;
        stopFireAction += stopAction;
    }
    public void AddReloadAction(Action reloadAction_)
    {
        reloadAction += reloadAction_;
    }
    public void AddWeaponChangeAction(Action changeAction)
    {
        weaponChangeAction += changeAction;
    }

    public void OnFireButtonDown()
    {
        if(startFireAction == null)
        {
            Debug.LogError("startFireAction was not intialized");
            return;
        }
        startFireAction(); // AnimAction
    }

    public void OnFireButtonUp()
    {
        if (stopFireAction == null)
        {
            Debug.LogError("stopFireAction was not intialized");
            return;
        }
        stopFireAction();
    }

    public void  OnReloadButtonClick()
    {
        if(reloadAction == null)
        {
            Debug.LogError("reloadAction was not initialized");
        }

        reloadAction();
    }

    public void OnWeaponChangeButtonClick()
    {
        if(weaponChangeAction == null)
        {
            Debug.LogError("weaponChangeAction was not initialized");
        }
        weaponChangeAction();
    }


}
