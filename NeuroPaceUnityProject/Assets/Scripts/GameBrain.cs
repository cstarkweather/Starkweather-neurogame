using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;
using System.Web;
using UnityEngine.InputSystem;

public class GameBrain : MonoBehaviour
{
    [SerializeField]
    private GameObject questions;
    [SerializeField]
    private Tutorial tutorial;
    [SerializeField]
    private GameObject bombLit;
    [SerializeField]
    private GameObject bombUnlit;
    [SerializeField]
    private GameObject chestLit;
    [SerializeField]
    private GameObject chestUnlit;
    [SerializeField]
    private GameObject gem;
    [SerializeField]
    private GameObject RubyEarned;
    [SerializeField]
    private GameObject RubyLost;
    [SerializeField]
    private GameObject FadeBlack;

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
    private Animator animatorCam;

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
    public bool is_game_started = false;
    public bool is_game_finished = false;

    private int rubies = 20;
    public string user_id = "DefaultUseer";
    public string game_id;
    private float roundStartTime = 0;
    public float[] timestamps = new float[6];
    private List<TrialType> trials_pool = new List<TrialType>();

    enum Modes { Local, Web };
    private Modes mode = Modes.Local;

    TrialType current_trial_type;
    private int current_trial = 0;
    private int current_trial_id = 0;
    public float decisionTimer = 0f;
    private int decision = -1;
    private int outcome = 0;

    public int trials_count_all;
    private int num_each_trial;
    List<int> trials_to_explode;
    List<int> trials_to_reward;
    List<float> explode_avg_chance;
    List<float> reward_avg_chance;
    List<int> exploded;
    List<int> rewarded;
    List<int> counter;
    List<int> performed;
    List<int> exploded_global;
    List<int> rewarded_global;

    [HideInInspector]
    public GameParams game_params;
    System.Random random = new System.Random();

    void Start()
    {
        //Application.targetFrameRate = 15; // only for fps influence test

#if UNITY_WEBGL && !UNITY_EDITOR
           mode = Modes.Web;
#endif

        ui.printInfo("");
        FadeBlack.SetActive(true);
        animatorCam = GetComponent<Animator>();
        animatorCam.SetFloat("mul", 0);

        if (mode == Modes.Web)
        {
            System.Uri parsedUrl = new System.Uri(Application.absoluteURL);
            switch (parsedUrl.Host) {
                case "qcpcpslws001.ucsf.edu":
                case "neurogame.ucsf.edu":
                    jsonUrl = parsedUrl.Scheme + "://" + parsedUrl.Host + "/backend/settings.json";
                    saveUrl = parsedUrl.Scheme + "://" + parsedUrl.Host + "/backend/save.php";
                    break;
                default:
                    Debug.Log("Using default test backend on CAG servers");
                    break;
            }
            // avoid caching settings.json
            jsonUrl += "?version=" + random.Next().ToString();
            if (HttpUtility.ParseQueryString(parsedUrl.Query).Get("id") is not null)
                user_id = HttpUtility.ParseQueryString(parsedUrl.Query).Get("id");
            Debug.Log("Loading JSON from URL");
            StartCoroutine(GetJSONFromURL());
        }
        else if (mode == Modes.Local)
        {
            string game_settings_path = Directory.GetParent(Application.dataPath) + "/game_settings.json";
            if (File.Exists(game_settings_path)) {
                json_str = File.ReadAllText(game_settings_path);
            }
            else { ui.printEndScreen("No \"game_settings.json\" file in game directory."); }
            user_id = "Local";
        }

        // set unique game id
        System.DateTime date = System.DateTime.Now;
        game_id = user_id + date.ToString("_yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        ui.printInfo("");
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

        if (json_str == "")
            return;
        else if (game_params == null)
            ParamsFromJson();

        if (!is_game_started) {
            is_game_started = (questions.activeSelf == false && tutorial.tutorials_left == 0);
            if (!is_game_started) return;
            else InitializeGame();
        }

        AnimatorStateInfo animStateInf = animatorCam.GetCurrentAnimatorStateInfo(0);

        // timestamps (NOTE: timestamp[2] is writing on KeyDown)
        if (timestamps[0] == 0 && animStateInf.IsName("CameraEntryWalk"))
        {
            // enable fuses sounds
            foreach (GameObject bomb in bombsAll)
                foreach (AudioSource audio in bomb.GetComponentsInChildren<AudioSource>())
                    audio.mute = false;
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
            SaveLogLine(game_id, TrialLogLine(), current_trial+1);
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
                ui.printInfo("");
                ui.printEndScreen("Your final result is \r\n" + rubies + " RUBIES\r\nThanks for playing \r\n\r\n (debug: 'R' for replay)");
                ClearAssets();
                is_game_finished = true;
            }
            // debug stats
            //Debug.Log($"\n trial: {current_trial} / {trials_count_all}, skipped: {current_trial - performed.Sum()}, exploded: {exploded.Sum()}, rewarded: {rewarded.Sum()}");
        }

        // if time to player action
        if (animStateInf.IsName("CameraWaiting"))
        {
            decisionTimer -= Time.deltaTime;
            string timer = (game_params.game_settings.time_for_decision != 0 && decisionTimer < game_params.game_settings.show_timer_from) ? $" {Mathf.CeilToInt(decisionTimer)}" : "";
            ui.printDescription($"Waiting for action{timer}");
            if (decisionTimer < 0 && game_params.game_settings.time_for_decision != 0)
            {
                // times off - skip
                SaveTimestamp(2);
                animatorCam.Play("CameraTimeIsOff", 0, 0f);
                ui.printDescription("");
                actionSkip.Play();
                decision = 2;
                outcome = -game_params.game_settings.cost_for_no_decision;
                rubies += outcome;
                ui.rubiesTarget = rubies;

                string desc = (Mathf.Abs(outcome) == 1) ? " RUBY!" : " RUBIES!";
                ui.printInfo(outcome + desc);
                for (int i = 0; i < Mathf.Abs(outcome); i++)
                    Instantiate(RubyLost, rubiesUIParent);
                bombParent.GetComponent<AudioSource>().Play();
                lostRubiesSFX.Play();
            }

            if (Keyboard.current.upArrowKey.isPressed || (Gamepad.current != null && Gamepad.current.aButton.isPressed))
            {
                // try
                SaveTimestamp(2);
                animatorCam.SetTrigger("try");
                ui.printDescription("");
                actionWalk.Play();
                decision = 1;
                performed[current_trial_id] += 1;
                //ScreenCapture.CaptureScreenshot("SomeLevel2.png", 2);
            }
            else if (Keyboard.current.rightArrowKey.isPressed || (Gamepad.current != null && Gamepad.current.bButton.isPressed))
            {
                // escape
                SaveTimestamp(2);
                animatorCam.Play("CameraBlack", 0, 0f);
                ui.printDescription("");
                actionSkip.Play();
                decision = 0;
            }
        }
       
    }

