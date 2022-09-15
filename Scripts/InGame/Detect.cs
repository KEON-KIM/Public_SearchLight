using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detect : MonoBehaviour
{
    //[SerializeField] float timeInterval;
    [SerializeField] float distance;
    [SerializeField] float angle;
    //[SerializeField] HashSet<Player> detected;
    Dictionary<string, Player> detectedPlayers;
    Dictionary<string, bool> detectedPlayersFlag;
    //[SerializeField] List<Player> detectedList;

    bool isOnDetectCooltime;

    //public float Distance { get { return distance; } set { distance = value; } }
    //public float Angle { get { return angle; } set { angle = value; } }

    private void Awake()
    {
        //detected = new HashSet<Player>();
        detectedPlayers = new Dictionary<string, Player>();
        detectedPlayersFlag = new Dictionary<string, bool>();
    }


    public void Init(float distance_, float angle_)
    {
        distance = distance_;
        angle = angle_;
        var collider = GetComponent<SphereCollider>().radius = distance;
    }
    /*
    * 
    *  float playerSightDigree 시야
    *  
    *  arccos(player.front와 other.position - player.position의 내적 / 
    *  player.front.magnitude * position - player.position.magnitude)
    *  
    *  위를 수행하면 각도를 구할 수 있음 해당 각도가 sightDigree /2 보다 작다면 시야 범위 안에있는것
    *
    *  
    */
    bool CheckIsTargetInSight(Vector3 otherPos)
    {
        Vector3 playerForward = transform.forward;
        Vector3 playerToTarget = otherPos - transform.position;
        float angleToTarget = Mathf.Acos(Vector3.Dot(playerForward, playerToTarget) / (playerForward.magnitude * playerToTarget.magnitude)) * Mathf.Rad2Deg;
        //Debug.Log(angleToTarget);
        if (angle / 2 > angleToTarget)
        {
            return true;
        }

        return false;
            
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Player" && other.gameObject != transform.parent.gameObject)
        {
            if(detectedPlayers.ContainsKey(other.name))
            {
                bool isInSight = CheckIsTargetInSight(other.transform.position);
                if (isInSight == true && detectedPlayersFlag[other.name] == false)
                {
                    detectedPlayers[other.name].setRendererActive(true);
                    detectedPlayersFlag[other.name] = true;
                    Debug.Log("InSight!");
                }
                else if(isInSight == false && detectedPlayersFlag[other.name] == true)
                {
                    detectedPlayers[other.name].setRendererActive(false);
                    detectedPlayersFlag[other.name] = false;
                    Debug.Log("OutSight");
                }
            }
            else
            {
                detectedPlayers.Add(other.name, other.GetComponent<Player>());
                detectedPlayersFlag[other.name] = false;
                Debug.Log(string.Format("{0} added in detectedPlayers dictionary", other.name));
            }
        }
        //if (detected.Contains(other.gameObject) == false)
        //{
        //    if (other.tag == "Player")
        //    {
        //        if()
        //        detected.Add(other.gameObject);
        //    }
        //}
        //else
        //{
        //    if (CheckIsTargetInSight(other.transform.position))
        //    {
        //        otherPlayer.setRendererActive(true);
        //    }
        //    else
        //    {

        //    }
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            if (detectedPlayers.ContainsKey(other.name) == false)
            {
                Debug.LogError("Unaddeded gameobject exit in detectedPlayers");
            }
            else
            {
                detectedPlayers[other.name].setRendererActive(false);
                detectedPlayers.Remove(other.name);
                detectedPlayersFlag.Remove(other.name);
                //var otherPlayer = other.gameObject.GetComponent<Player>();
                //if(otherPlayer)
                //{
                //    otherPlayer.setRendererActive(false);
                //    detected.Remove(other.gameObject);
                //}

            }
        }

        
    }
}
