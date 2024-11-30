<?php

$apiKey = 'your_api_key_here';

$req_headers = [];

if (isset($_SERVER['HTTP_X_WEASYL_API_KEY']) && $_SERVER['HTTP_X_WEASYL_API_KEY'] == $apiKey) {
	$req_headers[] = 'X-Weasyl-API-Key: ' . $apiKey;
} else {
	http_response_code(401);
	exit();
}

if (isset($_SERVER['CONTENT_TYPE'])) {
	$req_headers[] = 'Content-Type: ' . $_SERVER['CONTENT_TYPE'];
}

$headers = array();

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
	CURLOPT_HEADER         => false,
	CURLOPT_HTTPHEADER     => $req_headers,
	CURLOPT_CUSTOMREQUEST  => $_SERVER['REQUEST_METHOD'],
	CURLOPT_HEADERFUNCTION => function($curl, $header) use (&$headers) {
		$headers[] = trim($header);
		return strlen($header);
	}
];

if ($_SERVER['REQUEST_METHOD'] !== 'GET') {
	$curl_params[CURLOPT_POSTFIELDS] = file_get_contents('php://input');
}

$curl_session = curl_init();
curl_setopt_array($curl_session, $curl_params);
$response = curl_exec($curl_session);
curl_close($curl_session);

foreach ($headers as $h) {
	header($h);
}

echo $response;

?>