<?php

// based on: https://imrannazar.com/articles/proxying-rest-in-php

$apiKey = 'your_api_key_here';

$req_headers = [];

if (isset($_SERVER['CONTENT_TYPE'])) {
	$req_headers[] = 'Content-Type: ' . $_SERVER['CONTENT_TYPE'];
}

if (isset($_SERVER['HTTP_X_WEASYL_API_KEY']) && $_SERVER['HTTP_X_WEASYL_API_KEY'] == $apiKey) {
	$req_headers[] = 'X-Weasyl-API-Key: ' . $apiKey;
} else {
	http_response_code(401);
	exit();
}

$params = $_GET;
unset($params['path']);
$curl_params = [
	CURLOPT_URL => (
		'https://www.weasyl.com/' . $_GET['path'] . '?' . http_build_query($params)
  	),
	CURLOPT_TIMEOUT        => 30,
	CURLOPT_RETURNTRANSFER => true,
	CURLOPT_FOLLOWLOCATION => true,
	CURLOPT_SSL_VERIFYHOST => 2,
	CURLOPT_SSL_VERIFYPEER => true,
	CURLOPT_HEADER         => true,
	CURLOPT_HTTPHEADER     => $req_headers,
	CURLOPT_CUSTOMREQUEST  => $_SERVER['REQUEST_METHOD'],
];

if ($_SERVER['REQUEST_METHOD'] !== 'GET') {
  $curl_params[CURLOPT_POSTFIELDS] = file_get_contents('php://input');
}

$curl_session = curl_init();
curl_setopt_array($curl_session, $curl_params);
$response = curl_exec($curl_session);
curl_close($curl_session);

$split_point = strpos($response, "\r\n\r\n");
$headers = trim(substr($response, 0, $split_point));
$body = trim(substr($response, $split_point));

foreach (explode("\r\n", $headers) as $h) {
  header($h);
}

echo $body;

?>