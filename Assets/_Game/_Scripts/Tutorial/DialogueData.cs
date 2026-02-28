using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Tutorial
{
    public enum DialogueStyle { FullScreen, MiniTop }

    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "MaouSamaTD/Tutorial/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public DialogueStyle Style = DialogueStyle.FullScreen;
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
