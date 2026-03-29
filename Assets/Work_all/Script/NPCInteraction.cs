using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public GameObject interactionUI;
    public DialogueSystem dialogueSystem;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            interactionUI.SetActive(false);
            dialogueSystem.StartDialogue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactionUI.SetActive(false);
        }
    }
}