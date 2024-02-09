using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerController : MonoBehaviour
{
    public SpriteRenderer sRenderer;


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
