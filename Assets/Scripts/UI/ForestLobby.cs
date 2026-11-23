using System.Linq;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace DATN.UI
{
    public static class ForestLobby
    {
        public static void Open(LevelSelectPanel panel, UnityAction start)
        {
            if (panel.transform.Find("Forest_Lobby") != null) return;
            var selection = Resources.FindObjectsOfTypeAll<LevelAndCharacterSO>().First(asset => asset.name == "LevelAndCharacterSO");
            var hero = selection.SelectCharacterSo; var level = selection.SelectLevelSO;
            var go = new GameObject("Forest_Lobby", typeof(RectTransform), typeof(Image)); go.transform.SetParent(panel.transform, false);
            ForestUI.Place(go.transform,0,0,1,1); go.GetComponent<Image>().color = Color.clear;
            ForestUI.Background(go.transform);
            ForestUI.Label(go.transform,"Forest_LobbyChapter","BÊN THỀM KHU RỪNG","AT THE FOREST EDGE",23,.52f,.76f,.9f,.82f,color:ForestUI.Gold);
            ForestUI.Label(go.transform,"Forest_LobbyTitle","Sẵn sàng lên đường?","Ready to depart?",52,.52f,.65f,.94f,.76f);
            ForestUI.Label(go.transform,"Forest_LobbyCopy","Ngọn đèn đã thắp. Một hành trình mới đang chờ.","The lantern is lit. A new journey awaits.",27,.52f,.55f,.93f,.65f,color:ForestUI.Muted);
            ForestUI.Picture(go.transform,"Forest_LobbySeal","sigil",.10f,.27f,.43f,.76f,true);
            var portrait=ForestUI.Picture(go.transform,"Forest_LobbyHero",null,.16f,.33f,.37f,.70f,true);portrait.sprite=ForestUI.HeroArt(hero.CharacterName);
            ForestUI.Label(go.transform,"Forest_LobbyName",hero.CharacterName,hero.CharacterName,46,.10f,.20f,.43f,.31f,TextAlignmentOptions.Center,ForestUI.Gold);
            var preview=ForestUI.Picture(go.transform,"Forest_LobbyPreview",null,.52f,.36f,.69f,.53f);preview.sprite=ForestUI.LevelArt(level.name);
            var levelText=ForestUI.Label(go.transform,"Forest_LobbyLevel","","",29,.71f,.35f,.94f,.54f);
            levelText.GetComponent<ForestText>().LevelAsset=level.name;
            levelText.GetComponent<ForestText>().LevelFallback=level.LevelDescription;
            ForestUI.NewButton(go.transform,"StartBattleBtn","VÀO TRẬN","ENTER BATTLE",()=>{ShadowgroveAudio.Play(SoundCue.UiConfirm);start();},.52f,.22f,.83f,.30f,true);
            ForestUI.NewButton(go.transform,"LobbyBackBtn","ĐỔI VÙNG ĐẤT","CHANGE LAND",()=>{ShadowgroveAudio.Play(SoundCue.UiSelect);Object.Destroy(go);},.52f,.12f,.83f,.20f);
        }
    }
}
