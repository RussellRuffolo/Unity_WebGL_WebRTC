using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public SpriteRenderer sRenderer;

    public WebRTCClient client;

    public void SetColor(Color color)
    {
        sRenderer.color = color;
    }

    private Vector2 targetPosition = Vector2.zero;

    public void UpdatePosition(RtcMessageReader reader)
    {
        float x = reader.ReadFloat();
        float y = reader.ReadFloat();

        targetPosition = new Vector2(x, y);
    }


    // Update is called once per frame
    void Update()
    {
        RtcMessage inputMessage = new RtcMessage(MessageTags.INPUT_TAG);

        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.UpArrow);
        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);


        Vector2 moveVector = Vector2.zero;

        if (right)
        {
            moveVector += Vector2.right;
        }

        if (left)
        {
            moveVector += Vector2.left;
        }

        if (up)
        {
            moveVector += Vector2.up;
        }

        if (down)
        {
            moveVector += Vector2.down;
        }

        inputMessage.WriteFloat(moveVector.x);
        inputMessage.WriteFloat(moveVector.y);

        client.SendUnreliableMessage(inputMessage);



        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.y), targetPosition) > .01)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, .4f);
        }
        
        else
        {
            transform.position = targetPosition;
        }     
    }
}