    private void ParamsFromJson()
    {
        game_params = JsonConvert.DeserializeObject<GameParams>(json_str);
       
        num_each_trial = game_params.game_settings.number_of_trials_per_trial_type;
        trials_count_all = num_each_trial * game_params.trial_types.Count;
        trials_to_explode = game_params.game_settings.trials_to_explode;
        trials_to_reward = game_params.game_settings.trials_to_reward;

        exploded = new List<int>(new int[game_params.trial_types.Count]);
        rewarded = new List<int>(new int[game_params.trial_types.Count]);
        counter = new List<int>(new int[game_params.trial_types.Count]);
        performed = new List<int>(new int[game_params.trial_types.Count]);
        exploded_global = new List<int>(new int[trials_count_all]);
        rewarded_global = new List<int>(new int[trials_count_all]);
        explode_avg_chance = new List<float>(new float[game_params.trial_types.Count]);
        reward_avg_chance = new List<float>(new float[game_params.trial_types.Count]);
        for (int i = 0; i < game_params.trial_types.Count; i++)
        {
            explode_avg_chance[i] = (float)trials_to_explode[i] / num_each_trial;
            reward_avg_chance[i] = (float)trials_to_reward[i] / (num_each_trial - trials_to_explode[i]);
        }        
    }

    private void InitializeGame()
    {
        /* DEBUG: TRIALS SIMULATIONS: */
        //Simulation(10, 0.0f);
        /* END DEBUG */

        FillTrialsPool();
        ui.rubiesTarget = rubies;
        // start 1st round
        current_trial = -1;
        // start camera
        animatorCam.SetFloat("mul", 1);
        NewRound();
    }

