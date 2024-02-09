using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;
using Random = UnityEngine.Random;

public class WebRTCServer : MonoBehaviour
{
    private HttpListener HttpListener { get; set; }

    private UInt16 nextClientId = 0;

    public Dictionary<UInt16, RtcClient> clients = new Dictionary<UInt16, RtcClient>();

    public ServerPlayerManager ServerPlayerManager;

    private void Awake()
    {
        HttpListener = new HttpListener();
        HttpListener.Prefixes.Add("http://127.0.0.1:25565/");

        StartHttpListener();
    }

    private async void StartHttpListener()
    {
        HttpListener.Start();

        while (true)
        {
            HttpListenerContext ctx = await HttpListener.GetContextAsync();

            Debug.Log("Context");
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;
            Debug.Log(req.Url.AbsolutePath);
            if (req.Url.AbsolutePath == "/get-offer")
            {
                StartCoroutine(HandleGetOffer(req, resp));
            }

            if (req.Url.AbsolutePath == "/send-answer-get-candidate")
            {
                if (req.HttpMethod == "POST")
                {
                    StartCoroutine(HandleSendAnswer(req, resp));
                }
                else
                {
                    resp.AddHeader("Access-Control-Allow-Origin", "*");
                    resp.AddHeader("Access-Control-Allow-Headers", "*");
                    resp.Close();
                }
            }
        }
    }

    private IEnumerator HandleGetOffer(HttpListenerRequest req, HttpListenerResponse resp)
    {
        Debug.Log("Handle Get Offer");
        ushort clientId = nextClientId;
        nextClientId++;

        RTCPeerConnection peerConnection = new RTCPeerConnection();

        peerConnection.OnIceConnectionChange += state =>
        {
            if (state == RTCIceConnectionState.Disconnected)
            {
                if (clients.ContainsKey(clientId))
                {
                    Debug.Log("Removed Client On Ice Connection Disconnected Close");
                    clients[clientId].ConnectionClosed();
                }
            }
        };

        RTCDataChannel unreliableDataChannel = peerConnection.CreateDataChannel("Unreliable", new RTCDataChannelInit()
        {
            ordered = false,
            maxRetransmits = 0
        });

        unreliableDataChannel.OnOpen += () =>
        {
            clients[clientId].UnreliableOpen = true;
            if (clients[clientId].ReliableOpen)
            {
                clients[clientId].ConnectionReady();
            }
        };

        unreliableDataChannel.OnClose += () =>
        {
            if (clients.ContainsKey(clientId))
            {
                clients[clientId].ConnectionClosed();
            }
        };

        unreliableDataChannel.OnMessage += bytes => { MessageReceived(clientId, clients[clientId], bytes); };

        RTCDataChannel reliableDataChannel = peerConnection.CreateDataChannel("Reliable", new RTCDataChannelInit()
        {
            ordered = true
        });

        reliableDataChannel.OnOpen += () =>
        {
            clients[clientId].ReliableOpen = true;
            if (clients[clientId].UnreliableOpen)
            {
                clients[clientId].ConnectionReady();
            }
        };

        reliableDataChannel.OnClose += () =>
        {
            if (clients.ContainsKey(clientId))
            {
                clients[clientId].ConnectionClosed();
            }
        };

        reliableDataChannel.OnMessage += bytes => { MessageReceived(clientId, clients[clientId], bytes); };


        RtcClient client = new RtcClient(peerConnection, unreliableDataChannel, reliableDataChannel, clientId);

        client.ConnectionReady += () =>
        {
            Debug.Log("Connection Ready");
            Color color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
            ServerPlayerManager.AddPlayer(clientId, color, client);

            RtcMessage message = new RtcMessage(MessageTags.InitPlayer);
            message.WriteColorRGB(color);
            client.SendReliableMessage(message);
        };

        client.ConnectionClosed += () =>
        {
            //CLIENT REMOVED ON CONNECTION CLOSED
            clients.Remove(client.ID);
            ServerPlayerManager.RemovePlayer(client.ID);
            //send remove player messages 
            //TODO
        };

        clients.Add(clientId, client);

        RTCOfferAnswerOptions options = new RTCOfferAnswerOptions();

        RTCSessionDescriptionAsyncOperation offerOp = peerConnection.CreateOffer(ref options);

        yield return offerOp;

        RTCSessionDescription desc = offerOp.Desc;

        yield return peerConnection.SetLocalDescription(ref desc);

        resp.Headers["content-type"] = "application/json";
        string sdp = offerOp.Desc.sdp;
        // Debug.Log("Sdp: " + sdp);
        // string newSdp = sdp.Replace("IN IP4 127.0.0.1", "IN IP4 98.50.67.155");
        // Debug.Log("Sdp: " + newSdp);
        GetOfferResponse respObject = new GetOfferResponse(clientId, sdp);
        string jsonResponse = JsonUtility.ToJson(respObject);
        byte[] respBytes = Encoding.UTF8.GetBytes(jsonResponse);

        Debug.Log("Sending Response");
        resp.AddHeader("Access-Control-Allow-Origin", "*");
        resp.Close(respBytes, false);

        peerConnection.OnIceCandidate += candidate =>
        {
            // 1. Do nothing if a candidate one is already set
            if (client.IceCandidate != null || candidate == null)
            {
                return;
            }

            // 2. Skip candidates with certain addresses.  If your server is public, you
            //    would want to skip private address, so you could add 192.168., etc.
            if (candidate.Address.StartsWith("10."))
            {
                return;
            }

            // 3. Skip candidates that aren't udp.  We only want unreliable, 
            //    unordered connections.
            if (candidate.Protocol != RTCIceProtocol.Udp)
            {
                return;
            }

            //TEST- do we need a host?
            if (candidate.Type != RTCIceCandidateType.Host)
            {
                return;
            }

            // 4. If the user is waiting for a response, send the ICE Candidate now
            if (client.IceCandidateResponse != null)
            {
                client.IceCandidateResponse.Close(Encoding.UTF8.GetBytes(JsonUtility.ToJson(candidate)), false);
                return;
            }

            // 5. Otherwise, save it for when they are ready for a response

            client.IceCandidate = candidate;
        };
    }

