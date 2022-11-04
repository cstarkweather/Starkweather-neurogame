<?php

function clamp($current, $min, $max) {
    return max($min, min($max, $current));
}

/* read json file */
$file_json_path = "settings.json";
$file_json = fopen($file_json_path, "r") or die("Unable to open setting file!");
$settings_json = fread($file_json, filesize($file_json_path));
$settings = json_decode($settings_json, false);
fclose($file_json);

if ($_SERVER["REQUEST_METHOD"] == "POST") {
	if(!isset($_POST["revert"])) {
		$settings->rounds = max(1, (int) $_POST["rounds"]);
		$settings->explosions = clamp((int) $_POST["explosions"], 0, $settings->rounds);
		$settings->keys = clamp((int) $_POST["keys"], 0, $settings->rounds);;
		$settings->allbombs = clamp((int) $_POST["allbombs"], 0, 100);;
		$settings->litbombs = clamp((int) $_POST["litbombs"], 0, $settings->allbombs);;
		$settings->allchests = clamp((int) $_POST["allchests"], 0, 100);;
		$settings->litchests = clamp((int) $_POST["litchests"], 0, $settings->allchests);;
		$settings->punishment = max(0, (int) $_POST["punishment"]);
		$settings->revenue = max(0, (int) $_POST["revenue"]);
		$settings_json = json_encode($settings);
		file_put_contents($file_json_path, $settings_json);
	}
}

?>
 
<form method="post" action="index.php">
 
	<label for="rounds">Number of rounds:</label><br>
	<input type="number" id="rounds" name="rounds" min="1" value="<?php print($settings->rounds); ?>"><br>
	  
	<label for="explosions">Number of explosions:</label><br>
	<input type="number" id="explosions" name="explosions" min="0" value="<?php print($settings->explosions); ?>"><br>
	  
	<label for="keys">Number of keys:</label><br>
	<input type="number" id="keys" name="keys" min="0" value="<?php print($settings->keys); ?>"><br><br>
	  
	  
	<label for="allbombs">Number of bombs:</label><br>
	<input type="number" id="allbombs" name="allbombs" min="0" value="<?php print($settings->allbombs); ?>"><br>

	<label for="litbombs">Number of lit bombs:</label><br>
	<input type="number" id="litbombs" name="litbombs" min="0" value="<?php print($settings->litbombs); ?>"><br>
	  
	<label for="allchests">Number of chests:</label><br>
	<input type="number" id="allchests" name="allchests" min="0" value="<?php print($settings->allchests); ?>"><br>
	  
	<label for="litchests">Number of lit chests:</label><br>
	<input type="number" id="litchests" name="litchests" min="0" value="<?php print($settings->litchests); ?>"><br><br>


	<label for="punishment">Each bomb takes (crystals):</label><br>
	<input type="number" id="punishment" name="punishment" min="0" value="<?php print($settings->punishment); ?>"><br>
	  
	<label for="revenue">Each chest gives (crystals):</label><br>
	<input type="number" id="revenue" name="revenue" min="0" value="<?php print($settings->revenue); ?>"><br><br>
	  
	<input type="submit" value="Submit"> <input type="submit" name="revert" value="Revert">
  
</form> 