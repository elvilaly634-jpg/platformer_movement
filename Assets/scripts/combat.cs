using UnityEngine;

public class combat : MonoBehaviour
{
    public float knockback_value=5;
    movement movement;
    float vertical;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movement=GetComponent<movement>();
    }

    // Update is called once per frame
    void Update()
    {
        vertical=Input.GetAxisRaw("Vertical");
        if (Input.GetMouseButtonDown(0))
        {
            movement.take_knockback(get_knock_back_direction()*-1,knockback_value);
        }
    }
    Vector2 get_knock_back_direction()
    {
        if (vertical == 0)
        {
            if (movement.horizontal == 0)
            {
                return new Vector2(1,0);
            }
            return new Vector2(Mathf.Round(movement.horizontal),vertical);
        }
        
        
        return new Vector2(0,vertical);
        
    }
}
