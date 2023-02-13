using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;

public class GameBrain : MonoBehaviour
{
    private Animator animatorCam;
    private GameParams game_params;
    private int crystals = 0;

    [SerializeField]
    private int current_trial = 0;
    TrialType current_trial_type;

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
    [SerializeField]
    private GameObject gem;
    [SerializeField]
    private GameObject RubyEarned;
    [SerializeField]
    private GameObject RubyLost;
    [SerializeField]
    private GameObject FadeBlack;

    // animated key box
    [SerializeField]
    private Animator keyBoxAnim;

    // for assets visualization
    [SerializeField]
    private Transform bombParent;
    [SerializeField]
    private Transform chestParent;
    [SerializeField]
    private Transform rubiesUIParent;
    private int[] bombs;
    private int[] chests;
    private List<GameObject> bombsAll = new List<GameObject>();
    private List<GameObject> chestsLit = new List<GameObject>();
    private List<GameObject> gems = new List<GameObject>();

    // audio
    public AudioSource earnedRubiesSFX;
    public AudioSource lostRubiesSFX;
    public AudioSource actionWalk;
    public AudioSource actionSkip;

    [SerializeField]
    private UIController ui;

    public string jsonUrl;
    public string saveUrl;
    public string json_str = "";
    private bool is_game_finished = false;

    private string user_id = "DefaultUser";
    private string game_id;
    private float roundStartTime = 0;
    public float[] timestamps = new float[6];
    private List<TrialType> trials_pool = new List<TrialType>();
    
    public int trials_where_player_went_forward = 0;
    public int trials_that_exploded_so_far = 0;
    public int trials_that_rewarded_so_far = 0;
    private float explosion_avg_chance = 0;
    private float key_avg_chance = 0;
    private int trials_count = 0;

    void Start()
    {
        //Application.targetFrameRate = 15; // only for fps influence test
        string mode = "local"; // "web" or "local"

#if UNITY_WEBGL && !UNITY_EDITOR
        string gameUrl = Application.absoluteURL;
        if (Application.absoluteURL.IndexOf("=") > 0)
            user_id = Application.absoluteURL.Split('=')[1];
        mode = "web";
#endif

        FadeBlack.SetActive(true);
        animatorCam = GetComponent<Animator>();
        animatorCam.SetFloat("mul", 0);

        if (mode == "web")
        {
            Debug.Log("Load JSON from URL");
            StartCoroutine(GetJSONFromURL());
        }
        else if (mode == "local")
        {
            string game_settings_path = Directory.GetParent(Application.dataPath) + "/game_settings.json";
            if (File.Exists(game_settings_path)) {
                json_str = File.ReadAllText(game_settings_path);
            }
            else { ui.printEndScreen("No \"game_settings.json\" file in game directory."); }
        }
        else
        {
            ui.printEndScreen("Error. Undefined mode.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (is_game_finished) {
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(0);
            return;
        }

        if (json_str=="")
            return;

        if (game_params == null)
            InitializeGame();

        AnimatorStateInfo animStateInf = animatorCam.GetCurrentAnimatorStateInfo(0);

        // timestamps (NOTE: timestamp[2] is writing on KeyDown)
        if (timestamps[0] == 0 && animStateInf.IsName("CameraEntryWalk"))
        {
            FadeBlack.SetActive(false);
            animatorCam.SetFloat("mul", 1);
            SaveTimestamp(0);
        }
        else if (timestamps[1] == 0 && animStateInf.IsName("CameraWaiting"))
        {
            SaveTimestamp(1);
            ui.printDescription("Waiting for action");
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
        else if (timestamps[0] != 0 && animStateInf.IsName("CameraBlack"))
        {            
            SaveTimestamp(5);
            StartCoroutine(SendTimestamps(current_trial, TimestampsAsString()));
            FadeBlack.SetActive(true);
            animatorCam.SetFloat("mul", PoissonRandom(20));

            if (current_trial+1 < trials_pool.Count)
            {
                NewRound();
            }
            else
            {
                // last trial
                animatorCam.SetFloat("mul", 0);
                ui.printEndScreen("Your final result is \r\n" + crystals + " CRYSTALS\r\nThanks for playing\r\n\r\n" + TimestampsAsString() + "\r\n\r\n(debug: 'R' for replay)");
                ClearAssets();
                is_game_finished = true;
            }
        }

        // if time to player action
        if (animStateInf.IsName("CameraWaiting"))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // try
                SaveTimestamp(2);
                animatorCam.SetTrigger("try");
                ui.printDescription("");
                trials_where_player_went_forward += 1;
                actionWalk.Play();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                // escape
                SaveTimestamp(2);
                animatorCam.Play("CameraBlack", 0, 0f);
                ui.printDescription("");
                actionSkip.Play();
            }
        }
       
    }

