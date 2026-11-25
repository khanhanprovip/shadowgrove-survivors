# Shadowgrove Unity harness

Chạy tại thư mục gốc dự án. Yêu cầu Windows, Python 3.10+, Git và đúng Editor trong `ProjectVersion.txt`. Không cần tài khoản AI/API mới.

```powershell
.\Tools\UnityHarness\harness.ps1 doctor
.\Tools\UnityHarness\harness.ps1 probe
.\Tools\UnityHarness\harness.ps1 verify --suites all
```

- `doctor`: kiểm tra Editor, checksum Unity CLI và phiên bản Pipeline trong manifest.
- `setup`: tải CLI đã ghim và xác minh SHA256 nếu máy chưa có. Không sửa PATH/cấu hình Codex toàn cục. Pipeline đã khai báo trong Packages; Unity sẽ resolve khi mở dự án.
- `probe`: yêu cầu đóng Editor đang mở; tự mở một Editor riêng, đọc console, tạo/sửa/lưu/mở lại prefab 2D và scene thử, chạy Initialization, xác nhận frame tăng và menu được nạp, kiểm tra console rồi đóng Editor. Fixture được xóa qua AssetDatabase; không thêm vào build.
- `build`: build Windows x64 từ mã nguồn hiện tại qua `DATN.DatnBuild.BuildWindows64`.
- `backgrounds` trong `--suites`: vào đủ 10 màn, kiểm tra nền riêng, kích thước phủ bản đồ, thứ tự hiển thị, không thêm collider, di chuyển và tạm dừng. Ảnh từng màn được lưu cùng log.
- `animations` kiểm tra pixel alpha thực sự render cho mọi frame Idle/Run/Attack, rồi chạy tự tấn công 4 giây với va chạm nhận đòn được cô lập. Sprite khác null nhưng toàn bộ pixel trong suốt vẫn bị báo FAIL.
- `verify`: build mới rồi chạy các suite đã chọn. Mặc định `ui,skills,skill-lifetime,audio`; `all` thêm menus, survival, killtarget, endless, animations, hit-feedback và settings-write/read. `hit-feedback` đo nháy đỏ nhẹ, giữ opacity, giới hạn xung sáng và giữ nguyên sát thương trên cả ba nhân vật.
- `test`: chạy bản build có sẵn, ví dụ `harness.ps1 test --exe F:\path\ShadowgroveSurvivors.exe --suites ui,skills`. Đây là kiểm chứng binary đó; cần `verify` để gắn với mã nguồn hiện tại.
- `legacy`: chạy ca Editor cũ: `harness.ps1 legacy --case Smoke45`. Hỗ trợ Flow65, Full100, Lose60, KillTargetLevel. Full100 thực tế chạy 130 giây; KillTargetLevel 160 giây. Các ca được bọc sẵn, vẫn cần đọc kết quả mỗi lượt; không coi hỗ trợ lệnh là đã PASS.

`--timeout 300` là giới hạn mỗi ca; `--build-timeout 1800` là giới hạn build. Mã thoát harness là 0 khi đạt, 1 khi lỗi. Các ca chạy tuần tự; settings-write/read dùng chung profile riêng cho lượt đó. Các profile khác tách biệt cả JSON và khóa ngôn ngữ QA. Harness không xóa save thật hoặc reset Git.

## Điều khiển Editor đang mở

CLI chính thức có sẵn ở `.tools/unity/1.0.1/unity.exe`. Pipeline hoạt động cục bộ trên Editor được chỉ định. Dùng Python trực tiếp cho lệnh có dấu phân cách `--`, JSON hoặc cờ trùng tên với harness, để giữ chính xác tham số:

```powershell
python Tools/UnityHarness/harness.py live -- console status
python Tools/UnityHarness/harness.py live --query script
python Tools/UnityHarness/harness.py live -- script eval 'return UnityEngine.Application.unityVersion;'
python Tools/UnityHarness/harness.py live -- script run --file Tools/UnityHarness/VerifyAuthoring.cs --entry VerifyAuthoring.Run
```

`live` chờ Editor ready nhưng không tự mở hoặc đóng Editor. `--query` nằm sau `live` được chuyển tới `unity command`. Đọc skill `.agents/skills/using-unity` và reference unity-cli/unity-pipeline trước khi dùng API; khám phá schema của Editor thay vì đoán tham số. Code authoring dài để trong Tools, chạy qua `script run`; sửa scene/prefab bằng Unity API.

Trước sửa: lưu cursor `console status`. Sau sửa: `console get -- --since <cursor> --level error`. Không clear console. Trước Play Mode do agent khởi động: bật `editor set_auto_tick` và `editor.playmode set_run_in_background`; sau start chờ status ready; dùng `job wait` với Time.frameCount/op changed để chứng minh runtime tiến triển. Sau stop trả hai override về false. Không tiếp quản phiên Play Mode đang chạy của người dùng.

## Bằng chứng và giới hạn

