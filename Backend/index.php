<?php

$file = 'settings.json';
$json_str = file_get_contents($file);
$error = '';

/* save game json */
if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $json_str = preg_replace('/[^A-Za-z0-9\-\_\{\}\[\]\,\"\:\.\ \t\r\n]/', '', $_POST["jconf"]);
	//echo $json_str;
	

    $json = json_decode($json_str);
    if(json_last_error() === JSON_ERROR_NONE){

	
		$fp = fopen($file, 'w');  
		if(!$fp) {
			throw new Exception('File open failed.');
		}
			
		if(flock($fp, LOCK_EX)){
			fwrite($fp, $json_str);
			flock($fp, LOCK_UN);
		}
		fclose($fp);
	}
	else{
		$error = '&nbsp; UNSAVED. JSON parsing error';
	}
}
    
?>

<!DOCTYPE html>
<html lang="en">
<head>
  <title>NeuroPace</title>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <meta name="description" content="" />
</head>
<body>
  <h1>NeuroPace JSON Config:</h1>
  <form action="index.php" method="POST">
  <p><label for="jconf">JSON:</label></p>
  <textarea id="jconf" name="jconf" rows="20" cols="80"><?php echo $json_str; ?></textarea>
  <br>
  <input type="submit" value="Save"><?php echo $error; ?>
</form>
</body>
</html>



