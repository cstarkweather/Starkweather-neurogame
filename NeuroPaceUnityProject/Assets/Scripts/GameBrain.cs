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
    private game_params game_params;
    private int crystals = 0;

    [SerializeField]
    private int current_trial = 0;
    TrialType current_trial_type;

    private bool is_trial_started = false;

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
    private List<GameObject> bombsAll = new List<GameObject>();
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
    
    public int trials_where_player_went_forward = 0;
    public int trials_that_exploded_so_far = 0;
    public int trials_that_rewarded_so_far = 0;

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
                game_params = JsonConvert.DeserializeObject<game_params>(json);
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
            is_trial_started = true;
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
        else if (timestamps[5] == 0 && animStateInf.IsName("CameraBlack") && is_trial_started)
        {
            SaveTimestamp(5);
            //Debug.Log("timestapms: " + TimestampsAsString());
            StartCoroutine(SendTimestamps(current_trial, TimestampsAsString()));
            is_trial_started = false;

            if (current_trial == trials_pool.Count - 1)
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
                trials_where_player_went_forward += 1;
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

        // game_id based on date and time
        System.DateTime date = System.DateTime.Now;
        game_id = user_id + date.ToString("_yyyy-MM-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture);

        // fill trials pool
        foreach(TrialType trial in game_params.trial_types)
        {
            for (int i = 0; i < game_params.game_settings.number_of_trials_per_trial_type; i++)
            {
                trials_pool.Add(trial.id);
            }
        }

        // shuffle trials_pool
        trials_pool = Shuffle(trials_pool, 15);

        // start 1st round
        current_trial = -1;
        NewRound();
    }

    private void NewRound()
    {
        // update round index
        current_trial++;

        // get current trial type
        current_trial_type = GetTrialType(current_trial);

        // clear assets
        for (int i = 0; i < bombParent.childCount; i++)
            GameObject.Destroy(bombParent.GetChild(i).gameObject);
        for (int i = 0; i < chestParent.childCount; i++)
            GameObject.Destroy(chestParent.GetChild(i).gameObject);

        // spawn assets        
        bombs = FillRandomly(current_trial_type.bombs, current_trial_type.bombs_lit);
        chests = FillRandomly(current_trial_type.chests, current_trial_type.chests_lit);
        bombsAll = new List<GameObject>();
        chestsLit = new List<GameObject>();

        float x_offset = 1.4f;
        float y_rot = 20f;
        for (int i=0; i < bombs.Length; i++)
        {
            GameObject b = (bombs[i] == 0) ? Instantiate(bombUnlit, bombParent) : Instantiate(bombLit, bombParent);
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i / 2) + 0.45f, 0);
            bombsAll.Add(b);
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
        ui.setRounds(current_trial + 1);
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

        foreach (TrialType t in game_params.trial_types)
            if(t.id == trial_type)
                return t;

        // if nothing match get the first one
        return game_params.trial_types[0];
    }

    public void CheckBombs()
    {
        if (PlayEvent(trials_that_exploded_so_far))
        {
            //Debug.Log("Boom!");
            trials_that_exploded_so_far += 1;
            animatorCam.Play("CameraExplode", 0, 0f);
            int crystals_shift = current_trial_type.bombs_lit * game_params.game_settings.cost_for_bomb_explosion;
            crystals -= crystals_shift;
            ui.setCrystals(crystals);
            ui.setInfo(crystals_shift + " CRYSTALS LOST");
            foreach (GameObject bomb in bombsAll)
            {
                foreach(ParticleSystem ps in bomb.GetComponentsInChildren<ParticleSystem>())
                    ps.Play();
            }
        }
    }

    public void CheckChests()
    {
        keyBoxAnim.SetTrigger("open");
        if (PlayEvent(trials_that_rewarded_so_far))
        {
            //Debug.Log("Key!");
            trials_that_rewarded_so_far += 1;
            key.SetActive(true);
            int crystals_shift = current_trial_type.chests_lit * game_params.game_settings.reward_for_chest;
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

    public bool PlayEvent(float trials_that_played_so_far)
    {
        float trials_to_explode_ratio = (float)game_params.game_settings.trials_to_explode / (float)trials_pool.Count;
        float a = Mathf.Abs((trials_that_played_so_far + 1) / ((float)trials_where_player_went_forward) - trials_to_explode_ratio);
        float b = Mathf.Abs((trials_that_played_so_far) / ((float)trials_where_player_went_forward) - trials_to_explode_ratio);
        float t = ((float)current_trial) / ((float)trials_pool.Count - 1);
        float epsilon = Mathf.Lerp(game_params.game_settings.allowed_error_from_target_ratio[0], game_params.game_settings.allowed_error_from_target_ratio[1], t);
        //Debug.Log("x: " + a.ToString() + " y: " + b.ToString() + " t: " + t + " epsilon: " + epsilon.ToString());

        if (Mathf.Abs(a - b) > epsilon)
            return Random.value > 0.5f;
        else
            return a < b;
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
    public int number_of_groups;
    public int reward_for_chest;
    public int cost_for_bomb_explosion;
    public int trials_to_explode;
    public int trials_to_show_key;
    public List<float> allowed_error_from_target_ratio;
}

public class game_params
{
    public List<TrialType> trial_types;
    public GameSettings game_settings;
}
