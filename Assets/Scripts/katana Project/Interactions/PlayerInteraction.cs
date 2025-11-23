using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 1.5f;
    public LayerMask interactLayer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E");
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, interactRange, interactLayer);
            if (hit.collider != null)
            {
                IInteractable target = hit.collider.GetComponent<IInteractable>();
                target?.Interact();
            }
        }
    }
}
