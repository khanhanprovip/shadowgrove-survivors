# Shadowgrove Survivors

![Shadowgrove Survivors](Media/ShadowgroveLogo.png)

Đồ án game Unity 2D thuộc thể loại survivors roguelike, với chủ đề rừng cổ và phong cách Nhật Thực Ngọc Bích. Phiên bản này được phát triển và duy trì bởi [khanhanprovip](https://github.com/khanhanprovip), dựa trên Survivors Roguelike Kit của Roo-Roo-Roo.

![Gameplay Windows](Media/ShadowgroveGameplay.png)

## Nội dung hiện có

- Ba nhân vật: Rowan, Lyra và Kael; sprite, portrait và animation Idle/Run/Attack.
- Mười màn chơi với nền riêng; ba chế độ Survival, KillTarget và Endless.
- Survival thắng sau 90 hoặc 120 giây tùy màn; KillTarget thắng ở 30 kill; Endless tăng độ khó và kết thúc khi người chơi chết.
- UI tiếng Việt/Anh: menu, chọn nhân vật/màn, chuẩn bị trận, HUD, kỹ năng, cài đặt, tạm dừng và kết quả.
- Bộ hình quái/boss, icon và VFX theo cùng phong cách; nháy đỏ nhẹ khi nhận sát thương.
- Rương nhận 50 vàng trong trận khi chạm; chống nhận lặp.
- Âm thanh, save, nâng cấp và các công cụ kiểm thử Unity/Windows.
- Quái thường 30 HP, tinh anh 90 HP. Tối đa 40 quái thường hoạt động trên toàn bản đồ trong mọi chế độ; đạt trần thì ngừng sinh thêm, có chỗ trống mới sinh bù. Nhịp sinh quái nhẹ ở đầu trận rồi tăng dần; đợt vòng/cụm dùng chung ngân sách sinh, không quá 2 quái mới trong một frame.
- Boss chỉ xuất hiện trong Endless, mỗi 5 phút theo đồng hồ trận. Khi boss xuất hiện, quái nhỏ và đạn quái được dọn, quái nhỏ tạm ngừng sinh cho đến khi boss bị hạ.
- Năm boss xoay tua: Ảnh Hồn Nhật Thực, Cổ Vương Gai, Ai Nữ Băng Sương, Huyết Nguyệt Đao Phủ và Tiên Tri Vực Thẳm. Mỗi boss có 5 chiêu theo chủ đề, chọn ngẫu nhiên và tránh lặp ngay chiêu vừa dùng; chuẩn bị ra đòn qua chuyển động cơ thể, không vẽ cảnh báo trên sàn hoặc nhãn báo chiêu riêng.
- Boss đầu 300 HP, mỗi lượt tiếp theo thêm 300 HP, kể cả sau khi quay lại boss đầu. Boss cao khoảng 5 lần nhân vật, có tên màu riêng phía trên thanh máu. Không cộng dồn hai boss nếu trận đánh kéo dài qua mốc tiếp theo. Có banner tên/màu khi xuất hiện; năm bộ sprite animation riêng (180 sprite, 30 clip) cho chờ, di chuyển, chuẩn bị, tấn công, nhận đòn và chết. Ảnh Hồn giữ trượng/niệm phép; Cổ Vương bước bằng rễ/giáng tay; Ai Nữ lướt/bung tinh thể; Đao Phủ lao/vung lưỡi hái; Tiên Tri co/phóng xúc tu.
- VFX chiêu theo năm chủ đề linh hồn/gai/băng/lưỡi hái/mắt vực thẳm; hiệu ứng tay và thân khi chuẩn bị, vệt/đạn/vòng khi đòn đã kích hoạt. Boss quay trái/phải theo hướng ngắm và giữ hướng suốt đòn; di chuyển theo hướng đi, không lật tên/thanh máu. Hướng gần thẳng đứng giữ hướng ngang gần nhất.
- Hạ boss nhận trực tiếp 5 cấp và 500 vàng. Năm lượt chọn nâng cấp được xếp hàng; vàng được lưu theo phần chưa ghi nhận để tránh mất hoặc cộng hai lần khi chơi lại/về menu.
- Màn tạm dừng có danh sách kỹ năng tấn công/thuộc tính đang sở hữu, cấp và chỉ số, hỗ trợ cuộn và VI/EN.

## Chiêu khởi đầu

| Nhân vật | Đòn đánh | Tầm cơ bản (đơn vị Unity) |
| --- | --- | ---: |
| Rowan | Nhát Chém Gai, chém hình quạt cận chiến | 2,2 |
| Lyra | Cầu Bóng Đêm, đạn ma thuật | 7 |
| Kael | Tên Tàn Lửa, tên bay nhanh | 10,5 |

Chỉ tung đòn khi có quái hợp lệ trong tầm tròn. Đạn được thu hồi khi trúng hoặc hết tầm. Màn chọn nhân vật, HUD và thẻ nâng cấp dùng tên/icon riêng. Các ô Sức mạnh có vùng bấm phủ toàn ô để xem đúng mô tả, kể cả khi thuộc tính đã tối đa.

Đã kiểm tra UI, combat/rương và thẻ kỹ năng trên bản Windows ở 1280×720 và 1920×1080. Đồng hồ Survival được chặn đúng mốc 90/120 giây khi frame vượt hạn. [Kết quả và phạm vi kiểm chứng](Tools/UnityHarness/verification-2026-11-23.json).

Đợt boss/animation/VFX và cân bằng có [báo cáo kiểm chứng ngày 25/11/2026](Tools/UnityHarness/verification-2026-11-25.json), kèm đường dẫn build/log, ảnh thực tế, các lượt FAIL đã sửa và giới hạn kiểm thử.

## Chạy dự án

1. Dùng Unity **6000.3.21f1** theo `ProjectSettings/ProjectVersion.txt`.
2. Mở project bằng Unity Hub, chờ import package/Addressables.
3. Mở scene `Assets/RGame/RoguelikeKit/Scenes/Initialization.unity` và nhấn Play.

Dự án dùng Built-in Render Pipeline và Unity Input System. Giữ các file `.meta` khi sao chép asset.

## Kiểm thử và build

Xem [Tools/UnityHarness/README.md](Tools/UnityHarness/README.md). Harness hỗ trợ build Windows x64, profile QA riêng và các ca UI, kỹ năng, animation, nhận đòn, ba chế độ, nền 10 màn, âm thanh và lưu cài đặt.

```powershell
./Tools/UnityHarness/harness.ps1 doctor
./Tools/UnityHarness/harness.ps1 verify --suites all
```

Build, log, cache Unity và bộ thực thi CLI tải về không được đưa vào Git. Kết quả từng lần chạy được lưu cục bộ trong `Tools/UnityHarness/artifacts/`; không coi việc có harness là chứng minh mọi ca đã đạt.

## Nguồn gốc và giấy phép

- Mã nền: [Survivors Roguelike Kit — Roo-Roo-Roo](https://github.com/Roo-Roo-Roo/survivors-roguelike-kit).
- Giữ nguyên [giấy phép MIT và bản quyền gốc](LICENSE); thông tin dự án gốc được lưu ở [UPSTREAM_README.md](UPSTREAM_README.md).
- Phần mở rộng Shadowgrove gồm nội dung nhân vật/màn/chế độ, bản địa hóa, giao diện, tài nguyên hình ảnh mới và hệ thống QA. Một số hình ảnh được tạo bằng công cụ AI và xử lý để tích hợp vào Unity.
- Plug-in, font và các thành phần bên thứ ba giữ giấy phép riêng. DOTween và các tài nguyên nền kế thừa từ kit không được tuyên bố là do tác giả phiên bản này tự viết.

Đây là dự án phát triển dựa trên một bộ kit mã nguồn mở; việc sở hữu repo này không thay đổi tác giả hay bản quyền của các thành phần gốc.

## Giới hạn đã biết

- Khi rất đông quái dồn một chỗ, FPS có thể giảm mạnh; chưa tối ưu stress test dài hạn.
- Nâng cấp vĩnh viễn dùng mức cộng và giá mới, giữ số bậc đã mua và vàng hiện có. Việc mở lại menu không cộng lặp chỉ số. Xác thực nội dung save bất thường chưa thuộc đợt sửa này.
- Bộ kiểm thử UI dùng raycast và sự kiện pointer thật của Unity; kiểm tra chuột Windows ở ô Sức mạnh chưa hoàn tất do người dùng dừng Computer Use.
