using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum LobbyAnimType { Inspect, Check_Shoe, Turn_Right, Turn_Left}
public class LobbyAnimationPlayer : MonoBehaviour
{
    [SerializeField] LobbyAnimType animType;
    [SerializeField] ParticleSystem selectParticle;
    [SerializeField] CharacterIndex classType;

    Animator lobbyAnimator;

    public CharacterIndex ClassType { get { return classType; } private set { } }
    

    private void Awake()
    {
        lobbyAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        switch (animType)
        {
            case LobbyAnimType.Inspect:
                lobbyAnimator.SetInteger("animType", 0);
                break;
            case LobbyAnimType.Check_Shoe:
                lobbyAnimator.SetInteger("animType", 1);
                break;
            case LobbyAnimType.Turn_Right:
                lobbyAnimator.SetInteger("animType", 2);
                break;
            case LobbyAnimType.Turn_Left:
                lobbyAnimator.SetInteger("animType", 3);
                break;
        }
    }

    public void Select()
    {
        selectParticle.gameObject.SetActive(true);
        lobbyAnimator.SetTrigger("select");
    }

    public void Cancel()
    {
        selectParticle.gameObject.SetActive(false);
        //lobbyAnimator.SetTrigger("cancel");
    }
}
