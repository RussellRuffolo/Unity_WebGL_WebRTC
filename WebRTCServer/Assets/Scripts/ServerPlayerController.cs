using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerController : MonoBehaviour
{
  
    public Vector2 lastPosition = Vector2.zero;
    public ushort id;
    public Rigidbody2D rb;
    public RtcClient playerClient;
    public ServerPlayerManager manager;
    public SpriteRenderer sRenderer;
    public Color color;

    public void SetColor(Color col)
    {
        color = col;
        sRenderer.color = col;
    }

    private void Update()
    {
        if (Vector2.Distance(lastPosition, transform.position) > .1f)
        {
            RtcMessage positionMessage = new RtcMessage(MessageTags.CLIENT_POSITION_TAG);
            positionMessage.WriteUShort(id);
            positionMessage.WriteFloat(transform.position.x);
            positionMessage.WriteFloat(transform.position.y);
            manager.MessageAllPlayersUnreliable(positionMessage);
        }
    }
}