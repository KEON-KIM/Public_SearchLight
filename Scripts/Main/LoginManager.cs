using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    private static LoginManager instance = null;

    public static LoginManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("LoginManager instance does not exist.");
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
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
