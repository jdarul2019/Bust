using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer spriteRenderer;
    
    // 1. Dodajemy zmienną dla Animatora
    private Animator anim; 
    
    // Globalna flaga pozwalająca włączać/wyłączać ruch z każdego innego skryptu na scenie
    public static bool canMove = true;
    [Header("Skiny")]
    public RuntimeAnimatorController suitAnimatorController;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); 

        // Reset ruchu przy każdym załadowaniu sceny —
        // static persystuje między scenami więc wymuszamy świeży start
        canMove = true;
        if (GameManager.Instance != null && GameManager.Instance.hasSuit)
        {
            PutOnSuit();
        }
    }

    void Update()
    {
        // Jeśli ruch jest zablokowany, np. przez otwarte okno minigry
        if (!canMove)
        {
            // Natychmiastowe zerowanie pędu gracza (nie jedzie dalej jak po lodzie) i wymuszenie animacji stania
            movement = Vector2.zero;
            if (anim != null) anim.SetBool("isMoving", false);
            return; // Przerwanie czytania przycisków
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.x > 0) spriteRenderer.flipX = true;
        else if (movement.x < 0) spriteRenderer.flipX = false;

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

    public void PutOnSuit()
    {
        if (anim != null && suitAnimatorController != null)
        {
            anim.runtimeAnimatorController = suitAnimatorController;
        }
    }
}