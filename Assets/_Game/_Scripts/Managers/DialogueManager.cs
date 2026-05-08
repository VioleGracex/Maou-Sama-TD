using UnityEngine;
using MaouSamaTD.Tutorial;
using MaouSamaTD.UI.Tutorial;
using Zenject;

namespace MaouSamaTD.Managers
{
    public class DialogueManager : MonoBehaviour
    {
        [Inject] private DialogueUI _dialogueUI;
        public DialogueUI DialogueUI => _dialogueUI;

        public bool IsDialogueActive { get; private set; }

        public void StartDialogue(DialogueData data, System.Action onComplete = null)
        {
            Debug.Log($"[DialogueManager] StartDialogue requested for {data?.name ?? "NULL"}");
            if (IsDialogueActive) 
            {
                Debug.LogWarning("[DialogueManager] Dialogue already active! Force closing and overriding with new dialogue.");
                HideDialogue();
            }

            if (_dialogueUI == null)
            {
                Debug.LogError("[DialogueManager] DialogueUI reference is missing!");
                onComplete?.Invoke();
                return;
            }

            IsDialogueActive = true;
            _dialogueUI.transform.SetAsLastSibling();
            _dialogueUI.gameObject.SetActive(true);
            _dialogueUI.ShowDialogue(data, () => 
            {
                Debug.Log("[DialogueManager] Dialogue sequence finished.");
                IsDialogueActive = false;
                onComplete?.Invoke();
            });
        }

        public void HideDialogue()
        {
            if (_dialogueUI != null) _dialogueUI.gameObject.SetActive(false);
            IsDialogueActive = false;
        }
    }
}
