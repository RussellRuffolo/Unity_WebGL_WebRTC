mergeInto(LibraryManager.library, {   


Connect__deps: ['SendAnswer', 'AddIceCandidate'],
Connect: function (baseUrl) {

  var url = UTF8ToString(baseUrl)

  try{
    fetch(url)
    .then(function(response) {
    console.log(response)
      return response.json()
    })
    .then(function(offer){
      return _SendAnswer(offer)
    })
  }
  catch(e){
  }
    
},

  AddIceCandidate: function (candidate) {
    // 1. This checks for the server indicating it could not provide any
    //    ICE Candidates.
    if (candidate.candidate === '') {
      return console.error('the server had no ICE Candidates')
    }
  
    // 2. Pass the ICE Candidate to the Client PeerConnection
    peerConnection.addIceCandidate(candidate)
  },

  SendAnswer: function (offer) {

  const SendAnswerUrl = "http://127.0.0.1:25565/send-answer-get-candidate"

  // 1. Create the client side PeerConnection
  peerConnection = new RTCPeerConnection({
    iceServers: [
      {
        urls: "stun:stun2.l.google.com:19302"
      }
    ]
  });
  const clientId = offer.clientId

  SendMessage('WebRTCClient', 'SetClientId', offer.clientId)

  // 2. Set the offer on the PeerConnection
  peerConnection.setRemoteDescription(
    { type: 'offer', sdp: offer.sdp }
  ).then(function() {
    // 3. Create an answer to send to the Server
    peerConnection.createAnswer().then(function(answer) {
      // 4. Set the answer on the PeerConnection
      peerConnection.setLocalDescription(answer).then(function() {

        var body = JSON.stringify({clientId: clientId, sdp: answer.sdp})

        // 5. Send the answer to the server
        fetch(SendAnswerUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: body
        })
          .then(function(response) {
            return response.json()
          })
          .then(_AddIceCandidate)
      })
    })
  })

  //set up data channel
  peerConnection.ondatachannel = function (event) {

      const dataChannel = event.channel

      if(dataChannel.label === 'Reliable'){
        peerConnection.reliableChannel = dataChannel

	SendMessage('WebRTCClient', 'ReliableChannelOpen');


        dataChannel.onmessage = function(event) {            
	  console.log(typeof event.data)
	  console.log(event.data)
          SendMessage('WebRTCClient', 'ReceiveReliableMessage', event.data)
        }
      }
      else if(dataChannel.label === 'Unreliable'){
        peerConnection.unreliableChannel = dataChannel

        dataChannel.onmessage = function(event) {
          SendMessage('WebRTCClient', 'ReceiveUnreliableMessage', event.data)
        }
      }
   
    }
},




SendReliableMessage: function(message){
  peerConnection.reliableChannel.send(UTF8ToString(message))
},


SendUnreliableMessage: function(message){
  peerConnection.unreliableChannel.send(UTF8ToString(message))
},




GetToken: function() 
{ 
  var queryString = window.location.search;
  console.log(queryString);
  
  var urlParams = new URLSearchParams(queryString);
  
  var state = urlParams.get('state')
  
  
  var code = urlParams.get('code')
  console.log(state);
  console.log(code)

let callbackUrl;
if(document.referrer == "https://discord.com/"){
    callbackUrl = new URL('https://id.crashblox.net/auth/discord/callback');
}
else if(document.referrer == "https://accounts.google.com/"){
    callbackUrl = new URL('https://id.crashblox.net/auth/google/callback');
}
else{
console.log("Unknown referrer: " + document.referrer);
}


  var callbackParamData = {
    code : code,
    state : state
  };
  for(var k in callbackParamData){
    callbackUrl.searchParams.append(k, callbackParamData[k]);
  }
  
  fetch(callbackUrl)
    .then(function(response)
    {return response.json()} )
    .then(function(data) { console.log(data);
       

    SendMessage('Network', 'ReceiveToken', data.access_token)   

    })

  
  
  
},

PatchUsername: function(username){
  var newName = UTF8ToString(username)
  console.log(newName);
  var userBody = {
    "username": newName
    };

    console.log(JSON.stringify(userBody));
    console.log("At PatchUsername token is: " + window.localStorage.token)

  fetch("https://id.crashblox.net/users/me",
    {
      method: "PATCH",
      body: JSON.stringify(userBody),
      headers: { "Authorization": "Bearer " + window.localStorage.token,
      "Content-Type": "application/json",
      "accept": "application/json"}
    })
  .then(function(response) {
      return response.json()
    })
   .then(function(data) {
     console.log(data);
     SendMessage('Network', 'ReceiveUsername', data.username);

    })

  
},





});