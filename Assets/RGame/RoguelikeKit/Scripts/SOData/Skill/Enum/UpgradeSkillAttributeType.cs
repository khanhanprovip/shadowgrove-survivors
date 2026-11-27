using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum UpgradeSkillAttributeType
    {
        DamageAdd,
        CDLessPercent,
        ProjectilesCountAdd,
        VelocityAddPercent,
        AreaAddPercent,
        // Appended to preserve the serialized values of existing generic skills.
        SignatureLevel
    }
}
