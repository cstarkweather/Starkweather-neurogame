<?php

/* save game logs */

if ($_SERVER["REQUEST_METHOD"] == "POST") {
	$game_id = htmlspecialchars($_POST["id"]);
	$timestamps = htmlspecialchars($_POST["log"]);
	$trial = (int) htmlspecialchars($_POST["t"]);
	
	$file = 'logs' . DIRECTORY_SEPARATOR . $game_id . '.txt';
	
	if(!file_exists($file)){
		$fp = fopen($file, 'a');
		fwrite($fp, "");
		fclose($fp);
	}
	
	$lines = file($file, FILE_IGNORE_NEW_LINES);
	while(count($lines) < $trial){
		$lines[] = "";
	}

	$lines[$trial] = $timestamps;
	$text = implode(PHP_EOL, $lines);
	$fp = fopen($file, 'w');
	fwrite($fp, $text);
	fclose($fp);
}
	
?>