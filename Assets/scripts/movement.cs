using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public enum state
{
    walking,
    dashing,
    wall_sliding,
    standing,
    wall_jumping,
    knockback,
    air_born
}
public enum side
{
    up,
    down,
    left,
    right,
    up_right,
    up_left,
    down_right,
    down_left,
    left_top,
    left_bottom,
    right_top,
    right_bottom
}
public class rays
{
    public RaycastHit2D raycastHit2D;
    public side side;
    public Vector2 direction;
    public LayerMask layerMask;
    public Vector2 origin;
    public int x;
    public int y;
    public rays(side side,Vector2 direction,LayerMask layerMask,Vector2 origin,int x,int y)
    {
        this.side=side;
        this.direction=direction;
        this.layerMask=layerMask;
        this.origin=origin;
        this.x=x;
        this.y=y;
    }
    public void update_ray(float distance)
    {
        raycastHit2D=Physics2D.Raycast(origin,direction,distance,layerMask);
    }
}

public class movement : MonoBehaviour
{
    
    
    public float knockback_cooldown=0.2f;
    float knockback_timer;
    float wall_jump_timer;
    public float wall_jump_cooldown=0.3f;
    public float wall_sliding_speed=3;
    public float sliding_gravity_scale=0.5f;
    public float wall_push=7;
    bool can_dash=true;
    [NonSerialized]public float horizontal;
    public float correct_distance=2;
    public float dashing_speed=50;
    
    public float speed=10;
    public float jump_force=10;
    public float dash_duration=1.8f;
    public float dash_cooldown=0.5f;
    float dash_cooldown_timer;
    public Slider dash_slider;
    float dash_timer;
    bool wall_sliding;
    int wall_jump_direction;
    
    public Rigidbody2D rb;
    float gravityscale;
    List<rays> directions_rays=new List<rays>();
    public LayerMask obstacle_layers_to_hit;
    bool buffer_jump=false;
    float jump_buffer_timer;
    public float jump_buffer_cooldown=0.2f;
    side side_horizontal;
    side side_vertical;
    Vector2 directionx;
    Vector2 directiony;
    BoxCollider2D collider2D;
    
    [NonSerialized]public state current_state=state.walking;
    public KeyCode dashing_button=KeyCode.LeftShift;
    public float apex_gravity_scale=0.9f;
    public float falling_gravity_scale=2.2f;
    public float attack_range=1;
    float walking_speed;
    public LayerMask enemy_layers;
    [NonSerialized] public int last_direction=1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        walking_speed=speed;
        rb=GetComponent<Rigidbody2D>();
        gravityscale=rb.gravityScale;
        collider2D=GetComponent<BoxCollider2D>();
       
        directions_rays=create_rays(obstacle_layers_to_hit);
        
