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


## Survivor starter weapons and Survival boundary

`combat-pickups` verifies the three actual survivor profiles (Rowan cleave 2.2, Lyra orb 7, Kael arrow 10.5), radial target gating, initialized enemy stats, real damage/collisions, projectile range expiry, and gold chests. `ui` raycasts all 14 permanent-upgrade tiles and checks that pointer selection opens the corresponding description in VI/EN. It does not purchase upgrades.

`survival-boundary` simulates a 310ms frame overshoot on a real round and checks that the win clock is exactly the configured deadline. Pair it with `survival` for a full 120-second match. Evidence from 2026-11-23 is summarized in `verification-2026-11-23.json`; raw local logs/builds remain excluded from Git.
