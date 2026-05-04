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
        public float CharactersPerSecond = 30f;
        public List<DialogueLine> Lines;
    }

    public enum PortraitFocus { None, Left, Right, Center, All }

    [System.Serializable]
    public struct DialogueLine
    {
        public string SpeakerName;
        
        // Compatibility fields for old assets
        [HideInInspector] public Sprite SpeakerPortrait;
        [HideInInspector] public bool PortraitOnLeft;

        public Sprite LeftPortrait;
        public Sprite RightPortrait;
        public Sprite CenterPortrait;
        public PortraitFocus Focus;

        [TextArea(3, 10)]
        public string Text;
        
        public DialogueBackground Background;

        // Optional: Trigger event or sound
        public string EventID;
    }
}
