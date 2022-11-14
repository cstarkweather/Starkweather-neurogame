using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/*
 NOTES:
 Jeœli dobrze rozumiem, to w sumie prawdopodobieñstwo wybuchu bomb sprowadza siê do tego, ¿e w po³owie rund wybuchaja, a w po³owie nie
 Podobnie z pojawianiem siê klucza
 Pytanie tylko, czy to ok, ¿e to taje 50% szans na przegran¹, 25% szans na nic i 25% szans na wygran¹
*/

public class GameBrain : MonoBehaviour
{
    private Animator animatorCam;

    [SerializeField]
    private int crystals = 0;

    [SerializeField]
    private int rounds = 8;
    [SerializeField]
    private int explosions = 4;
    [SerializeField]
    private int keys = 4;
    [SerializeField]
    private int allbombs = 4;
    [SerializeField]
    private int litbombs = 3;
    [SerializeField]
    private int allchests = 6;
    [SerializeField]
    private int litchests = 3;
    [SerializeField]
    private int punishment = 1; // in crystals
    [SerializeField]
    private int revenue = 1; // in crystals

    [SerializeField]
    private List<int> fireBombs = new List<int>(); // per round: (true / false)
    [SerializeField]
    private List<int> showKey = new List<int>(); // per round: (true / false)

    // assets prefabs
    [SerializeField]
    private GameObject bombLit;
    [SerializeField]
    private GameObject bombUnlit;
    [SerializeField]
    private GameObject chestLit;
    [SerializeField]
    private GameObject chestUnlit;
    [SerializeField]
    private GameObject key;

    // animated key box
    [SerializeField]
    private Animator kayBoxAnim;

    // for assets visualization
    [SerializeField]
    private Transform bombParent;
    [SerializeField]
    private Transform chestParent;
    private List<int> bombs = new List<int>();
    private List<int> chests = new List<int>();

    private bool waitingForDecision = false;
    private bool isDecisionMade = false;
    private int roundIndex = 0;

    [SerializeField]
    private UIController ui;

    public string jsonUrl;
    public string saveUrl;
    public string json;    
    public bool connected = false;

    private string game_id;
    private float roundStartTime = 0;
    public float[] timestamps = new float[6];

    

    void Start()
    {
        //Application.targetFrameRate = 15; // only for fps influence test
        animatorCam = GetComponent<Animator>();
        StartCoroutine(GetSettings());
    }

    private IEnumerator GetSettings() {
        using (UnityWebRequest www = UnityWebRequest.Get(jsonUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                json = www.downloadHandler.text;                
                JsonUtility.FromJsonOverwrite(json, this);
                connected = true;
                NewGame();
            }
            else
            {
                Debug.Log(www.error);
                ui.setEndScreen("Connection error");
            }
        }
    }

    private IEnumerator SendTimestamps()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", game_id);
        form.AddField("ts", timestampsAsString());

