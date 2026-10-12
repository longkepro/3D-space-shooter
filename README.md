# 3D Sci-Fi Virtual Rail Shooter (Star Fox 64 Arcade Engine)

```text
 _____ ____    ____  _                 _            _____ _   _ ____  _     _____ ____ ____  
|___ /|  _ \  / ___|| |__   ___   ___ | |_ ___ _ __| ____| \ | |  _ \| |   | ____/ ___/ ___| 
  |_ \| | | | \___ \| '_ \ / _ \ / _ \| __/ _ \ '__|  _| |  \| | | | | |   |  _| \___ \___ \ 
 ___) | |_| |  ___) | | | | (_) | (_) | ||  __/ |  | |___| |\  | |_| | |___| |___ ___) |__) |
|____/|____/  |____/|_| |_|\___/ \___/ \__\___|_|  |_____|_| \_|____/|_____|_____|____/____/ 

                             Fly. Fight. Graze. Survive.
```

[![Unity 6000.0.28f1](https://img.shields.io/badge/Unity-6000.0.28f1%20URP-black.svg?style=flat&logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/MSBuild-0%20Errors-brightgreen.svg)]()
[![GC Allocation](https://img.shields.io/badge/GameLoop%20Alloc-0%20GC-orange.svg)]()

Tựa game bắn súng đường ray không gian 3D thế hệ mới (**Virtual Rail Arcade Shooter**), tái hiện chuẩn xác 100% cảm giác điều khiển cơ học kinh điển của **Star Fox 64 (N64 US Rev 1.1)** kết hợp cùng cấu trúc **Sinh tồn Vô hạn (Endless Mode)** và Động cơ Tính điểm Chuyên sâu (**Deep Scoring Engine**).

---

## 🌟 TÀI LIỆU NGUỒN CHÂN LÝ (SOURCE OF TRUTH)

* [**`MASTER_ROADMAP_TRACKER.md`**](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) — Bảng điều khiển tiến độ tổng thể, quản lý phân hệ và nhật ký nghiệm thu.
* [**`ENDLESS_SCORING_SPECIFICATION.md`**](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md) — Đặc tả kỹ thuật hành lang vô hạn, giải thuật DDA răng cưa và động cơ tính điểm Multiplier 32x / Graze.
* [**`LEVEL_DESIGN_SPECIFICATION.md`**](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md) — Đặc tả thiết kế màn chơi, đường cong nhịp độ và cấu trúc quần xã chuyển tiếp.
* [**`STAGE_1_DESIGN_DOCUMENT.md`**](file:///d:/3D-Shooter-master/STAGE_1_DESIGN_DOCUMENT.md) — Kịch bản chi tiết Biome khởi đầu: *Operation Meteor Breach* (Cycle 1 Onboarding).
* [**`STARFOX64_AIMING_PLAN.md`**](file:///d:/3D-Shooter-master/STARFOX64_AIMING_PLAN.md) — Hệ thống ngắm bắn 4 giai đoạn, Dual Reticle (Near/Far) và bắn tụ lực Charge Shot.
* [**`STARFOX64_ENEMY_SPAWN_PLAN.md`**](file:///d:/3D-Shooter-master/STARFOX64_ENEMY_SPAWN_PLAN.md) — Quy chuẩn spawn quái 3D Bézier và thiên thạch Perlin.
* [**`AGENTS.md`** / **`GEMINI.md`**](file:///d:/3D-Shooter-master/AGENTS.md) — Bộ Quy Tắc Phát Triển Dự Án (Hiến pháp 0 GC, Unidirectional Flow, An toàn 3 tầng).

---

## 🚀 TRỤ CỘT GAMEPLAY CỐT LÕI (CORE PILLARS)

### 1. Hành Lang Đường Ray Vô Hạn (Endless Procedural Rail)
* **Khối Ray Ghép Nối (Hermite Spline Chunks):** Đường ray liên tục sinh theo từng đoạn $400 - 600\text{m}$ ($8 - 12\text{s}$ bay). Duy trì bộ đệm tối thiểu 3 Chunks và bảo đảm tính trơn hình học $C^2$ tại hai đầu mút ghép.
* **Bảo Chứng Hình Học Khả Thi (Golden Path Raycast):** Thuật toán quét trước không gian 3D loại bỏ $100\%$ tình huống "bức tường chết" (Unwinnable scenarios).
* **Tái Định Vị Thế Giới (World Origin Shifting):** Tự động dịch chuyển toàn bộ thực thể ngược về gốc $(0, 0, 0)$ mỗi chu kỳ $4,000\text{m}$ để triệt tiêu hoàn toàn hiện tượng rung giật số thực (Floating-point jitter).

### 2. Bộ Điều Chỉnh Độ Khó Động (Dynamic Difficulty Adjustment - DDA)
* **Chu Kỳ Răng Cưa 3 Pha (Tri-Phase Loop - $60 - 80\text{s}$ / $2.5 - 3.5\text{km}$):**
  1. *Pha Leo Dốc (Ramp $60\%$):* Áp lực tăng dần, mật độ đạn, tốc độ tiếp cận của quái và thiên thạch dâng cao tuyến tính.
  2. *Pha Đỉnh Điểm (Climax Encounter $25\%$):* Hành lang bóp hẹp, kích hoạt trận chiến **Đột kích Trùm (Apex Boss Incursion)** hoặc bầy tiêm kích bủa vây nghẹt thở.
  3. *Pha Thư Giãn & Thu Hoạch (Breather / Restock $15\%$):* Mở rộng không gian an toàn, xuất hiện dày đặc vòng hồi máu, vòng khiên và nâng cấp hỏa lực.
* **Ngân Sách Mối Nguy (Threat Budget):** Quản lý điểm đe dọa đồng thời trên khung hình, ngăn chặn tình trạng quá tải không thể kiểm soát.

### 3. Cơ Học Bay Arwing Chuẩn N64 (Deterministic Flight Dynamics)
* **0 Rigidbody Physics:** Chuyển động là máy trạng thái tất định (Deterministic State Machine), loại bỏ hoàn toàn quán tính trôi dạt ngẫu nhiên.
* **Thao Tác Cơ Động Cao Cấp:**
  * **Tăng Tốc (Boost):** $V_z$ tăng $+50\%$, nén tiêu cự Camera FOV tạo cảm giác lao vút.
  * **Phanh Hãm (Air Brake):** $V_z$ giảm $-50\%$, mở rộng cửa sổ thời gian để nhắm bắn tỉa các cụm mục tiêu dày đặc.
  * **Lộn Cánh (Barrel Roll Deflection):** Nhấp đúp phím lộn vòng tạo từ trường phản xạ hất văng đạn plasma của địch.
  * **Nghiêng Cánh (Banking Tilt):** Lách thân qua các khe nứt thiên thạch hẹp mà không đổi hướng bay chính.

### 4. Hệ Thống Ngắm Bắn Kép & Hỏa Lực Đa Dạng
* **Dual-Reticle Predictive Aiming:**
  * Hồng tâm gần (*Near Reticle* tại $Z = +30\text{m}$) và hồng tâm xa (*Far Reticle* tại $Z = +120\text{m}$).
  * Cơ chế ngắm đón đầu (Lead Targeting) tự động bù trừ vận tốc bay của đạn.
* **Cơ Chế Bắn Tụ Lực (Charge Shot):**
  * Giữ nút bắn để nạp năng lượng cao tần và tự động khóa mục tiêu (Lock-on).
  * Nhả đạn phóng cầu năng lượng tầm nhiệt nổ lan bán kính lớn, kích hoạt bội số điểm số diệt cụm.
* **Vũ Khí Hủy Diệt Smart Bomb:** Phóng bom dọn sạch toàn bộ đạn đạo và tiêu diệt phi đội quái trên màn hình.

### 5. Hệ Thống Khiên Lục Giác & HUD Khoang Lái Hiện Đại
* **Lớp Khiên Năng Lượng Độc Lập ([`VirtualRailHexShield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHexShield.cs)):**
  * Sở hữu quỹ máu riêng ($300\text{ HP}$), hấp thụ $100\%$ sát thương đạn và va quẹt thiên thạch.
  * Hiệu ứng chớp sáng Neon White khi bị bắn trúng, tự động phục hồi sau thời gian trễ không bị tổn thương.
  * Khi cạn kiệt, khiên sụp đổ và tàu nhận sát thương trực tiếp vào Thân Tàu (Hull Integrity).
* **Giao Diện Dual-Gauge HUD ([`ModernCockpitHUD.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/ModernCockpitHUD.cs)):**
  * Đồng hồ hiển thị song song thanh máu Thân Tàu (Hull - Xanh lục) và thanh Khiên Năng Lượng (Shield - Xanh lam), đồng bộ mượt mà ở `LateUpdate`.

### 6. Động Cơ Tính Điểm Sâu (Deep Scoring Engine)
* **Dynamic Combo Multiplier ($1\times \to 32\times$):**
  * Hệ số nhân tăng cấp qua từng mốc: $1\times \to 2\times \to 4\times \to 8\times \to 16\times \to \mathbf{32\times}$.
  * Duy trì bằng thanh năng lượng suy giảm trong $3.5\text{s}$.
* **Cơ Chế Lướt Cận Kề (Graze / Near-Miss):**
  * Lách tàu sát sạt đường đạn (khoảng cách $0.5\text{m} - 1.8\text{m}$) mà không nhận sát thương.
  * Thưởng ngay $+500\text{ Điểm} \times \mathbf{M}$, hồi phục $30\%$ thanh Combo Decay.
* **Cổng Thách Thức Rủi Ro (Risk Gates):** Khe nứt hẹp nguy hiểm chứa huy hiệu điểm số cực đại và khóa đóng băng Multiplier trong $10\text{s}$.

### 7. Trùm Đột Kích Đỉnh Điểm (Apex Climax Boss: Corvette F3 "Crimson Titan")
Chiến hạm tuần duyên hạng nặng của Liên bang xuất hiện tại đỉnh cao trào của các chu kỳ sinh tồn:
* **Pha 1 (Bảo Vệ Khiên & Hỏa Lực Sườn):** Khiên năng lượng tiền tiêu che chắn lõi, 2 tháp pháo cánh xả đạn plasma và phóng tên lửa tầm nhiệt.
* **Pha 2 (Hẻm Lò Phản Ứng Lộ Diện):** Khiên sụp đổ, khe tản nhiệt nhiệt hạch bộc lộ hiệu ứng dung nham plasma rực sáng ([`VirtualRail_ThermalReactorTrench.shader`](file:///d:/3D-Shooter-master/Assets/Shaders/VirtualRail_ThermalReactorTrench.shader)), bắn ngắt nhịp xung nạp năng lượng của Boss.
* **Pha 3 (Quá Tải Lõi & Cuồng Nộ):** Lõi chuyển sang trạng thái quá nhiệt đỏ rực, quét tia Hyper Laser cực đại chiếm $60\%$ màn hình. Tiêu diệt Boss thưởng $+50,000\text{ Điểm}$ và mở cổng Warp sang quần xã tiếp theo.

---

## 🎮 ĐIỀU KHIỂN (CONTROLS)

| Thao Tác | Phím / Chuột | Cần Điều Khiển (Gamepad) | Mô Tả |
| :--- | :---: | :---: | :--- |
| **Di chuyển Tàu & Ngắm** | `W / A / S / D` hoặc Chuột | Trục Analog Trái (`L-Stick`) | Di chuyển Arwing trong khung hình và rê hồng tâm |
| **Bắn Thường / Tụ Lực** | Chuột Trái hoặc `Space` | Nút `A` (Xbox) / `Cross` (PS) | Nhấp để bắn tỉa, giữ để tụ lực Lock-on Charge Shot |
| **Phóng Smart Bomb** | Chuột Phải hoặc `F` | Nút `B` (Xbox) / `Circle` (PS) | Kích nổ bom giải tỏa áp lực toàn màn hình |
| **Tăng Tốc (Boost)** | `Shift` Trái | Nút `X` (Xbox) / `Square` (PS) | Tăng tốc độ bay vượt chướng ngại vật |
| **Phanh Hãm (Brake)** | `Ctrl` Trái | Nút `Y` (Xbox) / `Triangle` (PS) | Hãm tốc độ để ngắm bắn mục tiêu dày đặc |
| **Lộn Cánh (Barrel Roll)** | Nhấp đúp `Q` hoặc `E` | Nhấp đúp `LB` hoặc `RB` | Tạo từ trường phản xạ hất văng bão đạn địch |
| **Nghiêng Cánh (Banking)** | Giữ `Q` hoặc `E` | Giữ `LB` hoặc `RB` | Nghiêng thân tàu $90^\circ$ lách qua khe hẹp |
| **Tạm Dừng (Pause)** | `ESC` hoặc `P` | Nút `Start` / `Options` | Tạm dừng trò chơi và mở Menu Cài đặt |

---

## 🛠️ TIÊU CHUẨN KỸ THUẬT & KIẾN TRÚC HỆ THỐNG

Dự án áp dụng các nguyên tắc kỹ thuật nghiêm ngặt theo [`AGENTS.md`](file:///d:/3D-Shooter-master/AGENTS.md):
1. **0 GC Allocation trong Game Loop:** Tuyệt đối không gọi `new`, `Instantiate`, `Destroy`, LINQ, Boxing hoặc ghép chuỗi string trong `Update`, `LateUpdate`, `FixedUpdate`.
2. **Kiến Trúc Luồng Dữ Liệu 1 Chiều (Unidirectional Data Flow):**
   ```text
   Input Reader ──► Gameplay State ──► Flight & Combat Systems ──► Presentation (Visual/Camera/UI)
   ```
3. **Đồng Bộ Camera & Tọa Độ Chiếu UI:** Mọi tính toán vị trí hồng tâm, Crosshair, HUD Marker được xử lý tại `LateUpdate` sau khi Camera đã định vị xong trong frame, triệt tiêu hoàn toàn rung giật 1 frame (1-frame jitter).
4. **Quy Tắc An Toàn 3 Tầng Bảo Lưu:**
   * Tầng 1: Git Checkpoint nhánh trước mọi thay đổi cấu trúc.
   * Tầng 2: Giữ nguyên vẹn 100% Prefab gốc (`Player Ship (1).prefab`), mở rộng qua Prefab độc lập mới.
   * Tầng 3: Cung cấp cặp MenuItem 1-click đối xứng `[ON]` / `[OFF - Legacy]` trong Unity Editor.
5. **Nghiệm Thu Biên Dịch Bắt Buộc:** Mọi cập nhật đều phải vượt qua kiểm tra biên dịch MSBuild trên PowerShell với kết quả **`0 Errors`** cho cả `Assembly-CSharp.csproj` và `Assembly-CSharp-Editor.csproj`.

---

## 📂 CẤU TRÚC THƯ MỤC DỰ ÁN

```text
Assets/
├── Materials/                  # Material đạn laser, khiên hex, lò phản ứng plasma
├── Prefabs/                    # Tàu Arwing, quái vật, Boss Corvette F3, chướng ngại vật
├── Scenes/
│   ├── Game.unity              # Màn chơi chính vận hành hệ thống Virtual Rail Endless
│   └── Test.unity              # Scene thử nghiệm cơ học
├── Scripts/
│   ├── VirtualRail/            # Lõi hệ thống Virtual Rail Shooter
│   │   ├── VirtualRailShip.cs            # Máy trạng thái bay Arwing tất định
│   │   ├── VirtualRailWeapon.cs          # Hệ thống vũ khí, súng đơn/đôi, tụ lực
│   │   ├── VirtualRailHexShield.cs       # Khiên lục giác 300 HP, Neon White hit-flash
│   │   ├── ModernCockpitHUD.cs           # Dual-gauge HUD (Hull & Shield)
│   │   ├── VirtualRailEndlessDirector.cs # Bộ điều phối nhịp độ Endless & DDA 3 pha
│   │   ├── VirtualRailWaveSpawner.cs     # Bộ sinh quái theo đội hình Bézier
│   │   ├── VirtualRailAsteroidSpawner.cs # Bộ sinh dải thiên thạch ngẫu nhiên có kiểm soát
│   │   ├── BossLifecycleFSM.cs           # Máy trạng thái 3 pha của Boss Corvette F3
│   │   ├── AudioManager.cs               # Quản lý âm thanh tập trung, 0 GC Pool, Dual-BGM Shuffle
│   │   └── ...
│   └── ...
├── Shaders/
│   ├── VirtualRail_HexShield.shader      # Shader khiên lưới lục giác
│   ├── VirtualRail_ThermalReactorTrench.shader # Shader plasma lõi phản ứng pha 2 Boss
│   └── ...
└── ScriptableObjects/          # Bộ dữ liệu đội hình bay, kịch bản biome, chỉ số cân bằng
```

---

## 📜 BẢN QUYỀN & GIẤY PHÉP

Dự án phát hành theo giấy phép [MIT License](LICENSE).  
Phát triển và tối ưu hóa theo tiêu chuẩn hệ thống game Arcade Star Fox 64 cho Unity Engine.
