<?php

$apiKey = 'your_api_key_here';

$req_headers = [];

if (isset($_SERVER['HTTP_X_WEASYL_API_KEY']) && $_SERVER['HTTP_X_WEASYL_API_KEY'] == $apiKey) {
	$req_headers[] = 'X-Weasyl-API-Key: ' . $apiKey;
} else {
	http_response_code(401);
	exit();
}

$boundary = uniqid();
$delimiter = '-------------' . $boundary;

$req_headers[] = 'Content-Type: multipart/form-data; boundary=' . $delimiter;

$data = "";

foreach (array('title', 'subtype', 'folderid', 'rating', 'content', 'tags') as $name) {
	$data .= "--$delimiter\r\n";
	$data .= "Content-Disposition: form-data; name=\"$name\"\r\n";
	$data .= "\r\n";
	$data .= $_POST[$name] . "\r\n";
}

$data .= "--$delimiter\r\n";
$data .= "Content-Disposition: form-data; name=\"submitfile\"; filename=\"picture.dat\"\r\n";
$data .= "Content-Transfer-Encoding: binary\r\n";
$data .= "\r\n";
$data .= file_get_contents($_POST['submitfile']) . "\r\n";

$data .= "--$delimiter\r\n";
$data .= "Content-Disposition: form-data; name=\"thumbfile\"; filename=\"thumb.dat\"\r\n";
$data .= "Content-Transfer-Encoding: binary\r\n";
$data .= "\r\n";
$data .= "\r\n";

$data .= "--$delimiter--\r\n";

$locationHeader = '';

$curl_params = [
	CURLOPT_URL => (
		'https://www.weasyl.com/submit/visual'
  	),
	CURLOPT_TIMEOUT        => 30,
	CURLOPT_RETURNTRANSFER => true,
	CURLOPT_FOLLOWLOCATION => false,
	CURLOPT_SSL_VERIFYHOST => 2,
	CURLOPT_SSL_VERIFYPEER => true,
	CURLOPT_HEADER         => false,
	CURLOPT_HTTPHEADER     => $req_headers,
	CURLOPT_CUSTOMREQUEST  => 'POST',
	CURLOPT_POSTFIELDS     => $data,
	CURLOPT_HEADERFUNCTION => function($curl, $header) use (&$locationHeader) {
		if (str_starts_with(strtolower($header), 'location: ')) {
			$locationHeader = trim($header);
		}
		return strlen($header);
	}
];

$curl_session = curl_init();
curl_setopt_array($curl_session, $curl_params);
$response = curl_exec($curl_session);
curl_close($curl_session);

if ($locationHeader != '') {
	header('Content-Type: text/uri-list');
	echo substr($locationHeader, 10);
} else {
	http_response_code(502);
}

?>