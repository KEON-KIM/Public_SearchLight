using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BackEnd;
using BackEnd.Tcp;
using TMPro;

public class LoadUIManager : MonoBehaviour
{
    public static LoadUIManager instance = null;

    public Image progressBar;
    public TextMeshProUGUI progressText;

    public GameObject UserCardParent;

    private float loadTimer; // 페이크 로드 타이머
    private int numOfClient = -1; // 생성해야하는 유저 수

    private const string MMR_RECORD = "MMR : {0}";
    private const string NUM_RECORD = "MATCH : {0}";
    private const string WIN_RECORD = "WIN : {0}";
    private const string DEFEAT_RECORD = "DEFEAT : {0}";
    private const string PROGRESS_PERCENTAGE = "{0:N1} %";

    [SerializeField]
    private List<GameObject> userPrefabsObject = new List<GameObject>();

    [SerializeField]
    private List<GameObject> userCardList = new List<GameObject>();
    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
        Debug.Log("ENTER LOADROOM SCENE#" + ServerMatchManager.GetInstance().gameUserRecords.Count);
        GameManager.OnGameReady += OnGameReady;
    }
    void Start()
    {
        loadTimer = 0.0f;
        var matchInstance = ServerMatchManager.GetInstance();
        if (matchInstance == null)
        {
            return;
        }

        numOfClient = matchInstance.gameUserRecords.Count;

        if (numOfClient <= 0)
        {
            Debug.LogError("numOfClient가 0이하입니다.");
            return;
        }

        /*for (int i = 0; i < numOfClient; i++)
        {
            GameObject user = GameObject.Instantiate(userCardPrefab, Vector3.zero, Quaternion.identity, UserCardParent.transform);
            user.transform.localPosition = new Vector3(user.transform.localPosition.x, user.transform.localPosition.y, 0.0f);
            userObject.Add(user);
        }*/

        
        int index = 0;
        foreach (var record in matchInstance.gameUserRecords.OrderByDescending(x => x.Key))
        {
            var name = record.Value.m_nickname;
            string score = string.Empty;

            if (matchInstance.nowMatchType == MatchType.MMR)
            {
                score = string.Format(MMR_RECORD, record.Value.m_mmr);
            }
            else if (matchInstance.nowMatchType == MatchType.Point)
            {
                score = string.Format(MMR_RECORD, record.Value.m_points);
            }

            var data = userCardList[index].GetComponentsInChildren<TextMeshProUGUI>();

            data[0].text = name;
            data[1].text = string.Format(MMR_RECORD, score);
            data[2].text = string.Format(NUM_RECORD, record.Value.m_numberOfMatches);
            data[3].text = string.Format(WIN_RECORD, record.Value.m_numberOfWin);
            data[4].text = string.Format(DEFEAT_RECORD, record.Value.m_numberOfDefeats);

            var tmp = userCardList[index].transform;
            var modelObject = tmp.GetChild(1);
            Vector3 tmpVector = new Vector3(modelObject.position.x, modelObject.position.y - 1f, modelObject.position.z);
            GameObject user = GameObject.Instantiate(userPrefabsObject[record.Value.m_characterIdx], tmpVector, Quaternion.Euler(0, 180.0f, 0), tmp);
            modelObject.gameObject.SetActive(false);
            index++;
        }
    }
    private void OnGameReady()
    {
        loadTimer += Time.unscaledDeltaTime * 0.25f;// 로딩 페이크 타임
        progressBar.fillAmount = Mathf.Lerp(0.0f, 0.8f, loadTimer);
        progressText.text = string.Format(PROGRESS_PERCENTAGE, progressBar.fillAmount * 100);
    }

}
