using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebRTCClient : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string Connect(string baseUrl);

    [DllImport("__Internal")]
    private static extern void SendReliableMessage(string message);

    [DllImport("__Internal")]
    private static extern void SendUnreliableMessage(string message);

    public Dictionary<ushort, NetworkPlayerController> NetworkPlayers =
        new Dictionary<ushort, NetworkPlayerController>();

    private const string baseUrl = "http://127.0.0.1:25565";

    public ushort ClientId;

    public PlayerController Player;

    public GameObject playerPrefab;

    public GameObject networkPlayerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Connect(baseUrl + "/get-offer");
    }

    public void SetClientId(int clientId)
    {
        ClientId = (ushort)clientId;
    }

    public void ReceiveReliableMessage(string message)
    {
        ReceiveMessage(message);
    }

    public void ReceiveUnreliableMessage(string message)
    {
        ReceiveMessage(message);
    }

    public void SendReliableMessage(RtcMessage message)
    {
        SendReliableMessage(message.GetMessage());
    }

    public void SendUnreliableMessage(RtcMessage message)
    {
        SendUnreliableMessage(message.GetMessage());
    }

    private void ReceiveMessage(string message)
    {
        RtcMessageReader reader = new RtcMessageReader(message);
        char messageTag = reader.ReadTag();

        switch (messageTag)
        {
            case MessageTags.InitPlayer:
                GameObject playerObj = Instantiate(playerPrefab);
                Player = playerObj.GetComponent<PlayerController>();
                Player.client = this;
                Player.SetColor(reader.ReadColorRGB());
                break;
            case MessageTags.CLIENT_POSITION_TAG:
                ushort id = reader.ReadUShort();
                if (id == ClientId)
                {
                    Player.UpdatePosition(reader);
                }
                else
                {
                    NetworkPlayers[id].UpdatePosition(reader);
                }

                break;
            case MessageTags.INIT_NETWORK_PLAYER:
                GameObject networkPlayerObj = Instantiate(networkPlayerPrefab);
                NetworkPlayerController networkPlayerController =
                    networkPlayerObj.GetComponent<NetworkPlayerController>();

                ushort networkPlayerId = reader.ReadUShort();
                networkPlayerController.SetColor(reader.ReadColorRGB());
                networkPlayerController.transform.position = new Vector3(reader.ReadFloat(), reader.ReadFloat(), 0);
                NetworkPlayers.Add(networkPlayerId, networkPlayerController);
                break;
            case MessageTags.REMOVE_PLAYER:
                ushort removeId = reader.ReadUShort();
                GameObject networkPlayer = NetworkPlayers[removeId].gameObject;
                Destroy(networkPlayer);
                NetworkPlayers.Remove(removeId);
                break;
        }
    }

    public void ReliableChannelOpen()
    {
        Debug.Log("Reliable Channel Open");
    }

    // Update is called once per frame
    void Update()
    {
    }
}