    private void InitializeGame()
    {
        game_params = JsonConvert.DeserializeObject<GameParams>(json_str);
        // parse game_params
        int trials_where_zero_bombs_lit = 0;
        for (int i=0; i<game_params.trial_types.Count; i++)
        {
            // increase lit assets to all assets if needed
            game_params.trial_types[i].bombs_lit = Mathf.Min(game_params.trial_types[i].bombs, game_params.trial_types[i].bombs_lit);
            game_params.trial_types[i].chests_lit = Mathf.Min(game_params.trial_types[i].chests, game_params.trial_types[i].chests_lit);
            // count trialas where 0 bombs lit
            if (game_params.trial_types[i].bombs_lit == 0)
                trials_where_zero_bombs_lit += 1;
        }
        trials_count = game_params.trial_types.Count * game_params.game_settings.number_of_trials_per_trial_type;
        explosion_avg_chance = (float)game_params.game_settings.trials_to_explode / (float)(trials_count - (trials_where_zero_bombs_lit * game_params.game_settings.number_of_trials_per_trial_type));
        key_avg_chance = (float)game_params.game_settings.trials_to_show_key / (float)(trials_count - game_params.game_settings.trials_to_explode);
        FillTrialsPool();

        Debug.Log("Params Loaded");
        ui.printCrystals(crystals);

        // game_id based on date and time
        System.DateTime date = System.DateTime.Now;
        game_id = user_id + date.ToString("_yyyy-MM-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture);

        // start 1st round
        current_trial = -1;
        // start camera
        animatorCam.SetFloat("mul", 1);
        NewRound();
    }

    private void NewRound()
    {
        // update round index
        current_trial++;
        current_trial_type = trials_pool[current_trial];
        ui.printRounds(current_trial + 1);        

        // clear activities, messages and assets
        ui.printInfo("");
        ui.printDescription("");
        ui.printEndScreen("");
        key.SetActive(false);
        ClearAssets();

        // spawn assets        
        bombs = FillRandomly(current_trial_type.bombs, current_trial_type.bombs_lit);
        chests = FillRandomly(current_trial_type.chests, current_trial_type.chests_lit);
        bombsAll = new List<GameObject>();
        chestsLit = new List<GameObject>();
        gems = new List<GameObject>();

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
            //x_offset = (i == 2 || i == 3) ? 1f : 0.75f;
            x_offset = 0.75f;
            b.transform.localPosition = new Vector3((i % 2 == 0) ? x_offset : -x_offset, Mathf.FloorToInt(i / 2) * 0.75f, 0);
            b.transform.Rotate(new Vector3(0, (i % 2 == 0) ? y_rot : -y_rot, 0));
            if (chests[i] == 1)
            {
                for (int j = 0; j < Mathf.Min(2, game_params.game_settings.reward_for_chest); j++)
                {
                    GameObject g = Instantiate(gem, b.transform);
                    x_offset = (game_params.game_settings.reward_for_chest==1) ? 0.0f : 0.28f;
                    g.transform.localPosition = g.transform.localPosition + new Vector3((j % 2 == 0) ? x_offset : -x_offset, 0, 0);
                    g.SetActive(false);
                    gems.Add(g);
                    chestsLit.Add(b);
                }
            }
        }

        // reset timer
        roundStartTime = Time.time;
        timestamps = new float[6];
    }

    public void CheckBombs()
    {
        if (current_trial_type.bombs_lit != 0 && PlayEvent(explosion_avg_chance, trials_that_exploded_so_far))
        {
            //Debug.Log("Boom!");

            trials_that_exploded_so_far += 1;
            animatorCam.Play("CameraExplode", 0, 0f);
            int crystals_shift = current_trial_type.bombs_lit * game_params.game_settings.cost_for_bomb_explosion;
            crystals -= crystals_shift;
            ui.printCrystals(crystals);
            ui.printInfo(crystals_shift + " RUBIES LOST");
            /*
            foreach (GameObject bomb in bombsAll)
            {
                foreach(ParticleSystem ps in bomb.GetComponentsInChildren<ParticleSystem>())
                    ps.Play();
            }*/
            for (int i = 0; i < Mathf.Abs(crystals_shift); i++)
                Instantiate(RubyLost, rubiesUIParent);
            bombParent.GetComponent<AudioSource>().Play();
            lostRubiesSFX.Play();
        }
    }

