using System.Collections.Generic;
using UnityEngine;

public class combat : MonoBehaviour
{
    public float knockback_value=5;
    movement movement;
    int vertical;
    int directionx;
    public LayerMask enemy_layers;
    public float hit_radius=1;
    BoxCollider2D collider2D;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movement=GetComponent<movement>();
        collider2D=GetComponent<BoxCollider2D>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
        vertical=(int)Input.GetAxisRaw("Vertical");
        if (Input.GetMouseButtonDown(0))
        {
            attack();
        }
    }
    Vector2Int get_knock_back_direction()
    {
        if (vertical == 0)
        {
            if (movement.horizontal == 0)
            {
                return new Vector2Int(movement.last_direction,0);
            }
            return new Vector2Int(movement.last_direction,vertical);
        }
        
        
        return new Vector2Int((int)movement.horizontal,vertical);
        
    }
    void attack()
    {
        Vector2Int hit_direction=get_knock_back_direction();
        List<Collider2D> hit_colliders=new List<Collider2D>();
        hit_colliders.AddRange(Physics2D.OverlapCircleAll(new Vector2(transform.position.x+(collider2D.size.x/2)*hit_direction.x,transform.position.y+(collider2D.size.y/2)*hit_direction.y),hit_radius,enemy_layers));
        if (hit_colliders.Count!=0)
        {
            movement.take_knockback(get_knock_back_direction()*-1,knockback_value);
        }
        
    }
    bool is_object_there(Vector2Int direcion)
    {
        return false;
    }
    bool collided(RaycastHit2D raycastHit2D)
    {
        if(raycastHit2D.collider!=null){return true;}
        return false;
    }
}
