using System.Collections;
using UnityEngine;

public class ControllerJoueur : MonoBehaviour
{
    public float speed = 5f;               // Unités par seconde
    public float gridSize = 0.5f;          // Taille d'une case de la grille (0.5 ou 1)

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isMoving)
            HandleInput();
    }

    void HandleInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Pas de diagonales
        if (moveInput.x != 0) moveInput.y = 0;

        if (moveInput != Vector2.zero)
        {
            UpdateAnimatorDirection(moveInput);
            Vector2 targetPos = rb.position + moveInput * gridSize;
            StartCoroutine(MoveTo(targetPos));
        }
        else
        {
            animator.SetFloat("isMoving", 0f);
        }
    }

    IEnumerator MoveTo(Vector2 targetPos)
    {
        isMoving = true;
        animator.SetFloat("isMoving", 1f);

        // Teste si on peut bouger vers la position cible
        Vector2 testPos = Vector2.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime);
        rb.MovePosition(testPos);
        yield return new WaitForFixedUpdate();

        // Si on n'a pas pu bouger, annule le mouvement
        if (Vector2.Distance(rb.position, testPos) > 0.0001f)
        {
            animator.SetFloat("isMoving", 0f);
            isMoving = false;
            yield break;
        }

        // Variable pour savoir si on a été bloqué par une collision
        bool blockedByCollision = false;

        // Sinon, continue le mouvement normalement
        while ((targetPos - rb.position).sqrMagnitude > 0.001f)
        {
            Vector2 previousPos = rb.position;
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();

            // Si on n'a pas bougé, on est bloqué par une collision
            if (Vector2.Distance(rb.position, previousPos) < 0.0001f)
            {
                blockedByCollision = true;
                break;
            }
        }

        // Arrondi à la grille SEULEMENT si on n'a pas été bloqué par une collision
        if (!blockedByCollision)
        {
            Vector2 newPos = rb.position;

            // Arrondi seulement sur l'axe du mouvement
            if (moveInput.x != 0) // Mouvement horizontal
                newPos.x = Mathf.Round(rb.position.x / gridSize) * gridSize;
            if (moveInput.y != 0) // Mouvement vertical  
                newPos.y = Mathf.Round(rb.position.y / gridSize) * gridSize;

            rb.position = newPos;
        }

        animator.SetFloat("isMoving", 0f);
        isMoving = false;
    }

    void UpdateAnimatorDirection(Vector2 dir)
    {
        if (dir.y > 0)
            animator.SetFloat("dir", 3f); // haut
        else if (dir.y < 0)
            animator.SetFloat("dir", 0f); // bas
        else if (dir.x < 0)
            animator.SetFloat("dir", 1f); // gauche
        else if (dir.x > 0)
            animator.SetFloat("dir", 2f); // droite
    }
}
