using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer spriteRenderer;
    
    // 1. Dodajemy zmienną dla Animatora
    private Animator anim; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 2. Pobieramy Animatora
        anim = GetComponent<Animator>(); 
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.x < 0) spriteRenderer.flipX = true;
        else if (movement.x > 0) spriteRenderer.flipX = false;

        // 3. Sprawdzamy, czy gracz się rusza (w lewo/prawo lub góra/dół)
        if (movement.x != 0 || movement.y != 0)
        {
            anim.SetBool("isMoving", true); // Odpal animację Walk!
        }
        else
        {
            anim.SetBool("isMoving", false); // Wróć do Idle!
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}