Mỗi lệnh tạo `Tools/UnityHarness/artifacts/<timestamp>-<action>-<pid>/` chứa `summary.json`, dữ liệu trả về từ CLI, log tiến trình và mã thoát. Build/test còn có Git HEAD/status, SHA256 của các file nguồn hiện tại (kể cả thay đổi chưa commit), build mới, log Unity/Player và kết quả từng ca. Build ghi SHA256 exe và Assembly-CSharp.dll. Đối chiếu assembly khi đánh giá nguồn; riêng hash exe không bao phủ toàn bộ dữ liệu build.

Cổng kiểm thử yêu cầu: ít nhất một PASS, không FAIL, có đúng một tổng lỗi runtime bằng 0, không exception/crash trong log thô, tiến trình thoát 0, không timeout. Báo cáo thiếu hoặc dở dang là FAIL. Có khóa ngăn hai thao tác harness đồng thời; build/probe/legacy từ chối khi Unity đang giữ khóa dự án. Harness chỉ kết thúc tiến trình chính nó tạo khi quá hạn.

```powershell
python -m unittest discover -s Tools/UnityHarness -p test_harness.py -v
```

Các test trên kiểm tra cổng kết quả và timeout thật; không thay thế kiểm thử game. Các suite Windows kiểm tra hành vi và cấu trúc UI. Ảnh chụp khi cửa sổ ẩn có thể đen; không dùng chúng làm bằng chứng giao diện đã đẹp. Âm thanh được kiểm tra wiring/phát nguồn; cảm nhận âm lượng và hình ảnh vẫn cần duyệt trực tiếp trong cửa sổ game. Không đặt lời gọi mạng/AI trả phí vào harness. Coplay MCP và Autopilot chưa bật; hỏi chủ dự án trước khi thêm dịch vụ phát sinh phí.

Để tắt tiếng riêng các cửa sổ Shadowgrove đang mở trên Windows, chạy `./Tools/UnityHarness/game-audio.ps1`. Có thể dùng `-WatchSeconds 600` để mute cả các Player được mở sau đó trong 10 phút; đầu ra Windows và ứng dụng khác giữ nguyên. Cách này mute tại mixer, không sửa save game và vẫn cho phép suite audio đo tín hiệu bên trong Unity. Bật tiếng trở lại bằng Volume Mixer của Windows.

## Công cụ đã ghim

Unity CLI 1.0.1 và Pipeline 1.0.0; URL/SHA256 nằm trong toolchain.lock.json. Editor luôn lấy phiên bản từ ProjectVersion, mặc định tìm dưới F:/unity; đổi thư mục cài bằng biến UNITY_HARNESS_EDITOR_ROOT. Skills Unity chính thức cùng năm skill C#/ScriptableObject/Input/Tilemap/build được cài trong `.agents/skills`; xem từng SKILL.md để dùng theo ngữ cảnh. Không tự nâng Unity hoặc công cụ trong lượt sửa lỗi.

## Boss, cân bằng và nhịp sinh quái

```powershell
./Tools/UnityHarness/harness.ps1 verify --suites balance,pause-skills,spawn-pacing,bosses --timeout 750
```

- `balance`: giữ các bậc đã mua, mở menu nhiều lần không cộng lặp, vào trận nhận đúng bonus, XP lớn qua đúng các ngưỡng, ngân hàng vàng không cộng hai lần, mua tiếp và đọc lại save. Chỉ sửa profile QA của lượt chạy.
- `pause-skills`: cả Rowan/Lyra/Kael ở VI/EN, danh sách 12 kỹ năng, cuộn bằng sự kiện wheel qua raycast thật; thời gian và chỉ số không đổi khi đọc.
- `spawn-pacing`: đo hai cửa sổ 30 giây liên tiếp trong Endless; đầu trận ít quái hơn cửa sổ sau; lịch boss không thay đổi. Bot chỉ có đệm HP và tắt tầm vũ khí trong ca này để đếm lượt sinh không bị lẫn với lượt hạ quái.
- `enemy-cap`: chạy tới trần 40 quái thật, thử thêm 200 yêu cầu sinh, kiểm tra không sinh vượt trần, không quá 2 quái mới/frame, đo FPS ở tải đầy rồi hạ một quái và xác nhận có lượt sinh bù. Các ca Survival/KillTarget/Endless cũng ghi nhận số quái lớn nhất và kiểm tra trần chung.
- `bosses`: mốc boss đầu được chạy bằng thời gian mô phỏng thật đến 300 giây; các mốc sau dùng bước nhảy đồng hồ QA, không tuyên bố đã chơi tay 30 phút. Kiểm tra 6 lượt (300 → 1.800 HP, loại thứ 6 quay về loại đầu), 25 chiêu ngẫu nhiên, nhịp chuẩn bị không có cảnh báo sàn, sát thương/vị trí an toàn, banner tên màu không chặn input, animation render thực tế, tên màu VI/EN, tỷ lệ pixel nhìn thấy, dọn quái, tạm ngưng/khôi phục wave, +5 cấp/+500 vàng và không cộng dồn boss. HP bot được tăng trong bộ nhớ của profile QA để vượt mốc năm phút; HP boss sản phẩm được chụp trước khi tăng độ bền riêng cho phần quan sát đủ chiêu. EXP còn trên sân được thu hồi trong ca này để cô lập phần thưởng.