        using (UnityWebRequest www = UnityWebRequest.Post(saveUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                ui.setEndScreen("Connection error");
            }
        }
    }

    void Update()
    {
        if (!connected)
            return;

        AnimatorStateInfo animStateInf = animatorCam.GetCurrentAnimatorStateInfo(0);

        // if black screen appear, set new round or finish the game
        if (animStateInf.IsName("CameraBlack") && isDecisionMade == true)
        {
            timestamps[5] = Time.time - roundStartTime;
            StartCoroutine(SendTimestamps());

            if (roundIndex == rounds - 1)
            {
                animatorCam.SetFloat("mul", 0);
                ui.setEndScreen("Your final result is \r\n" + crystals + " CRYSTALS\r\nThanks for playing\r\n\r\n" + timestampsAsString() + "\r\n\r\n(debug: 'R' for replay)");
                isDecisionMade = false; // to avoid repeting
            }
            else
            {
                Debug.Log("timestapms: " + timestampsAsString());
                NewRound();
            }
        }

        // this is time for player actions
        if (animStateInf.IsName("CameraWaiting"))
        {
            // run once at the beginning of CameraWaiting
            if (isDecisionMade == false && waitingForDecision == false)
            {
                ui.setDescription("Waiting for action");
                timestamps[1] = Time.time - roundStartTime;
                waitingForDecision = true;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // try
                SaveTimestamp(2);
                animatorCam.SetTrigger("try");
                isDecisionMade = true;
                waitingForDecision = false;
                ui.setDescription("");
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                // escape
                SaveTimestamp(2);
                animatorCam.Play("CameraBlack", 0, 0f);
                isDecisionMade = true;
                waitingForDecision = false;
                ui.setDescription("");
            }
        }

        // for debug
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(0);
    }

    private void NewGame()
    {
        Debug.Log("New Game");
        crystals = 0;
        ui.setCrystals(0);
        resolveDataConflicts();

        // game_id based on date and time
        System.DateTime date = System.DateTime.Now;
        game_id = date.Year.ToString("0000") + date.DayOfYear.ToString("000") + date.Hour.ToString("00") + date.Minute.ToString("00") + date.Second.ToString("00");

        // randomly fill rounds
        fireBombs = RandomlyFilledList(rounds, explosions);
        showKey = RandomlyFilledList(rounds, keys);
        // start 1st round
        roundIndex = -1;
        NewRound();
    }

    private void NewRound()
    {
        // clear assets
        for (int i = 0; i < bombParent.childCount; i++)
            GameObject.Destroy(bombParent.GetChild(i).gameObject);
        for (int i = 0; i < chestParent.childCount; i++)
            GameObject.Destroy(chestParent.GetChild(i).gameObject);

        // spawn assets
        bombs = RandomlyFilledList(allbombs, litbombs);
        chests = RandomlyFilledList(allchests, litchests);

        float x_offset = 1.3f;
        for (int i=0; i < bombs.Count; i++)
        {
            GameObject b = (bombs[i] == 0) ? Instantiate(bombUnlit, bombParent) : Instantiate(bombLit, bombParent);
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i/2) * 0.75f, 0);
        }

        for (int i = 0; i < chests.Count; i++)
        {
            GameObject b = (chests[i] == 0) ? Instantiate(chestUnlit, chestParent) : Instantiate(chestLit, chestParent);
            x_offset = (i == 2 || i == 3) ? 1f : 0.75f;
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i / 2) * 0.75f, 0);
        }

        // Poisson for black screen
        animatorCam.SetFloat("mul", PoissonRandom(20));

        // clear activities and messages
        isDecisionMade = false;
        ui.setRounds(roundIndex + 1);
        ui.setInfo("");
        ui.setDescription("");
        ui.setEndScreen("");
        key.SetActive(false);

        // update round index
        roundIndex++;

        // reset timer
        roundStartTime = Time.time;
        timestamps = new float[6];
    }

    public void CheckBombs(string s)
    {
        SaveTimestamp(3);
        if (fireBombs[roundIndex] == 1)
        {
            Debug.Log("Boom!");
            animatorCam.Play("CameraExplode", 0, 0f);
            int shift = litbombs * punishment;
            crystals -= shift;
            ui.setCrystals(crystals);
            ui.setInfo(shift + " CRYSTALS LOST");
        }
    }

    public void CheckChests(string s)
    {
        SaveTimestamp(4);
        kayBoxAnim.SetTrigger("open");
        if (showKey[roundIndex] == 1)
        {
            Debug.Log("Key!");
            key.SetActive(true);
            int shift = litchests * revenue;
            crystals += shift;
            ui.setCrystals(crystals);
            ui.setInfo(shift + " CRYSTALS EARNED");
        }
    }

    public void SaveTimestamp(int i)
    {
        timestamps[i] = Time.time - roundStartTime;
    }

    private List<int> RandomlyFilledList(int size, int fill)
    {
        fill = Mathf.Min(size, fill);

        List<int> list = new List<int>();
        for (int i = 0; i < size; i++)
            list.Add(0);

        while (list.Sum() < fill)
            list[Random.Range(0, list.Count - 1)] = 1;
        return list;
    }

    private float PoissonRandom(double lambda)
    {
        System.Random rand = new System.Random();
        double lambda_exp = System.Math.Exp(-lambda);
        double p = 1;
        int randPoisson = -1;

        while (p > lambda_exp)
        {
            p *= rand.NextDouble();
            randPoisson++;
        }
        return randPoisson / (float)lambda;
    }

    private string timestampsAsString()
    {
        string timestamps_str = "";
        foreach (float t in timestamps)
        {
            int millis = Mathf.RoundToInt(t * 1000);
            timestamps_str += millis.ToString() + " ";
        }
        return timestamps_str;
    }

    private void resolveDataConflicts()
    {
        rounds = Mathf.Max(1, rounds);
        explosions = Mathf.Clamp(explosions, 0, rounds);
        keys = Mathf.Clamp(keys, 0, rounds);
        allbombs = Mathf.Clamp(allbombs, 0, 100);
        litbombs = Mathf.Clamp(litbombs, 0, allbombs);
        allchests = Mathf.Clamp(allchests, 0, 100);
        litchests = Mathf.Clamp(litchests, 0, allchests);
        punishment = Mathf.Max(0, punishment);
        revenue = Mathf.Max(0, revenue);
    }
}
