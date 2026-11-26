using RGame.RoguelikeKit;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace DATN.UI
{
    public sealed class RunContinueButton:MonoBehaviour
    {
        void OnEnable(){GetComponent<Button>().interactable=RunCheckpointSession.HasSavedRun;}
        void Start()
        {
            GetComponent<Button>().interactable=RunCheckpointSession.HasSavedRun;
            if(!string.IsNullOrEmpty(RunCheckpointSession.LastError))ForestUI.Label(transform.parent,"Load status","Chưa tải được trận đã lưu. Bản lưu vẫn được giữ.","The run could not be loaded. Your checkpoint is retained.",23,.58f,.15f,.95f,.23f,color:ForestUI.Gold);
        }
    }
    public sealed class RunSaveStatus:MonoBehaviour
    {
        TMP_Text label;
        void Update()
        {
            if(label==null){label=ForestUI.Label(transform,"Save status","","",20,.35f,.105f,.93f,.15f,color:ForestUI.Gold);label.GetComponent<ForestText>().enabled=false;}
            label.text=string.IsNullOrEmpty(RunCheckpointSession.LastError)?ForestUI.Tr("Chọn kỹ năng · Cuộn danh sách hoặc mô tả để xem tiếp.","Select a skill · Scroll the list or description to read more."):
                ForestUI.Tr("Chưa lưu được trận. Hãy thử lại; bạn vẫn đang tạm dừng.","The run could not be saved. Try again; the game remains paused.");
        }
    }
}
