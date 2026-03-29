using UnityEngine;
using TMPro;
using StarterAssets;

public class DialogueSystem : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public string[] dialogueLines;

    private int currentLine = 0;
    private bool isTalking = false;

    public ThirdPersonController playerController;
    public Animator npcAnimator;

    void Update()
    {
        // ถ้ากำลังคุยอยู่ และกด Space
        if (isTalking && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        currentLine = 0;
        dialogueText.text = dialogueLines[currentLine];
        isTalking = true;

        playerController.enabled = false;

        if (npcAnimator != null)
            npcAnimator.SetTrigger("Talk");
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isTalking = false;
        playerController.enabled = true;
    }
}