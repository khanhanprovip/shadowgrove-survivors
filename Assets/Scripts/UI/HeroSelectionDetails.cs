using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;

namespace DATN.UI
{
    /// <summary>Compact, live-localized passive and signature copy for hero selection.</summary>
    public static class HeroSelectionDetails
    {
        public static void AddCharacterCardLines(Transform card, string heroId)
        {
            if (card == null || HeroLocalization.KindForHeroId(heroId) == StarterWeaponKind.Legacy) return;

            ForestUI.Label(card, "Forest_HeroPassive",
                HeroLocalization.SelectionPassiveCopy(heroId, true),
                HeroLocalization.SelectionPassiveCopy(heroId, false),
                16, .045f, .09f, .955f, .135f, TextAlignmentOptions.Center, ForestUI.Muted);
            ForestUI.Label(card, "Forest_HeroSignature",
                HeroLocalization.SelectionSignatureCopy(heroId, true),
                HeroLocalization.SelectionSignatureCopy(heroId, false),
                16, .045f, .035f, .955f, .08f, TextAlignmentOptions.Center, ForestUI.Gold);
        }

        public static void AddLobbyLines(Transform lobby, string heroId)
        {
            if (lobby == null || HeroLocalization.KindForHeroId(heroId) == StarterWeaponKind.Legacy) return;

            ForestUI.Label(lobby, "Forest_LobbyHeroPassive",
                HeroLocalization.SelectionPassiveCopy(heroId, true),
                HeroLocalization.SelectionPassiveCopy(heroId, false),
                18, .055f, .135f, .475f, .181f, TextAlignmentOptions.Center, ForestUI.Muted);
            ForestUI.Label(lobby, "Forest_LobbyHeroSignature",
                HeroLocalization.SelectionSignatureCopy(heroId, true),
                HeroLocalization.SelectionSignatureCopy(heroId, false),
                18, .055f, .078f, .475f, .128f, TextAlignmentOptions.Center, ForestUI.Gold);
        }
    }
}
