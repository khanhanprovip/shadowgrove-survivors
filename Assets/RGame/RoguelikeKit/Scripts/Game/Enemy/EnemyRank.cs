using UnityEngine;
namespace RGame.RoguelikeKit
{
    public enum EnemyTier { Normal, Elite, Boss }
    public sealed class EnemyRank : MonoBehaviour { public EnemyTier Tier; }
}
