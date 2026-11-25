using UnityEngine;
namespace RGame.RoguelikeKit
{
    public static class SpawnPacing
    {
        // Total budget across all authored random/circle/dense sets, not per segment.
        public static float Rate(float seconds)
        {
            if(seconds<30)return Mathf.Lerp(.65f,1.1f,seconds/30);
            if(seconds<60)return Mathf.Lerp(1.1f,2f,(seconds-30)/30);
            if(seconds<120)return Mathf.Lerp(2f,3.5f,(seconds-60)/60);
            if(seconds<300)return Mathf.Lerp(3.5f,5f,(seconds-120)/180);
            return Mathf.Lerp(5f,6f,Mathf.Clamp01((seconds-300)/600));
        }
    }
}