        dash_slider.maxValue=dash_cooldown;

    }

    // Update is called once per frame
 
 

    void FixedUpdate()
    {
        
        #region dashing_walking_jumping
            horizontal=Input.GetAxisRaw("Horizontal");
            if(horizontal!=0){last_direction=(int)horizontal;}
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if(is_grounded() || wall_sliding)
                {
                    jump(jump_force);
                }
                else
                {
                    buffer_jump=true;
                    jump_buffer_timer=Time.time;
                }
                
            }
            else if (buffer_jump && Time.time - jump_buffer_timer <= jump_buffer_cooldown)
            {
                if(get_side(new List<side> { side.down_left, side.down_right }) || wall_sliding)
                {
                    jump(jump_force);
                    buffer_jump=false;
                }
            }
            else
            {
                buffer_jump=false;
            }
            if (Time.time - dash_cooldown_timer >= dash_cooldown)
            {
                can_dash=true;
            }
            dash_slider.value=Time.time-dash_cooldown_timer;
            if (Input.GetKeyDown(dashing_button)&&current_state!=state.dashing&&can_dash)
            {
                
                current_state=state.dashing;
            }

            
            if (rb.linearVelocityY < 0.7 && rb.linearVelocityY > -0.7f&&current_state==state.walking)
            {
                
                rb.gravityScale=apex_gravity_scale;
            }
            else if (rb.linearVelocityY <= -1.5f)
            {
                rb.gravityScale=falling_gravity_scale;
            }
            else{rb.gravityScale=gravityscale;}
            
            if (current_state == state.walking)
            {
                dash_timer=Time.time;
                
                rb.linearVelocity=new Vector2(horizontal*speed,rb.linearVelocityY);
                
            }   
            
            if (current_state == state.dashing)
            {
                dash(dashing_speed);
                
            
                if(get_side(new List<side>{side.left_top})|get_side(new List<side> {  side.right_top }))
                {
                    rb.linearVelocityX=0;
                    current_state=state.walking;
                    dash_cooldown_timer=Time.time;
                }
            
                else if (Time.time - dash_timer >= dash_duration)
                {
                    current_state=state.walking;
                    rb.linearVelocityX=0;
                    dash_cooldown_timer=Time.time;
                }
                
            }
            update_rays(0.3f,0.5f,0.5f,0.5f,directions_rays);
            correct();
        #endregion

        #region wall_sliding_and_jumping
        if (is_wall_sliding()&&current_state!=state.wall_jumping)
        {
            wall_sliding=true;
            
            current_state=state.wall_sliding;
            rb.linearVelocity=new Vector2(rb.linearVelocityX,Mathf.Clamp(rb.linearVelocityY,-wall_sliding_speed,float.MaxValue));
        }
        else if (current_state == state.wall_sliding && !is_wall_sliding())
        {
            wall_sliding=false;
            
            current_state=state.walking;
        }
        if (current_state == state.wall_jumping && Time.time - wall_jump_timer >= wall_jump_cooldown)
        {
            current_state=state.walking;
        }
        #endregion

        #region knockback

            if (current_state == state.knockback&&Time.time-knockback_timer>=knockback_cooldown)
            {
                current_state=state.walking;
                rb.gravityScale=gravityscale;
            }
        #endregion

    }

    public void take_knockback(Vector2 direction,float strength)
    {
        current_state=state.knockback;
        knockback_timer=Time.time;

        rb.linearVelocity=new Vector2(direction.x*strength*0.9f,direction.y*strength*1.5f);
        
    }

    void jump(float force){
        if(get_side(new List<side>{side.down_left,side.down_right}) ){
        
            
            rb.linearVelocity=new Vector2(rb.linearVelocityX,force);
            
            
            current_state=state.walking;
        }
        else if(wall_sliding)
        {
            wall_sliding=false;
            current_state=state.wall_jumping;
            wall_jump_timer=Time.time;
            rb.linearVelocity=new Vector2(wall_push*wall_jump_direction,force);
        }
    }

    bool is_grounded()
    {
        return get_side(new List<side>{side.down_left,side.down_right});
    }
    bool is_wall_sliding()
    {
        List<side> sides=get_sides();
        List<side> left_sides=new List<side>{side.left_bottom,side.left_top};
        List<side> right_sides=new List<side>{side.right_bottom,side.right_top};
        List<side> ground_sides=new List<side>{side.down_left,side.down_right};
        if (!is_grounded() && get_specific_sides(left_sides))
        {
            
            wall_jump_direction=1;
            return true;
        }
        if (!is_grounded() && get_specific_sides(right_sides))
        {
            
            wall_jump_direction=-1;
            return true;
        }
        return false;

        
    }
    bool get_specific_sides(List<side> sides)
    {
        List<side> sides1=get_sides();
        foreach(side side in sides)
        {
            if (!sides1.Contains(side))
            {
                return false;
            }
        }
        return true;
    }
    List<side> get_sides()
    {
        List<side> sides=new List<side>();
        foreach(rays rays in directions_rays)
        {
            Collider2D collider2D=rays.raycastHit2D.collider;
            if (collider2D != null)
            {
                sides.Add(rays.side);
            }
        }
        return sides;
    }
    bool get_one_side(side target_side)
    {
        List<side> sides=get_sides();
        if(sides.Contains(target_side)){return true;}
        return false;
    }
    void dash(float dashing_speed)
    {
        rb.linearVelocity=new Vector2(dashing_speed*last_direction,rb.linearVelocityY);
    }

    void update_rays(float top,float bottom,float left,float right,List<rays> target_list)
    {
        
        foreach(rays ray in target_list)
        {
            Vector2 corner = transform.TransformPoint(collider2D.offset + Vector2.Scale(collider2D.size / 2, new Vector2(ray.x, ray.y)));
            ray.origin=corner;
            if (ray.side == side.up_left | ray.side == side.up_right)
            {
                ray.update_ray(top);
            }
            else if (ray.side == side.down_left | ray.side == side.down_right)
            {
                ray.update_ray(bottom);
            }
            else if (ray.side == side.left_bottom | ray.side == side.left_top)
            {
                ray.update_ray(left);
            }
            else if (ray.side == side.right_bottom | ray.side == side.right_top)
            {
                ray.update_ray(right);
            }
            
            
        }
        
    }
    List<Collider2D> get_colliders(List<rays> target_list){
        List<Collider2D> colliders=new List<Collider2D>();
        foreach(rays rays in target_list)
        {
            Collider2D collider2D=rays.raycastHit2D.collider;
            if (collider2D != null)
            {
                colliders.Add(collider2D);
            }
        }
        return colliders;

    }
    Collider2D get_collider(rays rays)
    {
        if (rays.raycastHit2D.collider != null)
        {
            return rays.raycastHit2D.collider;
        }
        return null;
    }

    void correct()
    {
        if (current_state == state.walking || current_state == state.dashing)
        {
            if (get_one_side(side.left_bottom) && !get_one_side(side.left_top)&&is_grounded())
            {
                if (!get_specific_sides(new List<side>{side.up_left,side.up_right}))
                {
                    RaycastHit2D midle_cast=Physics2D.Raycast(transform.position,Vector2.left,1,obstacle_layers_to_hit);
                    if (midle_cast.collider == null&&horizontal<0)
                    {   
                        rb.position=new Vector2(rb.position.x,rb.position.y+0.1f);
                        if (get_ray(side.left_bottom) == null)
                        {
                            rb.position=new Vector2(rb.position.x-0.4f,rb.position.y);
                        }
                    }
                }
                
            }
            else if (get_one_side(side.right_bottom) && !get_one_side(side.right_top)&&is_grounded())
            {
                if (!get_specific_sides(new List<side>{side.up_left,side.up_right}))
                {
                    RaycastHit2D midle_cast=Physics2D.Raycast(transform.position,Vector2.right,1,obstacle_layers_to_hit);
                    if (midle_cast.collider == null&&horizontal>0)
                    {   
                        rb.position=new Vector2(rb.position.x,rb.position.y+0.1f);
                        if (get_ray(side.right_bottom) == null)
                        {
                            rb.position=new Vector2(rb.position.x+0.4f,rb.position.y);
                        }
                    }
                }
                
            }
        }
    }

    public List<rays> get_rays()
    {
        List<rays> rayss=new List<rays>();
        foreach(rays ray in directions_rays)
        {
            Collider2D collider2D=ray.raycastHit2D.collider;
            if (collider2D != null)
            {
                rayss.Add(ray);
            }
        }
        return rayss;
    }
    public bool get_side(List<side>target_side){

        List<side> sides=get_sides();
        foreach(side side in sides){
            if(target_side.Contains(side)){
                return true;
            }
        }

        return false;
    }
    
    
    List<rays> create_rays(LayerMask target_layers)
    {
        List<rays> temp=new List<rays>();
        for(int y = -1; y < 2; y += 2)
        {
            for (int x = -1; x < 2; x += 2)
            {

                if (y == -1)
                {
                    directiony=Vector2.down;
                    if (x == -1)
                    {
                        directionx=Vector2.left;
                        side_horizontal=side.down_left;
                        side_vertical=side.left_bottom;
                    }
                    else
                    {
                        directionx=Vector2.right;
                        side_horizontal=side.down_right;
                        side_vertical=side.right_bottom;
                    }
                }
                else
                {
                    directiony=Vector2.up;
                    if (x == -1)
                    {
                        directionx=Vector2.left;
                        side_horizontal=side.up_left;
                        side_vertical=side.left_top;
                    }
                    else
                    {
                        directionx=Vector2.right;
                        side_horizontal=side.up_right;
                        side_vertical=side.right_top;
                    }
                }
                Vector2 corner = transform.TransformPoint(collider2D.offset + Vector2.Scale(collider2D.size / 2, new Vector2(x, y)));
                temp.Add(new rays(side_horizontal,directiony,target_layers,corner,x,y));
                temp.Add(new rays(side_vertical,directionx,target_layers,corner,x,y));
            }
        }
        return temp;
    }
    
    public rays get_ray(side tartget_side)
    {
        List<rays> rays1=get_rays();
        foreach(rays rays in rays1)
        {
            if (rays.side == tartget_side)
            {
                return rays;
            }
        }
        return null;
    }
}
