using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerManager : MonoBehaviour
{
    public WebRTCServer WebRtcServer;
    public GameObject playerPrefab;

    public Dictionary<ushort, ServerPlayerController> PlayerDictionary =
        new Dictionary<ushort, ServerPlayerController>();

    public Dictionary<ushort, ClientInputs> InputDictionary = new Dictionary<ushort, ClientInputs>();


    public float playerSpeed = 2;

    public void AddPlayer(ushort playerId, Color color, RtcClient client)
    {
        GameObject newPlayer = Instantiate(playerPrefab);
        ServerPlayerController serverPlayerController = newPlayer.GetComponent<ServerPlayerController>();
        serverPlayerController.manager = this;
        serverPlayerController.playerClient = client;
        serverPlayerController.SetColor(color);
        serverPlayerController.id = playerId;


        //Notify existing players that a new player has joined
        RtcMessage networkPlayerMessage = new RtcMessage(MessageTags.INIT_NETWORK_PLAYER);
        networkPlayerMessage.WriteUShort(playerId);
        networkPlayerMessage.WriteColorRGB(color);
        networkPlayerMessage.WriteFloat(serverPlayerController.transform.position.x);
        networkPlayerMessage.WriteFloat(serverPlayerController.transform.position.y);

        foreach (ushort id in PlayerDictionary.Keys)
        {
            PlayerDictionary[id].playerClient.SendReliableMessage(networkPlayerMessage);

            RtcMessage existingPlayerMessage = new RtcMessage(MessageTags.INIT_NETWORK_PLAYER);
            existingPlayerMessage.WriteUShort(id);
            existingPlayerMessage.WriteColorRGB(PlayerDictionary[id].color);
            existingPlayerMessage.WriteFloat(PlayerDictionary[id].transform.position.x);
            existingPlayerMessage.WriteFloat(PlayerDictionary[id].transform.position.y);

            client.SendReliableMessage(existingPlayerMessage);
        }

        PlayerDictionary.Add(playerId, serverPlayerController);
        InputDictionary.Add(playerId, new ClientInputs(Vector3.zero));
    }

    public void RemovePlayer(ushort playerId)
    {
        GameObject player = PlayerDictionary[playerId].gameObject;
        Destroy(player);

        PlayerDictionary.Remove(playerId);
        InputDictionary.Remove(playerId);
        RtcMessage removePlayerMessage = new RtcMessage(MessageTags.REMOVE_PLAYER);
        removePlayerMessage.WriteUShort(playerId);

        foreach (ServerPlayerController serverPlayerController in PlayerDictionary.Values)
        {
            serverPlayerController.playerClient.SendReliableMessage(removePlayerMessage);
        }
    }

    public void MessageAllPlayersUnreliable(RtcMessage message)
    {
        foreach (ServerPlayerController serverPlayerController in PlayerDictionary.Values)
        {
            serverPlayerController.playerClient.SendUnreliableMessage(message);
        }
    }

    public void ReceiveInputs(ushort clientId, RtcClient client, RtcMessageReader reader)
    {
        ClientInputs newInputs = new ClientInputs();
        newInputs.MoveVector = new Vector2(reader.ReadFloat(), reader.ReadFloat());
        Debug.Log("Received Inputs: " + newInputs.MoveVector);
        InputDictionary[clientId] = newInputs;
    }

    private void Update()
    {
        foreach (ushort id in PlayerDictionary.Keys)
        {
            Vector2 moveVector = InputDictionary[id].MoveVector;
            PlayerDictionary[id].transform.position +=
                new Vector3(moveVector.x, moveVector.y, 0) * (playerSpeed * Time.deltaTime);
        }
    }
}