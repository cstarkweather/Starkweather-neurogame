# Starkweather-neurogame

Unity code for first-person videogame used in the manuscript "An orbitofrontal microcircuit for approach-avoidance decision-making". This readme will go through 1) how to toggle game settings relevant to neuroscientists (such as reward/punishment probabilities), 2) how data is recorded (reaction times, trial types, outcomes...), and 3) how to compile the Unity code into a playable .exe file on your computer.

## How to easily toggle game settings relevant to neuroscientists

Navigate to Starkweather-neurogame-->NeuroPaceUnityProject and you will see a game_settings.json file. This is where you can toggle experimental settings. Here you can specify trial types with the magnitude of bombs lit and treasures lit.

Variable definitions and explanations of default settings:

1) number_of_groups: this is the number of groups to split the total number of trials into. For example, if you have 20 trials per trial type and 10 total trials, you have a total of 200 trials. Setting number_of_groups=4 splits these 200 trials into 4 groups such that all trial types would be evenly distributed every 200/4 = 50 trials. This avoids clumping of single trial types over the course of gameplay.
2) number_of_trials_per_trial_type: number of trials per trial type. Your total number of trials is number_of_trials_per_trial_type*length(trial_types)
3) trials_to_explode: vector of values of length(trial_type) that specifies, over the course of gameplay, how many times the lit bombs will explode. In the version of the game played by patients included in the manuscript, I informed patients prior to gameplay that lit bombs explode 40% of the time the player passes them. Therefore, I specify that there are 8 trials_to_explode (8/20 total trials per trial type = 40%) in each trial type with lit bombs.
4) trials_to_reward: vector of values of length(trial_type) that specifies, over the course of gameplay, how many times the lit treasure chests will reveal rubies. In the version of the game played by patients included in the manuscript, I informed patients prior to gameplay that, IF they pass the bombs (without explosion) and reach the treasures, that the treasure chests will reveal rubies 70% of the time. Therefore, I specify that there are 14 trials_to_reward (14/20 total trials per trial type = 70%) in trial types with no lit bombs, and between 8 and 9 trials_to_reward ((8-9)/12 unexploded trials ~70%) in trial types with lit bombs. The denominator is 12 in trials with lit bombs because only on 12/20 trials (60%) will not bombs NOT explode, allowing the player to reach the treasures.
5) minimum_error: this parameter specifies the extent to which the proportion of outcomes is allowed to deviate (stochastically) from the predetermined proportion of outcomes specified trials_to_reward and trials_to_explode. If it is set to one extreme (1), then the game algorithm determining the outcome on a trial in which the patient chooses to approach will simply be a weighted coinflip: for instance, on a trial type in which the bombs are NOT lit, the outcome would be reward in 70% of cases if trials_to_reward(trial_type)/number_of_trials_per_trial_type(trial_type) = 70%. However, this risks that the patient could potentially stochastically experience multiple unrewarded trials in a row, or multiple rewarded trials in a row, for a particular trial type. To avoid this, we could set minimum_error to another extreme (0) where the algorithm tracks the history of outcomes and attempts to steer the outcome probability as close to 70% as possible. So, if minimum_error = 0, and the history of outcomes for a 70% rewarded trialtype is includes 6 rewarded trials and 3 reward omissions, then the outcome on the 10th trial of that particular trialtype MUST be reward.
6) cost_for_bomb_explosion: number of rubies subtracted per LIT bomb. For example, if 3 bombs are lit and all 3 explode, then 3 rubies are subtracted.
7) cost_for_no_decision: number of rubies subtracted if the patient does not make a decision
8) reward_for_chest: number of rubies earned per ruby that appears in each treasure chest.
9) time_for_decision: number of seconds given to make a decision before cost_for_no_decision is subtracted and a ruby explodes in front of the patient
10) show_timer_from: start counting down the time a patient has left to make a decision from show_timer_from onwards
11) goal_description: at UCSF we utilized monetary rewards, so customized this sentence to motivate patients to perform well in the task. The "top half" score was not revealed to patients.

## How behavioral data is recorded

Data is recorded in a timestamped .txt logfile in the **same location** that you choose to store the Neuropace.exe file (this is the file that contains the playable game). This is what a typical logfile looks like (this is Subject 5 from the manuscript):

0 6 0 0 5 6
0 3 1 2 1001 1667 6176 7683 9184 11185
1 3 1 -4 870 1535 5273 6780 0 8790
2 7 1 0 1107 1778 2672 4179 5680 7682
3 1 0 0 871 1536 2914 0 0 2920
4 4 1 4 1177 1848 2048 3555 5056 7058
5 4 1 0 1248 1913 2431 3938 5439 7440
...

The first line contains the mood questionnaire ratings from the survey at the beginning of the game. The columns in the second line onwards indicates the following:
1) trial number (starts at 0)
2) trial_type (specified from your game_settings.json file, see above)
3) decision (0 = avoid, 1 = approach)
4) outcome (positive magnitude indicates reward magnitude won; negative magnitude indicate punishment magnitude)
5) timestamp of trial start relative to black screen that precedes each trial
6) timestamp of "Waiting for action" command at bottom of screen that marks eligible to make decision with button press
7) timestamp of patient's button press indicating either approach or avoid
8) timestamp of when the bombs were passed. if the bombs exploded, this is the timestamp of punishment. if the trial was avoided, this value is recorded as "0".
9) timestamp of when the treasure chests were reached. if the treasure chests opened up, this is the timestamp of reward. if the trial was avoided, this value is recorded as "0".
10) timestamp of when the black screen (intertrial interval) started
**please note that the timestamps used for neural data analysis in the manuscript were confirmed and corrected with greater millisecond precision using the Blackbox toolkit, as the computer clocktime for recording keypresses and screen changes is not perfect. Blackbox toolkit inputs involved a separate buttonbox for indicating button presses, a photodiode for trial onset times, and a microphone for the timing of outcomes**. In general, we found that the logfile records timepoints within tens of milliseconds of precision.

## How to compile Unity code into a playable .exe file on your computer

1. Install [Unity Hub](https://unity.com/download). FYI: I have tested opening and compiling the code with Unity2021.3.16.
2. Clone or download this repository
3. Open Unity Hub, click **“Add project”**, and select: Starkweather-neurogame/NeuroPaceUnityProject. You **must** selected the inner "NeuroPaceUnityProject" folder. **Do not** select the outer Starkweather-neurogame folder or any other outer folder.
4. Once the project is open in Unity, do the following. In the **Project** pane, navigate to: Assets/Scenes/SampleScene.unity. Then double-click `SampleScene.unity` to open it. You should see a long hallway with the game objects (bombs, treasures) inside of a large room.
5. Click `File ▸ Build Settings...`. Select **Platform** = `PC, Mac & Linux`, **Target** = `Windows`. Click **“Add Open Scenes”**. Then Click **“Build”** and choose an output folder.
6. The compiled Neuropace.exe file should then appear in your chosen output folder. This .exe file is compiled with the game contingencies you set inside the game_settings json file. It can be dropped onto just about any computer (I have run it on Lenovo Thinkstation, macbooks, Lenovo laptops...) to be opened and played. Once you start playing, it will record the results in a logfile in the **same folder** in which you save the .exe file.
