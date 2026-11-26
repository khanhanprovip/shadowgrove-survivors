# Lưu trận đang chơi

Trong menu tạm dừng, chọn **Lưu và về menu**. Ở menu chính chọn **Tiếp tục trận đã lưu**; trận được mở trong trạng thái tạm dừng để người chơi kiểm tra kỹ năng rồi tiếp tục.

Bản lưu giữ nhân vật/màn, cả đồng hồ mục tiêu và đồng hồ trận, HP/EXP/cấp/thuộc tính, kỹ năng và trạng thái tiến hóa, nhịp hồi chiêu, vàng/kill, lịch sinh quái, nhóm xáo trộn boss, quái còn sống hoặc đang tan biến, đạn, vật phẩm, rương còn lại, hiệu ứng trạng thái và các vùng đánh boss. Boss đang xuất hiện hoặc ra chiêu tiếp tục từ pha đã lưu. Vệt trang trí, số sát thương và phản hồi ánh sáng ngắn được dựng lại; chúng không thay đổi vùng gây sát thương.

Đây là chức năng tạm lưu một lượt chơi: checkpoint được đánh dấu đã dùng sau khi tải thành công, tránh nhận lại cùng phần thưởng. Muốn tạm dừng lượt chơi thêm lần nữa, chọn **Lưu và về menu** để ghi mốc mới. Mua nâng cấp vĩnh viễn và cài đặt tiếp tục dùng file riêng; snapshot không ghi đè tiến độ đã mua.

File sản phẩm nằm trong `Application.persistentDataPath` (Windows: `%USERPROFILE%\AppData\LocalLow\RGame\Shadowgrove Survivors`):

- `Shadowgrove.run.json`: dữ liệu trận, phiên bản schema và SHA-256 để phát hiện hỏng dữ liệu.
- `Shadowgrove.run.bak`: checkpoint trước lần thay thế gần nhất.
- `Shadowgrove.run.consumed`: danh sách checkpoint đã tiếp tục thành công, bao gồm bản dự phòng.

File chính được thay thế nguyên tử qua `FileManager`; bản chính hỏng có thể đọc bản dự phòng hợp lệ chưa dùng. Nếu ghi hoặc khôi phục thất bại, giữ checkpoint cũ và báo trên giao diện. Tải thất bại không ngân hàng hóa trạng thái khôi phục dở dang.

## Kiểm thử Windows

```powershell
python Tools/UnityHarness/harness.py verify --suites pause-skills,run-save-write,run-save-read,run-save-modes,run-save-bosses,bosses --timeout 750
```

- `run-save-write` và `run-save-read` chạy hai tiến trình Player riêng, dùng chung một profile QA riêng của lượt harness. Lượt đầu lưu giữa chiêu boss với đạn Lyra trên sân; lượt sau mở game từ Initialization, bấm Tiếp tục và đối chiếu trạng thái thật.
- `run-save-modes` kiểm tra Rowan/Survival, Lyra/KillTarget, Kael/Endless; các đạn khác nhau, quái bị đóng băng, rương đã nhặt, HP, chỉ số, nhịp hồi chiêu và nhịp sinh quái. Ca này ép một lần ghi ví vàng thất bại bằng tên đích trỏ vào thư mục profile QA, kiểm tra vẫn pause, không mất vàng và không thay checkpoint trước đó. Cuối ca, chỉ file QA bị làm hỏng để thử fallback và chống sử dụng lại backup.
- `run-save-bosses` kiểm tra cả năm boss sau khi xáo trộn: mỗi con lưu/tải giữa màn xuất hiện và giữa chuẩn bị chiêu, rồi tiếp tục đánh/chết/trao thưởng. Mốc boss sau dùng bước nhảy đồng hồ QA, không coi là chơi tay liên tục 25 phút.
- `pause-skills` kiểm tra bấm từng dòng, hai vùng cuộn độc lập, VI/EN và cả ba nhân vật; đồng hồ và chỉ số phải giữ nguyên khi đọc. GPU readback kiểm tra alpha bóng trên vật liệu quái thật và bóng nhân vật.

Kiểm tra `results.json`, từng `report-*.txt`, log Player và mã thoát. Một ca thiếu báo cáo, FAIL, lỗi runtime, timeout hoặc mã thoát khác 0 vẫn là thất bại. Không dùng profile người chơi thật để kiểm thử.

`RunStateFields` chỉ khôi phục trường tồn tại trên kiểu component tin cậy đã có trong prefab. File không chỉ định kiểu component để tạo hay phương thức để gọi. Prefab/scene tiếp tục được nạp qua các kênh Unity hiện có; checkpoint không sửa YAML hoặc asset cấu hình.