    public void CheckChests()
    {
        //keyBoxAnim.SetTrigger("open");
        chestParent.GetComponent<AudioSource>().Play();
        foreach (GameObject chest in chestsLit)
        {
            chest.GetComponent<Animator>().SetTrigger("open");
            //chest.GetComponent<ParticleSystem>().Play();
        }

        if (PlayEvent(key_avg_chance, trials_that_rewarded_so_far))
        {
            //Debug.Log("Key!");
            trials_that_rewarded_so_far += 1;
            key.SetActive(true);
            int crystals_shift = current_trial_type.chests_lit * game_params.game_settings.reward_for_chest;
            crystals += crystals_shift;
            ui.printCrystals(crystals);
            ui.printInfo(crystals_shift + " RUBIES EARNED");
            foreach (GameObject g in gems)
                g.SetActive(true);
            for (int i=0; i< crystals_shift; i++)
                Instantiate(RubyEarned, rubiesUIParent);
            earnedRubiesSFX.Play();
        }
    }

    public void ClearAssets()
    {
        for (int i = 0; i < bombParent.childCount; i++)
            GameObject.Destroy(bombParent.GetChild(i).gameObject);
        for (int i = 0; i < chestParent.childCount; i++)
            GameObject.Destroy(chestParent.GetChild(i).gameObject);
        for (int i = 0; i < rubiesUIParent.childCount; i++)
            GameObject.Destroy(rubiesUIParent.GetChild(i).gameObject);
    }

    public bool PlayEvent(float event_avg_chance, float events_that_played_so_far)
    {        
        float a = Mathf.Abs((events_that_played_so_far + 1) / ((float)trials_where_player_went_forward) - event_avg_chance);
        float b = Mathf.Abs((events_that_played_so_far) / ((float)trials_where_player_went_forward) - event_avg_chance);
        float t = ((float)current_trial) / ((float)trials_count - 1);
        float epsilon = Mathf.Lerp(game_params.game_settings.allowed_error_from_target_ratio[0], game_params.game_settings.allowed_error_from_target_ratio[1], t);
        //Debug.Log("x: " + a.ToString() + " y: " + b.ToString() + " t: " + t + " epsilon: " + epsilon.ToString());

        if (Mathf.Abs(a - b) > epsilon)
            return Random.value > 0.5f;
        else
            return a < b;
    }

    private void FillTrialsPool()
    {
        trials_pool = new List<TrialType>();
        List<TrialType>[] groups = new List<TrialType>[game_params.game_settings.number_of_groups];
        for (int i = 0; i < groups.Length; i++)
            groups[i] = new List<TrialType>();

        for (int i = 0; i < game_params.game_settings.number_of_trials_per_trial_type / groups.Length; i++)
        {
            for (int j = 0; j < groups.Length; j++)
            {
                groups[j].AddRange(game_params.trial_types);
            }
        }

        int rest = game_params.game_settings.number_of_trials_per_trial_type % groups.Length;
        List<TrialType> pool_rest = new List<TrialType>();
        for (int i = 1; i < rest + 1; i++)
            pool_rest.AddRange(game_params.trial_types);
        pool_rest = Shuffle(pool_rest);

        // add pool_rest to the next group
        for (int i = 0; i < pool_rest.Count; i++)
            groups[i % groups.Length].Add(pool_rest[i]);

        // shuffle each group and add to the pool
        for (int i = 0; i < groups.Length; i++)
            trials_pool.AddRange(Shuffle(groups[i]));
    }

    private int[] FillRandomly(int size, int fill)
    {
        // 5, 5
        fill = Mathf.Min(size, fill);
        int[] list = new int[size];

        while (list.Sum() < fill)
            list[Random.Range(0, size)] = 1;
        return list;
    }

    private List<TrialType> Shuffle(List<TrialType> list)
    {
        TrialType tmp;
        int swapWith;
        for (int j = 0; j < 5; j++)
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

    public void SaveTimestamp(int i)
    {
        timestamps[i] = Time.time - roundStartTime;
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
                ui.printEndScreen("Connection error");
                StartCoroutine(SendTimestamps(trial_index, log));
            }
        }
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

    IEnumerator GetJSONFromURL()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(jsonUrl))
        {
            // Request and wait for the desired page.
            yield return www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + www.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + www.error);
                    break;
                case UnityWebRequest.Result.Success:
                    json_str = www.downloadHandler.text;
                    break;
            }
        }
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

public class GameParams
{
    public List<TrialType> trial_types;
    public GameSettings game_settings;
}
