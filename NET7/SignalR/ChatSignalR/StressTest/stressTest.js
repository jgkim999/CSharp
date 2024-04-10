import ws from 'k6/ws';
import { check, sleep } from 'k6';
import http from "k6/http";
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export const options = {
  stages: [
    { duration: '1m', target: 1 },
    { duration: '1m', target: 1 },
    { duration: '1m', target: 1 },
  ]
};

let httpUrl = "http://localhost:5003/chatHub";
let wsUrl = "ws://localhost:5003/chatHub";

export default function () {
  console.log('Test Start');

  const payload = JSON.stringify({
    name: 'test' + randomIntBetween(1, 2100000000)
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  // Get the connection id to use in the web socket connect
	var negotiateRes = http.post(`${httpUrl}/negotiate`, null, {
    "responseType": "text"
  });
  var connectionId = negotiateRes.json()["connectionId"];

  console.log('ws connect');
  const res = ws.connect(`${wsUrl}?id=${connectionId}`,
    {
      headers: {
        //"Cookie": `${authCookieName}=${authCookie}`,
        'Content-Type': 'application/json',
        "Origin": httpUrl,
      }
    },
    function (socket) {
    console.log('ws Start');

    socket.on('open', () => {
      console.log('open');
      socket.send('{"protocol":"json","version":1}\x1e') // add this
      console.log('sent protocol request');
      socket.send('{"type":1, "target":"SendMessage", "arguments":["Wade","Hi"]}\x1e') // add this
    });

    socket.on('message', function(message) {
      const msg = JSON.parse(message);
      console.log(msg);

      switch (message) {
        case '{}\x1e':
          // This is the protocol confirmation
          break;
        // {"type":1,"target":"ReceiveMessage","arguments":["Wade","Hi"]}
        case '{"type":6}\x1e':
          // Received handshake
          console.info('Received. ping');
          break;
        default:
          // should check that the JSON contains type === 1
          console.log(`Received message: ${message}`);
      }
    });
    
    socket.on('ping', function () {
      console.log('PING!');
    });

    socket.on('pong', function () {
      console.log('PONG!');
    });
    
    socket.on('close', function () {
      console.log(`VU ${__VU}: disconnected`);
    });

    socket.on('error', function (e) {
      if (e.error() != "websocket: close sent") {
        console.log('An unexpected error occurred: ', e.error());
      }
    });
    
    socket.setTimeout(function () {
      console.log('60 seconds passed, closing the socket');
      socket.close();
    }, 60000);
  });
  check(res, { 'Connected successfully': (r) => r && r.status === 101 });
  sleep(10);
}
