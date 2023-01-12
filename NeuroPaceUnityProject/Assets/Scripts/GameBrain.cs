using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GameBrain : MonoBehaviour
{
    private Animator animatorCam;
    private GameParams gameParams;
    private int crystals = 0;

    [SerializeField]
    private int currentTrial = 0;
    TrialType currentTrialType;

    private bool isTrialStarted = false;

    [SerializeField]
    private int[] fireBombs; // per round: (true / false)
    [SerializeField]
    private int[] showKey; // per round: (true / false)

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
    private Animator keyBoxAnim;

    // for assets visualization
    [SerializeField]
    private Transform bombParent;
    [SerializeField]
    private Transform chestParent;
    private int[] bombs;
    private int[] chests;
    private List<GameObject> bombsLit = new List<GameObject>();
    private List<GameObject> chestsLit = new List<GameObject>();

    [SerializeField]
    private UIController ui;

    public string jsonUrl;
    public string saveUrl;
    public string json;    
    public bool connected = false;

    private string user_id = "DefaultUser";
    private string game_id;
    private float roundStartTime = 0;
    public float[] timestamps = new float[6];
    private List<int> trials_pool = new List<int>();
    public float errorChance = 0.5f;

    void Start()
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        string gameUrl = Application.absoluteURL;
        if (Application.absoluteURL.IndexOf("=") > 0)
            user_id = Application.absoluteURL.Split('=')[1];
#endif
        Time.timeScale = 0;
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
                gameParams = JsonConvert.DeserializeObject<GameParams>(json);
                connected = true;
                Time.timeScale = 1;
                NewGame();
            }
            else
            {
                Debug.Log(www.error);
                ui.setEndScreen("Connection error.\r\nRetrying to connect...");
                StartCoroutine(GetSettings());
            }
        }
    }

    private IEnumerator SendTimestamps(int trial_index, string log)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", game_id);
        form.AddField("log", log);
        form.AddField("t", trial_index);        

        using (UnityWebRequest www = UnityWebRequest.Post(saveUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                ui.setEndScreen("Connection error");
                StartCoroutine(SendTimestamps(trial_index, log));
            }
        }
    }

    void Update()
    {
        if (!connected)
            return;

        AnimatorStateInfo animStateInf = animatorCam.GetCurrentAnimatorStateInfo(0);

        // timestamps (NOTE: timestamp[2] is writing on KeyDown)
        if (timestamps[0] == 0 && animStateInf.IsName("CameraEntryWalk"))
        {
            SaveTimestamp(0);
            isTrialStarted = true;
        }
        else if (timestamps[1] == 0 && animStateInf.IsName("CameraWaiting"))
        {
            SaveTimestamp(1);
            ui.setDescription("Waiting for action");
        }
        else if (timestamps[3] == 0 && animStateInf.IsName("CameraBraveWalk") && animStateInf.normalizedTime >= 0.5f)
        {
            SaveTimestamp(3);
            CheckBombs();
        }
        else if (timestamps[4] == 0 && animStateInf.IsName("CameraWaitForKey"))
        {
            SaveTimestamp(4);
            CheckChests();
        }
        else if (timestamps[5] == 0 && animStateInf.IsName("CameraBlack") && isTrialStarted)
        {
            SaveTimestamp(5);
            Debug.Log("timestapms: " + TimestampsAsString());
            StartCoroutine(SendTimestamps(currentTrial, TimestampsAsString()));
            isTrialStarted = false;

            if (currentTrial == trials_pool.Count - 1)
            {
                // last trial
                animatorCam.SetFloat("mul", 0);
                ui.setEndScreen("Your final result is \r\n" + crystals + " CRYSTALS\r\nThanks for playing\r\n\r\n" + TimestampsAsString() + "\r\n\r\n(debug: 'R' for replay)");
            }
            else
            {
                NewRound();
            }
        }

        // time to player action
        if (animStateInf.IsName("CameraWaiting"))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // try
                SaveTimestamp(2);
                animatorCam.SetTrigger("try");
                ui.setDescription("");
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                // escape
                SaveTimestamp(2);
                animatorCam.Play("CameraBlack", 0, 0f);
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
        //ResolveDataConflicts();

        // game_id based on date and time
        System.DateTime date = System.DateTime.Now;
        game_id = user_id + date.ToString("_yyyy-MM-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture);

        // fill trials pool
        foreach(TrialType trial in gameParams.trial_types)
        {
            for (int i = 0; i < gameParams.game_settings.number_of_trials_per_trial_type; i++)
            {
                trials_pool.Add(trial.id);
            }
        }

        /*
        // randomly fill trials scenario
        fireBombs = FillRandomly(trialsNumber, gameParams.game_settings.trials_to_explode);
        showKey = FillRandomly(trialsNumber, gameParams.game_settings.trials_to_show_key);
        // avoid appearing key when explosion (infinity loop risk if not resolveDataConflicts() before)
        while (fireBombs.Zip(showKey, (x, y) => x + y).Max() > 1)
            showKey = FillRandomly(trialsNumber, gameParams.game_settings.trials_to_show_key);
        */

        // shuffle trials_pool
        trials_pool = Shuffle(trials_pool, 15);

        // start 1st round
        currentTrial = -1;
        NewRound();
    }

    private void NewRound()
    {
        // update round index
        currentTrial++;

        // get current trial type
        currentTrialType = GetTrialType(currentTrial);

        // clear assets
        for (int i = 0; i < bombParent.childCount; i++)
            GameObject.Destroy(bombParent.GetChild(i).gameObject);
        for (int i = 0; i < chestParent.childCount; i++)
            GameObject.Destroy(chestParent.GetChild(i).gameObject);

        // spawn assets        
        bombs = FillRandomly(currentTrialType.bombs, currentTrialType.bombs_lit);
        chests = FillRandomly(currentTrialType.chests, currentTrialType.chests_lit);
        bombsLit = new List<GameObject>();
        chestsLit = new List<GameObject>();

        float x_offset = 1.4f;
        float y_rot = 20f;
        for (int i=0; i < bombs.Length; i++)
        {
            GameObject b = (bombs[i] == 0) ? Instantiate(bombUnlit, bombParent) : Instantiate(bombLit, bombParent);
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i / 2) + 0.45f, 0);
            if (bombs[i] == 1) bombsLit.Add(b);
        }
        x_offset = 1.3f;
        for (int i = 0; i < chests.Length; i++)
        {
            GameObject b = (chests[i] == 0) ? Instantiate(chestUnlit, chestParent) : Instantiate(chestLit, chestParent);
            x_offset = (i == 2 || i == 3) ? 1f : 0.75f;
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i / 2) * 0.75f, 0);
            b.transform.Rotate(new Vector3(0, (i % 2 == 0) ? y_rot : -y_rot, 0));
            if (chests[i] == 1) chestsLit.Add(b);
        }

        // Poisson for black screen
        animatorCam.SetFloat("mul", PoissonRandom(20));

        // clear activities and messages
        ui.setRounds(currentTrial + 1);
        ui.setInfo("");
        ui.setDescription("");
        ui.setEndScreen("");
        key.SetActive(false);

        // reset timer
        roundStartTime = Time.time;
        timestamps = new float[6];
    }

    private TrialType GetTrialType(int i)
    {
        int trial_type = trials_pool[i];

        foreach (TrialType t in gameParams.trial_types)
            if(t.id == trial_type)
                return t;

        // if nothing match get the first one
        return gameParams.trial_types[0];
    }

    public void CheckBombs()
    {
        if (errorChance < Random.value && bombsLit.Count > 0)
        {
            Debug.Log("Boom!");
            animatorCam.Play("CameraExplode", 0, 0f);
            int crystals_shift = currentTrialType.bombs_lit * gameParams.game_settings.cost_for_bomb_explosion;
            crystals -= crystals_shift;
            ui.setCrystals(crystals);
            ui.setInfo(crystals_shift + " CRYSTALS LOST");
            foreach (GameObject bomb in bombsLit)
            {
                foreach(ParticleSystem ps in bomb.GetComponentsInChildren<ParticleSystem>())
                    ps.Play();
            }
        }
    }

    public void CheckChests()
    {
        keyBoxAnim.SetTrigger("open");
        if (errorChance > Random.value)
        {
            Debug.Log("Key!");
            key.SetActive(true);
            int crystals_shift = currentTrialType.chests_lit * gameParams.game_settings.reward_for_chest;
            crystals += crystals_shift;
            ui.setCrystals(crystals);
            ui.setInfo(crystals_shift + " CRYSTALS EARNED");
            foreach(GameObject chest in chestsLit)
            {
                chest.GetComponent<Animator>().SetTrigger("open");
                chest.GetComponent<ParticleSystem>().Play();
            }
        }
    }

    public void SaveTimestamp(int i)
    {
        timestamps[i] = Time.time - roundStartTime;
    }

    private int[] FillRandomly(int size, int fill)
    {
        fill = Mathf.Min(size, fill);
        int[] list = new int[size];

        while (list.Sum() < fill)
            list[Random.Range(0, size - 1)] = 1;
        return list;
    }

    private List<int> Shuffle(List<int> list, int times)
    {
        int tmp, swapWith;
        for (int j = 0; j < times; j++)
        {
            for (int i = 0; i < list.Count; i++)
            {
                tmp = list[i];
                swapWith = Random.Range(0, list.Count - 1);
                list[i] = list[swapWith];
                list[swapWith] = tmp;
            }
        }
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

    private string TimestampsAsString()
    {
        string timestamps_str = "";
        foreach (float t in timestamps)
        {
            int millis = Mathf.RoundToInt(t * 1000);
            timestamps_str += millis.ToString() + " ";
        }
        return timestamps_str;
    }
    /*
    private void ResolveDataConflicts()
    {
        int rounds = trials_pool.Count;
        gameParams.game_settings.trials_to_explode = Mathf.Clamp(gameParams.game_settings.trials_to_explode, 0, rounds - gameParams.game_settings.trials_to_show_key);
        gameParams.game_settings.trials_to_show_key = Mathf.Clamp(gameParams.game_settings.trials_to_show_key, 0, rounds);
    }
    */
}

public class TrialType
{
    public int id;
    public int bombs;
    public int bombs_lit;
    public int chests;
    public int chests_lit;
}

public class GameSettings
{
    public int number_of_trials_per_trial_type;
    public int reward_for_chest;
    public int cost_for_bomb_explosion;
    public int trials_to_explode;
    public int trials_to_show_key;
    public List<float> allowed_error_from_target_ratio;
}

public class GameParams
{
    public List<TrialType> trial_types;
    public GameSettings game_settings;
}
