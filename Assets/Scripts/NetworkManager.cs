using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public GameObject playerPrefab;
    public GameObject[] spawnPoints;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        // Start the server on port 20521 with a maximum of 10 players
        Server.Start(10, 20521);
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    public Player InstantiatePlayer()
    {
        // handle where the player will spawn
        int choice = Random.Range(0, 7);

        return Instantiate(playerPrefab, spawnPoints[choice].transform.position, spawnPoints[choice].transform.rotation).GetComponent<Player>();
    }
}
