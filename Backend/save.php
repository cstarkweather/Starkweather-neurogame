<?php

/* save game logs */

if ($_SERVER["REQUEST_METHOD"] == "POST") {
	$game_id = preg_replace('/[^A-Za-z0-9\-\_]/', '', $_POST["id"]);
	$timestamps = $string = preg_replace('/[^0-9\ ]/', '', $_POST["log"]);
	$trial = (int) $_POST["t"];
	
	$file = 'logs' . DIRECTORY_SEPARATOR . $game_id . '.txt';
	$lines[] = '';
	
	try{
		
		if(file_exists($file)){
			$lines = file($file, FILE_IGNORE_NEW_LINES);
		}
		
		while(count($lines) < $trial){
			$lines[] = '';
		}
		
		$lines[$trial] = $timestamps;
		$text = implode(PHP_EOL, $lines);
		
		$fp = fopen($file, 'w');
		if(!$fp) {
			throw new Exception('File open failed.');
		} 
			
		if(flock($fp, LOCK_EX)){
			ftruncate($fp, 0);
			fwrite($fp, $text);
			flock($fp, LOCK_UN);
			fflush($fp);
		} else {
			throw new Exception('Can\'t lock the log file.');
		}
		fclose($fp);
	}catch ( Exception $e ) {
		print('save log error');
	}
}
	
?>