`Tools/Art/BossAuthoring.cs` tạo/import sprite, prefab, vai trò và cấu hình cân bằng bằng Unity API; atlas gốc ở `Tools/Art/Sources/BossAtlas.png`. `Tools/Art/BossFrameAnimationAuthoring.cs` là pipeline hiện tại: cắt 10 sheet Locomotion/Combat 4×4 và giữ Hit/Death từ năm sheet gốc bằng Sprite Editor Data Provider, căn PPU/pivot theo alpha của Idle0, tạo 30 clip và năm Animator Controller, 36 sprite/boss, nhịp giữ khung riêng cho mỗi boss, hit đỏ nhẹ và dissolve riêng. Bộ shader/mesh cũ được giữ để đối chiếu, không phải animation đang dùng. `Tools/Art/BossWire.cs` nối nguồn tiến độ đã mua, sự kiện sinh nhân vật và dịch vụ lưu vàng, rồi kiểm tra tham chiếu sau khi mở lại scene. Boss Resources không giữ bản sao pool/global/save; director truyền đúng context của trận khi lấy boss ra khỏi pool.


## Survivor starter weapons and Survival boundary

`combat-pickups` verifies the three actual survivor profiles (Rowan cleave 2.2, Lyra orb 7, Kael arrow 10.5), radial target gating, initialized enemy stats, real damage/collisions, projectile range expiry, and gold chests. `ui` raycasts all 14 permanent-upgrade tiles and checks that pointer selection opens the corresponding description in VI/EN. It does not purchase upgrades.

`survival-boundary` simulates a 310ms frame overshoot on a real round and checks that the win clock is exactly the configured deadline. Pair it with `survival` for a full 120-second match. Evidence from 2026-11-23 is summarized in `verification-2026-11-23.json`; raw local logs/builds remain excluded from Git.

Native boss animation QA kiểm tra đủ 180 sprite qua GPU, sáu clip mỗi boss qua 41 mẫu thời gian/clip (không giả định keyframe cách đều), đổi sprite/pixel thật khi chạy AI và đứng yên, hit/death, đóng băng khi pause. Phần đo hình ảnh tạm cô lập collider nhận đòn của boss để vũ khí từ thưởng trước không làm nhiễu HP; các kiểm tra nhận đòn/thưởng gọi EnemyHit thật và khôi phục collider trước khi hạ boss. Preview qua `Tools/Art/BossRuntimeInspect.cs` + `BossAnimationPreview.cs` dùng clip thật trong Play Mode, rồi `assemble_boss_preview.py` ghép GIF; đây là preview các tư thế, không phải video chơi một trận.

VFX được tạo bởi `BossSkillVfx` và `BossCastingVfx`: mỗi boss có motif/màu riêng, buffer cố định và material dùng chung. Chỉ đòn đã kích hoạt mới vẽ trên sàn; windup chỉ có ánh phép trên thân/tay. Suite `bosses` yêu cầu đủ VFX của 25 chiêu, kiểm tra toàn bộ line con ẩn trước khi kích hoạt và vị trí hiệu ứng đóng băng khi pause. Boss khóa hướng trái/phải theo hướng ngắm khi bắt đầu cast; hướng gần thẳng đứng giữ hướng ngang trước đó. Chỉ lật SpriteRenderer, không lật tên, thanh máu hay collider. QA đưa mục tiêu sang trái/phải/trên và chạy vòng qua giữa cast để kiểm tra khóa hướng.

Phần quan sát chết/thưởng dùng vòng chờ theo thời gian thực và lấy mẫu alpha trước khi trả pool, đồng thời xử lý đủ năm bảng chọn. Không chờ bằng `WaitForSeconds` qua thời điểm bảng thưởng làm `timeScale=0`; thiếu báo cáo hoặc timeout luôn bị coi là FAIL.

Đối với Hit dài 0,18 giây, QA lấy mẫu sprite/alpha mỗi frame trước khi ghi PNG. Chỉ sau khi thấy khung hình thay đổi mới đóng băng pose để chụp; encode ảnh 1080p không được tính vào cửa sổ quan sát animation.

QA còn kích hoạt/hủy trực tiếp SlowBuff, FreezingBuff và BurnBuff trên mỗi boss: shader mới không có `_TimeSpeed`, nên hai buff đầu chỉ đọc/ghi thuộc tính shader được hỗ trợ; hiệu chỉnh Speed vẫn giữ nguyên. Shader boss hỗ trợ `_FlameRedBlend` để màu cháy hiện đúng và trở về trung tính khi hết hiệu ứng. Kiểm tra bao gồm alpha render thật và hoàn trả Speed/shadow offset; đây không phải thay đổi quy tắc khống chế hoặc thời gian cast của boss.