    private IEnumerator HandleSendAnswer(HttpListenerRequest req, HttpListenerResponse resp)
    {
        Debug.Log("Handle Send Answer");
        using (var reader = new StreamReader(req.InputStream,
                   req.ContentEncoding))
        {
            string text = reader.ReadToEnd();
            Debug.Log("TEXT: " + text);
            GetOfferResponse answer = JsonUtility.FromJson<GetOfferResponse>(text);
            RtcClient client = clients[answer.clientId];
            RTCSessionDescription answerDescription = new RTCSessionDescription()
            {
                sdp = answer.sdp,
                type = RTCSdpType.Answer
            };

            RTCSetSessionDescriptionAsyncOperation answerOp =
                client.PeerConnection.SetRemoteDescription(ref answerDescription);
            yield return answerOp;
            if (client.IceCandidate != null)
            {
                Debug.Log("Candidate: " + client.IceCandidate.Candidate);
                string candidate = client.IceCandidate.Candidate;
                //  string newCandidate = candidate.Replace("192.168.0.171", "98.50.67.155");

                Debug.Log(candidate);
                JObject jObject = JObject.FromObject(new
                {
                    address = "127.0.0.1",
                    candidate = candidate,
                    component = "rtp",
                    foundation = client.IceCandidate.Foundation,
                    port = client.IceCandidate.Port,
                    priority = client.IceCandidate.Priority,
                    protocol = "udp",
                    sdpMid = client.IceCandidate.SdpMid,
                    sdpMLineIndex = client.IceCandidate.SdpMLineIndex,
                    type = "host",
                    usernameFragment = client.IceCandidate.UserNameFragment
                });

                jObject["relatedAddress"] = null;
                jObject["relatedPort"] = null;
                jObject["tcpType"] = null;

                string jObjectString = jObject.ToString();
                resp.AddHeader("Access-Control-Allow-Origin", "*");
                resp.Close(Encoding.UTF8.GetBytes(jObjectString), false);
                yield break;
            }

            client.IceCandidateResponse = resp;
        }
    }


    public void MessageReceived(ushort clientId, RtcClient client, byte[] message)
    {
        Debug.Log("Message Received");
        string messageStr = new string(Encoding.ASCII.GetChars(message));
        RtcMessageReader reader = new RtcMessageReader(messageStr);
        char messageTag = reader.ReadTag();
        Debug.Log("Message tag: " + messageTag);
        switch (messageTag)
        {
            case MessageTags.INPUT_TAG:
                ServerPlayerManager.ReceiveInputs(clientId, client, reader);
                break;
        }
    }
}