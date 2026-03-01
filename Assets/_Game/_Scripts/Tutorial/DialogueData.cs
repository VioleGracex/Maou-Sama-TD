using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Tutorial
{
    public enum DialogueStyle { FullScreen, MiniTop }
    public enum DialogueBackground { None, UIBlocker, FullScreenDim }

    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "MaouSamaTD/Tutorial/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public DialogueStyle Style = DialogueStyle.FullScreen;
        public DialogueBackground Background = DialogueBackground.None;
        public float CharactersPerSecond = 30f;
        public List<DialogueLine> Lines;
    }

    [System.Serializable]
    public struct DialogueLine
    {
        public string SpeakerName;
        public Sprite SpeakerPortrait;
        [TextArea(3, 10)]
        public string Text;
        public bool PortraitOnLeft;
        
        // Optional: Trigger event or sound
        public string EventID;
    }
}
