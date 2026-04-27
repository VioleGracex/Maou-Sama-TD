using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Story
{
    public enum PortraitFocus { None, Left, Middle, Right, All }

    [System.Serializable]
    public class StoryLine
    {
        public string SpeakerName;
        [TextArea(3, 10)]
        public string DialogueText;
        
        public Sprite PortraitLeft;
        public Sprite PortraitMiddle;
        public Sprite PortraitRight;
        
        public Sprite Background;
        
        public PortraitFocus Focus = PortraitFocus.None;
        
        // Optional: Trigger event or sound
        public string EventID;
        public AudioClip VoiceClip;
    }

    [CreateAssetMenu(fileName = "NewStoryData", menuName = "MaouSamaTD/Story/Story Data")]
    public class StoryDataSO : ScriptableObject
    {
        public List<StoryLine> Lines = new List<StoryLine>();
    }
}