    private void NewRound()
    {
        // update round index
        decisionTimer = (float)game_params.game_settings.time_for_decision;
        decision = -1;
        outcome = 0;
        current_trial++;
        current_trial_type = trials_pool[current_trial];
        current_trial_id = trials_pool[current_trial].id - 1;
        counter[current_trial_id] += 1;
        ui.printRounds(current_trial + 1);

        // clear activities, messages and assets
        ui.printInfo("");
        ui.printDescription("");
        ui.printEndScreen("");
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
        if (PlayEvent(num_each_trial, counter[current_trial_id], performed[current_trial_id], exploded[current_trial_id], trials_to_explode[current_trial_id], explode_avg_chance[current_trial_id]) && current_trial_type.bombs_lit != 0)
        {
            //Debug.Log("Boom!");
            exploded_global[current_trial] = 1;
            exploded[current_trial_id] += 1;

            animatorCam.Play("CameraExplode", 0, 0f);
            outcome = -current_trial_type.bombs_lit * game_params.game_settings.cost_for_bomb_explosion;
            rubies += outcome;
            ui.rubiesTarget = rubies;
            string desc = (Mathf.Abs(outcome) == 1) ? " RUBY!" : " RUBIES!";
            ui.printInfo(outcome + desc);

            for (int i = 0; i < Mathf.Abs(outcome); i++)
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
        }

        if (PlayEvent(num_each_trial, counter[current_trial_id], performed[current_trial_id], rewarded[current_trial_id], trials_to_reward[current_trial_id], reward_avg_chance[current_trial_id]))
        {
            //Debug.Log("Rubies!");
            rewarded_global[current_trial] = 1;
            rewarded[current_trial_id] += 1;

            outcome = current_trial_type.chests_lit * game_params.game_settings.reward_for_chest;
            rubies += outcome;
            ui.rubiesTarget = rubies;
            string desc = (Mathf.Abs(outcome) == 1) ? " RUBY!" : " RUBIES!";
            ui.printInfo("+" + outcome + desc);
            foreach (GameObject g in gems)
                g.SetActive(true);
            for (int i=0; i< outcome; i++)
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

    public bool PlayEvent(float trials, int trial, int trials_performed, int events_performed, int events_expected, float event_avg_chance)
    {
        float designed_events_count = ((float)(trials_performed) / trials) * events_expected;
        if (events_performed > designed_events_count)
            return ((float)random.NextDouble() < game_params.game_settings.minimum_error)
        else
			if (events_performed < designed_events_count)
            return ((float)random.NextDouble() < 1 - game_params.game_settings.minimum_error);
			else
				return ((float)random.NextDouble() < event_avg_chance);
		

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

    private string TrialLogLine()
    {
        string log_line = System.String.Format("{0} {1} {2} {3}", current_trial, current_trial_type.id, decision, outcome);
        foreach (float t in timestamps)
        {
            int millis = Mathf.RoundToInt(t * 1000);
            log_line += " " + millis.ToString();
        }
        return log_line;
    }

    public void SaveLogLine(string game_id, string line, int row_index)
    {
        if (mode == Modes.Local)
            SaveLogLineLocal(game_id, line);
        else if (mode == Modes.Web)
            StartCoroutine(SaveLogLineWeb(game_id, line, row_index));
    }

    public static async void SaveLogLineLocal(string game_id, string line)
    {
        string logs_path = Directory.GetParent(Application.dataPath) + "/logs_" + game_id + ".txt";
        using StreamWriter file = new(logs_path, append: true);
        await file.WriteLineAsync(line);
    }

    private IEnumerator SaveLogLineWeb(string game_id, string log, int row_index)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", game_id);
        form.AddField("log", log);
        form.AddField("t", row_index);

        using (UnityWebRequest www = UnityWebRequest.Post(saveUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                ui.printEndScreen("Connection error");
                StartCoroutine(SaveLogLineWeb(game_id, log, row_index));
            }
        }
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


    private void Simulation(int sim_num, float chance_to_skip)
    {
        FillTrialsPool();
        string output = "to_explode: ";
        for (int i = 0; i < game_params.trial_types.Count; i++)
            output += trials_to_explode[i].ToString() + ", ";
        output += "\nto_reward: ";
        for (int i = 0; i < game_params.trial_types.Count; i++)
            output += trials_to_reward[i].ToString() + ", ";
        output += "\nexplode_avg_chance: ";
        for (int i = 0; i < game_params.trial_types.Count; i++)
            output += explode_avg_chance[i].ToString() + ", ";
        output += "\nreward_avg_chance: ";
        for (int i = 0; i < game_params.trial_types.Count; i++)
            output += reward_avg_chance[i].ToString() + ", ";

        output += $"\n\nall: {game_params.trial_types.Count * num_each_trial}, to explode: {trials_to_explode}, to reward: {trials_to_reward}";
        for (int i = 0; i < sim_num; i++)
        {
            ParamsFromJson();
            int skip = 0;
            for (int t = 0; t < game_params.trial_types.Count * num_each_trial; t++)
            {
                int current_trial_id = trials_pool[t].id - 1;
                counter[current_trial_id] += 1;
                if ((float)random.NextDouble() < chance_to_skip)
                {
                    skip++;
                    continue;
                }
                

                if (PlayEvent(num_each_trial, counter[current_trial_id], performed[current_trial_id], exploded[current_trial_id], trials_to_explode[current_trial_id], explode_avg_chance[current_trial_id]) && explode_avg_chance[current_trial_id] != 0)
                    exploded[current_trial_id] += 1;
                else if (PlayEvent(num_each_trial, counter[current_trial_id], performed[current_trial_id], rewarded[current_trial_id], trials_to_reward[current_trial_id], reward_avg_chance[current_trial_id]))
                    rewarded[current_trial_id] += 1;

		performed[current_trial_id] += 1;

            }
            output += $"\nSIMULATION {i}: trials: {trials_count_all}, skipped: {trials_count_all - performed.Sum()}, exploded: {exploded.Sum()}, rewarded: {rewarded.Sum()}";
        }
        Debug.Log(output);
        ParamsFromJson(); //restart params
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
    public List<int> trials_to_explode;
    public List<int> trials_to_reward;
    public float minimum_error;
    public int time_for_decision;
    public int show_timer_from;
    public int cost_for_no_decision;
    public string goal_description;
    public string reward_description;
}

public class GameParams
{
    public List<TrialType> trial_types;
    public GameSettings game_settings;
}
