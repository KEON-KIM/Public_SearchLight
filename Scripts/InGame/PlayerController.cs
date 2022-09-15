using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{


    [SerializeField] float moveSpeed;

    public Vector3 currVelocity { get; private set; }
    public bool isMove { get; private set; }

    Rigidbody playerRig;
    Animator playerAnim;
    public bool isOwner;
    bool isDead;

    float rotSpeed = 30.0f;


    
    //Riggedbody 및 Animator초기화, 인풋매니저를 통해 조이스틱 이벤트에 따른 이동 애니메이션 액션 바인드
    void Awake()
    {
        InitCommon();
    }

    void InitCommon()
    {
        playerRig = GetComponent<Rigidbody>();
        playerAnim = GetComponent<Animator>();
        moveSpeed = GetComponent<Player>().MoveSpeed;
        this.isDead = false;
        this.isMove = false;
        this.currVelocity = new Vector3(0, 0, 0);
    }

    public void InitOwner()
    {
        isOwner = true;
    }

    public void Move(Vector3 newDir) // Owner : Player.Update {Move}에 의해 호출 -> InputManager.Jostick에 의해 newVelocity 생성 
    {
        /*if (isOwner.Equals(false) && newVelocity.Equals(Vector3.zero))
        {
            SetAnimMove();
        }*/

        if (isDead)
            return;

        Vector3 moveVector = newDir * moveSpeed;
        Rotation(newDir);
        playerRig.MovePosition(playerRig.position + moveVector * Time.fixedDeltaTime);
    }

    public void LookAt(Vector3 lookPoint)
    {
        if (lookPoint == Vector3.zero)
        {
            Debug.Log("LookAt : Zero");
            return;
        }
        Quaternion newRotation = Quaternion.LookRotation(lookPoint);
        playerRig.rotation = newRotation;
    }
    /*
    public void Initialize()
    {
        this.isLive = true;
        this.isMove = false;
        this.currVelocity = new Vector3(0, 0, 0);
    }*/

    //void Update()
    //{
    //    if (ServerManager.GetInstance() == null)
    //    {
    //        return;
    //    }

    //    if (isDead)
    //    {
    //        return;
    //    }

    //    if (isMove)
    //    {
    //        Move();
    //    }
    //}

    private void FixedUpdate()
    {
        if (ServerManager.GetInstance() != null && !isDead && isMove)
            Move();
    }

    public void SetMoveVector(Vector3 vector)
    {
        currVelocity = vector;

        if (vector == Vector3.zero)
        {
            isMove = false;
            playerRig.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            SetAnimStop();
        }
        else
        {
            isMove = true;
            playerRig.constraints = RigidbodyConstraints.None;
            playerRig.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            SetAnimMove();
        }
    }

    public void SetAnimMove()
    {
        playerAnim.SetBool("isWalking", true);
        playerAnim.SetFloat("walkBlend", 0.5f);
    }

    public void SetAnimStop()
    {
        playerAnim.SetBool("isWalking", false);
        playerAnim.SetFloat("walkBlend", 0);
    }
   
    public void Rotation(Vector3 newRotate)
    {
        if (newRotate.Equals(Vector3.zero))
        {
            return;
        }
        if (Quaternion.Angle(playerRig.rotation, Quaternion.LookRotation(newRotate)) < Quaternion.kEpsilon)
        {
            return;
        }
        playerRig.rotation = Quaternion.Lerp(playerRig.rotation, Quaternion.LookRotation(newRotate), Time.deltaTime * rotSpeed);
    }

    //애니메이션 이벤트
    public void AnimNotify_OnReloadStart()
    {
        Debug.Log("OnReloadStart");
        //SetPlayerState(PlayerState.Interaction);
    }

    public void AnimNotify_OnReloadEnd()
    {
        Debug.Log("OnReloadEnd");
        //SetPlayerState(PlayerState.Idle);
    }

    public void SetDead()
    {
        GetComponent<CapsuleCollider>().center = new Vector3(0, 0.5f, 0);
        playerAnim.SetBool("isShooting", false);
        SetAnimStop();
        isDead = true;
        playerAnim.SetBool("isDead", true);
    }

    private void Move()
    {
        Vector3 moveInput = Vector3.forward * currVelocity.z +
            Vector3.right * currVelocity.x;
        
        Move(moveInput);
    }

    /*private void Update() // 호스트나 비호스트나 호출 노상관
    {
        if (currVelocity.Equals(Vector3.zero))
        {
            if (isOwner)
            {
                SetAnimStop();
            }
            isMove = false;
            return;
        }
        isMove = true;
        playerRig.MovePosition(playerRig.position + currVelocity * Time.fixedDeltaTime);
    }*/

    public void SetPlayerPosition(Vector3 pos)
    {
        playerRig.position = pos;
    }

}
