              #region

using RGame.Framework;
using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(fileName = "SaveSystem", menuName = "RGame/RoguelikeKit/Settings/SaveSystem")]
    public class SaveSystemSO : ScriptableObject
    {
        [SerializeField] private VoidEventChannelSO _saveData;
        [SerializeField] private SettingsSO _currentSettings;
        [SerializeField] private AttributeSO _attribute;
        [SerializeField] private CharacterSaveSO _characterSave;
        
        public string saveFilename = "RoguelikeKit.RGame";
        public string backupSaveFilename = "RoguelikeKit.RGame.bak";
        [HideInInspector] public SaveGame Save = new();

        private void OnEnable()
        {
            _saveData.RegisterListener(SaveDataToDisk);
        }

        private void OnDisable()
        {
            _saveData.UnregisterListener(SaveDataToDisk);
        }

        public void LoadSaveDataFromDisk()
        {
            if (!TryLoad(saveFilename) && !TryLoad(backupSaveFilename)) return;
            LoadSettings();
            LoadPowerUp();
            LoadCharacter();
        }

        private bool TryLoad(string fileName)
        {
            if (!FileManager.LoadFromFile(fileName, out var json) || string.IsNullOrWhiteSpace(json))
                return false;
            try
            {
                var saved = new SaveGame();
                saved.LoadFromJson(json);
                Save = saved;
                return true;
            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"Could not read save {fileName}: {e.Message}");
                return false;
            }
        }

        public void SaveDataToDisk()
        {
            SaveSettings();
            SavePowerUp();
            SaveCharacter();
            
            FileManager.WriteToFile(saveFilename, Save.ToJson(), backupSaveFilename);
        }
        public void BankEarnings(int amount)
        {
            if(amount<=0)return;
            _attribute.SetAttribute("GoldCount",_attribute.GoldCount+amount);
            SaveDataToDisk();
        }

        public void WriteEmptySaveFile()
        {
            FileManager.WriteToFile(saveFilename, "");
        }

        private void SaveSettings()
        {
            Save.SaveSettings(_currentSettings);
        }
        
        private void SavePowerUp()
        {
            Save.SaveAttributes(_attribute);
        }

        private void SaveCharacter()
        {
            Save.SaveCharacter(_characterSave);
        }
        
        private void LoadSettings()
        {
            _currentSettings.LoadSavedSettings(Save);
        }
        
        private void LoadPowerUp()
        {
            _attribute.LoadSavedAttributes(Save);
        }

        private void LoadCharacter()
        {
            _characterSave.LoadCharacter(Save);
        }
    }
}
