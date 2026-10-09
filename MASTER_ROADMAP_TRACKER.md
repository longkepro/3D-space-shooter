# KẾ HOẠCH TỔNG THỂ & THEO DÕI TIẾN ĐỘ DỰ ÁN (MASTER ROADMAP & PROGRESS TRACKER)
> **Dự án:** 3D Sci-Fi Virtual Rail Shooter (Chuẩn Star Fox 64 Arcade)  
> **Tài liệu nguồn chân lý:** [`STAGE_1_DESIGN_DOCUMENT.md`](file:///d:/3D-Shooter-master/STAGE_1_DESIGN_DOCUMENT.md) • [`LEVEL_DESIGN_SPECIFICATION.md`](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md) • [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md) • [`STARFOX64_AIMING_PLAN.md`](file:///d:/3D-Shooter-master/STARFOX64_AIMING_PLAN.md) • [`STARFOX64_ENEMY_SPAWN_PLAN.md`](file:///d:/3D-Shooter-master/STARFOX64_ENEMY_SPAWN_PLAN.md)  
> **Quy chuẩn hiệu lực:** Tuân thủ 100% Bộ Quy Tắc Phát Triển Dự Án ([`AGENTS.md`](file:///d:/3D-Shooter-master/AGENTS.md) / [`GEMINI.md`](file:///d:/3D-Shooter-master/GEMINI.md)) — An toàn 3 tầng, 0 GC Allocation, Kiểm thử biên dịch `0 Errors`.

---

## I. BẢNG ĐIỀU KHIỂN TIẾN ĐỘ TỔNG THỂ (EXECUTIVE DASHBOARD)

```text
TIẾN ĐỘ TOÀN DỰ ÁN HƯỚNG TỚI BẢN HOÀN CHỈNH (PRODUCTION-READY):
[█████████████████████░░░░░░░░░░░] 67% HOÀN TẤT
```

| Phân Hệ / Giai Đoạn | Trọng Tâm Phát Triển | Trạng Thái | Số Task | Tỉ Lệ | Ghi Chú Nghiệm Thu |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Giai đoạn 0: Cốt Lõi (Đã Có)** | Bay, Vũ khí, Đội hình, Director, Điểm số | **HOÀN TẤT** | 12/12 | $100\%$ | Biên dịch 0 Errors, 0 GC loop |
| **Giai đoạn 1: Âm Thanh & Rung Chấn** | AudioManager, SFX, BGM, Screen Shake | **HOÀN TẤT** | 4/4 | $100\%$ | 0 GC Pool, Procedural SFX + Custom BGM Slots |
| **Giai đoạn 2: Trận Đấu Trùm** | Mini-Boss Cruiser, Boss Gorgon Core, Boss UI | **CHỜ DUYỆT** | 0/4 | $0\%$ | 3 Pha chiến đấu theo LDS v1.0.0 |
| **Giai đoạn 3: Vòng Lặp & UI/UX** | Menu, Pause, Settings, Debriefing S-Rank | **CHỜ DUYỆT** | 0/6 | $0\%$ | Hoàn chỉnh Game Loop thương mại |
| **Giai đoạn 4: Mở Rộng & Endless** | Kịch bản Stage 2/3, Chế độ Endless TDD v2.0.0 | **CHỜ DUYỆT** | 0/4 | $0\%$ | Procedural Spline + DDA 3 pha |

---

## II. CƠ CHẾ THEO DÕI & QUY ƯỚC TRẠNG THÁI (TRACKING PROTOCOL)

Để người dùng và AI theo dõi chính xác từng bước, các đầu việc tuân theo quy ước ký hiệu:
* `[ ]` **Chờ thực hiện (Pending):** Đã lập kế hoạch, chưa có lệnh phê duyệt thực thi từ người dùng.
* `[-]` **Đang triển khai (In Progress):** Đã được phê duyệt, đang tạo mã nguồn, asset và kiểm thử.
* `[x]` **Đã hoàn thành & Nghiệm thu (Completed):** Đã chạy MSBuild đạt **`0 Errors`**, kiểm thử thực tế đạt yêu cầu và đã cập nhật nhật ký.

> [!IMPORTANT]
> **QUY TRÌNH KÍCH HOẠT THAY ĐỔI THEO ĐIỀU 1 BỘ QUY TẮC:**
> 1. AI không tự ý sửa code hoặc tạo asset khi người dùng chưa ra lệnh trực tiếp (*"thực hiện Giai đoạn 1"*, *"tiến hành Task 1.1"*, v.v.).
> 2. Mỗi thay đổi đều phải tạo điểm checkpoint, bảo lưu prefab gốc và cung cấp MenuItem Editor hoàn nguyên (Undo).
> 3. Sau khi hoàn thành một task, AI cập nhật dấu `[x]` và ghi nhật ký vào Mục VII của tài liệu này.

---

## III. GIAI ĐOẠN 1: HỆ THỐNG ÂM THANH & PHẢN HỒI XÚC GIÁC (AUDIO & POLISH CORE)
> **Mục tiêu:** Bù đắp lỗ hổng lớn nhất của dự án (trò chơi hiện đang câm hoàn toàn), mang lại phản hồi xúc giác mạnh mẽ chuẩn phong cách Arcade.

### Task 1.1: Kiến Trúc Quản Lý Âm Thanh Tập Trung (AudioManager 0 GC)
- [x] **1.1.1** Tạo [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs) theo mô hình Singleton, Unidirectional Data Flow, cấu hình sẵn 3 AudioMixer Groups: `Master`, `Music (BGM)`, `Sound Effects (SFX)`.
- [x] **1.1.2** Xây dựng bộ đệm Object Pool tái sử dụng 16 `AudioSource` 2D và 16 `AudioSource` 3D (0 GC Allocation, không gọi `AudioSource.PlayClipAtPoint` gây cấp phát rác).
- [x] **1.1.3** Kết nối sự kiện bắn đạn trong [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs) và nòng súng [`VirtualRailMuzzle.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailMuzzle.cs).

### Task 1.2: Thư Viện Sound Effects (SFX) Chuẩn Star Fox 64 Arcade
- [x] **1.2.1** **Hỏa lực:**
  - SFX Laser bắn chuẩn: Single Laser (tiếng tạch giòn), Twin Laser (tiếng kép dày âm), Hyper Laser (tiếng năng lượng rít).
  - SFX Tụ lực Charge Shot: Vòng lặp tiếng rít nạp điện cao tần (Charging Loop) + Tiếng bắt khóa mục tiêu Lock-on ("BEEP-BEEP!").
  - SFX Nổ lan Splash Impact của quả cầu năng lượng khi trúng mục tiêu.
- [x] **1.2.2** **Phòng thủ & Cơ động:**
  - SFX Kích nổ Smart Bomb: Tiếng phóng bom rít gió + Sóng xung kích rung chuyển không gian (Sub-bass explosion).
  - SFX Lộn cánh Barrel Roll Deflection: Tiếng xé gió từ trường hất văng đạn plasma.
  - SFX Động cơ Arwing: Âm gầm phản lực khi giữ Boost + Tiếng xì hơi giảm áp khi giữ Phanh Air Brake.
- [x] **1.2.3** **Va chạm, Cảnh báo & Phần thưởng:**
  - SFX Thiên thạch nổ vỡ vụn & Tàu quái vật nổ tung.
  - SFX Cảnh báo khiên yếu (Shield Alarm Beeping) khi máu dưới $30\%$.
  - SFX Thu thập Vòng Năng Lượng (Ring Collect Chime) & Nâng cấp súng Laser.
  - SFX Bộ đàm Radio Transmission (Voice chatter blips kiểu N64 cổ điển).

### Task 1.3: Hệ Thống Nhạc Nền Động (Interactive BGM System)
- [x] **1.3.1** BGM Main Menu (Giai điệu khoa học viễn tưởng hào hùng).
- [x] **1.3.2** BGM Stage 1: "Operation Meteor Breach" (Space Synth / Orchestral nhịp điệu dồn dập).
- [x] **1.3.3** BGM Boss Encounter (Tiết tấu căng thẳng kịch tính).
- [x] **1.3.4** BGM Victory Fanfare (Mừng chiến thắng qua màn) & BGM Game Over.
- [x] **1.3.5** Chuyển tiếp nhạc nền trơn tru qua `AudioMixerSnapshot` khi nhảy vọt Hyperspace Warp.

### Task 1.4: Rung Chấn Camera & Cảnh Báo Viền Màn Hình (Polish Feel)
- [x] **1.4.1** Tích hợp module rung chấn Camera Impulse (0 GC Alloc) vào [`VirtualRailCameraRig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailCameraRig.cs):
  - Rung nhẹ (Micro-shake): Khi nổ động cơ Boost hoặc lộn cánh Barrel Roll.
  - Rung mạnh (Heavy-shake): Khi tàu dính đạn, va quẹt thiên thạch, hoặc kích nổ Smart Bomb.
- [x] **1.4.2** Tạo hiệu ứng viền màn hình nhấp nháy đỏ (Vignette Low-Health Pulse) trên Canvas UI khi khiên tàu rơi vào tình trạng nguy cấp.

---

## IV. GIAI ĐOẠN 2: TRẬN ĐẤU TRÙM & CHIẾN HẠM HẠNG NẶNG (BOSS ECOSYSTEM)
> **Mục tiêu:** Hiện thực hóa đỉnh cao trào của màn chơi theo đúng đặc tả [`LEVEL_DESIGN_SPECIFICATION.md`](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md).

### Task 2.1: Kiến Trúc Nền Tảng Boss & Bộ Phận Bị Phá Hủy (Boss Framework)
- [ ] **2.1.1** Xây dựng [`BossLifecycleFSM.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossLifecycleFSM.cs): Quản lý các trạng thái Intro $\rightarrow$ Combat (Pha 1, 2, 3) $\rightarrow$ Defeated $\rightarrow$ Death Explosion Sequence.
- [ ] **2.1.2** Xây dựng [`BossWeakpoint.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossWeakpoint.cs): Quản lý từng tháp pháo/bộ phận độc lập có thanh máu riêng, khả năng bộc lộ hoặc ẩn nấp theo trạng thái.
- [ ] **2.1.3** Kết nối trực tiếp vào [`VirtualRailStageDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailStageDirector.cs) để tự động ghìm tốc độ ray đường bay về $5 - 10\text{ m/s}$ khi giao tranh Boss.

### Task 2.2: Mini-Boss Tuần Dương Hạm ("Iron Vanguard" - Cụm Mục Tiêu Đa Điểm)
- [ ] **2.2.1** Mô hình hóa Tuần Dương Hạm đồng tốc trước mũi tàu ở cự ly $+70\text{m}$ ($Z = 11,500\text{m}$).
- [ ] **2.2.2** Tích hợp 3 cụm mục tiêu độc lập:
  - Tháp pháo cánh trái (HP: 60) bắn đạn chùm 3 viên.
  - Tháp pháo cánh phải (HP: 60) bắn đạn chùm 3 viên.
  - Ống phóng tên lửa trung tâm (HP: 120) liên tục phóng tên lửa tự dẫn.
- [ ] **2.2.3** Cơ chế bộc lộ buồng lái (Bridge Weakpoint): Bắn nổ 2 tháp pháo cánh $\implies$ Lộ buồng lái chỉ huy $\implies$ Tiêu diệt thưởng Smart Bomb và $+10\text{ Hits}$.

### Task 2.3: Main Boss Pháo Đài Lõi ("Gorgon Core" - Máy Trạng Thái 3 Pha)
- [ ] **2.3.1** **Pha 1: Khiên Hợp Kim Xoay Vòng ($100\% \rightarrow 66\%\text{ HP}$):**
  - 4 Tấm khiên thép xoay quanh lõi tốc độ $45^\circ/\text{s}$.
  - Súng trung tâm bắn lưới đạn Plasma buộc người chơi lộn cánh Barrel Roll hất đạn.
- [ ] **2.3.2** **Pha 2: Bão Tên Lửa & Vòng Xung Kích EMP ($66\% \rightarrow 33\%\text{ HP}$):**
  - 2 Tấm khiên nổ tung, tốc độ xoay tăng vọt $90^\circ/\text{s}$.
  - Boss phóng 6 quả tên lửa dẫn đường và dộng sàn phát sóng xung kích màu lam (yêu cầu Boost bay lên trần để né).
- [ ] **2.3.3** **Pha 3: Cuồng Nộ & Tia Hủy Diệt Hyper Laser ($33\% \rightarrow 0\%\text{ HP}$):**
  - Toàn bộ khiên nổ sạch, lõi chuyển sang màu đỏ rực.
  - Boss quét luồng laser khổng lồ chiếm $60\%$ màn hình (cửa sổ vàng để người chơi xả toàn bộ bom và đạn đôi dứt điểm trùm).

### Task 2.4: Giao Diện Boss UI & Chuỗi Cắt Cảnh Hạ Boss
- [ ] **2.4.1** Tạo `BossHealthBarUI`: Thanh máu Boss đa phân đoạn ở đáy màn hình hiển thị 3 pha máu rõ ràng.
- [ ] **2.4.2** Cảnh báo báo động đỏ toàn màn hình: *"WARNING: GORGON CORE DETECTED"*.
- [ ] **2.4.3** Cắt cảnh nổ dây chuyền (Chain Explosions) và sóng xung kích trắng xóa mừng chiến thắng trước khi mở cổng Hyperspace Warp.

---

## V. GIAI ĐOẠN 3: VÒNG LẶP TRÒ CHƠI & GIAO DIỆN NGƯỜI DÙNG (COMPLETE GAME LOOP & UI/UX)
> **Mục tiêu:** Xây dựng vòng lặp trò chơi hoàn chỉnh từ Main Menu $\rightarrow$ Gameplay $\rightarrow$ Pause $\rightarrow$ Debriefing/Medal $\rightarrow$ Save Persistence.

### Task 3.1: Menu Chính Toàn Diện (Arcade Main Menu)
- [ ] **3.1.1** Thiết kế giao diện Main Menu Sci-Fi hiện đại:
  - Nút **Campaign Mode** (Chơi Chiến dịch theo Stage 1 -> 2 -> 3).
  - Nút **Endless Mode** (Chế độ Sinh tồn Vô hạn săn điểm số).
  - Nút **Settings / Options** (Cài đặt hệ thống).
  - Nút **Medals & Hall of Fame** (Bộ sưu tập Huân chương & Bảng vinh danh điểm cao).
  - Nút **Quit Game** (Thoát game).
- [ ] **3.1.2** Tích hợp hiệu ứng tàu Arwing bay tuần tra làm phông nền menu động 3D.

### Task 3.2: Menu Tạm Dừng Trong Trận (Pause Menu - ESC / P)
- [ ] **3.2.1** Tạm dừng trò chơi mượt mà (`Time.timeScale = 0`), làm mờ hậu cảnh Canvas.
- [ ] **3.2.2** Cung cấp các nút: Tiếp tục (Resume), Chơi lại màn (Restart Stage), Cài đặt nhanh (Options), Thoát ra Menu chính (Quit to Menu).

### Task 3.3: Bảng Cài Đặt Hệ Thống (Settings Panel)
- [ ] **3.3.1** **Cài đặt Âm thanh:** Thanh trượt điều chỉnh âm lượng Master, Music (BGM), Sound Effects (SFX).
- [ ] **3.3.2** **Cài đặt Điều khiển (Flight Controls):**
  - Tùy chọn **Đảo trục Y (Invert Flight Y-Axis)** cho cả Chuột và Cần điều khiển Analog.
  - Thanh trượt điều chỉnh Độ nhạy Chuột / Analog Stick (Aim Sensitivity).
- [ ] **3.3.3** **Cài đặt Đồ họa & Hiển thị:**
  - Chọn cấu hình chất lượng URP (Fastest / Good / Beautiful).
  - Tùy chọn Độ phân giải màn hình, Chế độ Cửa sổ / Toàn màn hình (Fullscreen / Windowed).

### Task 3.4: Màn Hình Tổng Kết & Trao Huân Chương (Mission Accomplished Debriefing)
- [ ] **3.4.1** Hiển thị bảng tổng kết thành tích sau khi hoàn tất màn chơi:
  - Tổng số mục tiêu bị tiêu diệt (`Hit Count`).
  - Điểm thưởng nổ lan (`Charge Shot Combos`).
  - Độ vẹn toàn của Khiên (`Shield Integrity %`).
  - Thời gian hoàn thành (`Mission Clear Time`).
  - Tổng điểm cuối cùng (`Final Score`).
- [ ] **3.4.2** Thuật toán tính bậc xếp hạng kỹ năng: **Rank D, Rank C, Rank B, Rank A, Rank S**.
- [ ] **3.4.3** Cơ chế trao tặng **Huân Chương Danh Dự (Stage Medal)**:
  - Kích hoạt biểu tượng Huân chương Vàng sáng bóng kèm hiệu ứng chuông ngân khi đạt đủ cả 2 điều kiện: $\text{Hit Count} \ge 150\text{ Hits}$ và $\text{Khiên cuối màn} \ge 50\%$.

### Task 3.5: Màn Hình Thất Bại (Mission Failed / Game Over)
- [ ] **3.5.1** Hiển thị màn hình khi tàu bị bắn hạ: Thông báo thất bại, điểm số đạt được, tùy chọn Thử lại ngay (Retry Stage) hoặc Thoát về Menu.

### Task 3.6: Hệ Thống Lưu Trữ Dữ Liệu Bền Vững (Save & Persistence Manager)
- [ ] **3.6.1** Xây dựng [`SaveDataManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SaveDataManager.cs) lưu trữ độc lập:
  - Điểm cao kỷ lục của từng Stage (High Score per Stage).
  - Điểm kỷ lục chế độ Endless (Endless Best Distance & Score).
  - Trạng thái Huân chương đã mở khóa (Medal Unlock Flags).
  - Cấu hình tùy chọn âm thanh và điều khiển người chơi đã chỉnh.

---

## VI. GIAI ĐOẠN 4: NỘI DUNG MỞ RỘNG & CHẾ ĐỘ CHƠI VÔ HẠN (EXPANDED CONTENT & ENDLESS)
> **Mục tiêu:** Mở rộng trải nghiệm chiến dịch cho Stage 2/3 và hiện thực hóa Chế độ Chơi Vô hạn theo đúng đặc tả [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md).

### Task 4.1: Hoàn Thiện Kịch Bản Stage 2 (Volcano / Lava Planet)
- [ ] **4.1.1** Xây dựng kịch bản Timeline trong [`StageData_02.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_02.asset): Vùng biển dung nham, bão nhiệt, cột lửa phun trào ngẫu nhiên từ dưới lên.
- [ ] **4.1.2** Tích hợp các đợt quái chuyên biệt: Phi đội tiêm kích lượn sườn, máy bay ném bom nhiệt đới.

### Task 4.2: Hoàn Thiện Kịch Bản Stage 3 (Deep Space / Black Hole & Alien Citadel)
- [ ] **4.2.1** Xây dựng kịch bản Timeline trong [`StageData_03.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_03.asset): Không gian sâu thẳm, lực hút từ trường bẻ cong đạn đạo, phi đội phòng thủ pháo đài mẹ.
- [ ] **4.2.2** Đại chiến Boss Tối Thượng kết thúc toàn bộ chiến dịch Lylat.

### Task 4.3: Chế Độ Chơi Vô Hạn Thủ Tục (Endless Procedural Corridor theo TDD v2.0.0)
- [ ] **4.3.1** **Bộ Ghép Nối Đường Ray Vô Hạn (Procedural Spline Chunker):**
  - Sinh đường ray liên tục theo từng đoạn Chunk $400 - 600\text{m}$.
  - Duy trì tối thiểu 3 Chunks đồng thời trong bộ nhớ và bảo đảm tính trơn hình học $C^2$ tại hai đầu ghép.
- [ ] **4.3.2** **Hành Lang Thoát Hiểm Ảo (Golden Path Raycast):**
  - Thuật toán quét trước kiểm định hình học để loại bỏ $100\%$ tình huống "bức tường chết" (Unwinnable scenarios).
- [ ] **4.3.3** **Tái Định Vị Thế Giới (World Origin Shifting):**
  - Dịch chuyển toàn bộ thực thể ngược về gốc $(0, 0, 0)$ mỗi khi đạt mốc tích lũy $4,000\text{m}$ để triệt tiêu hoàn toàn rung giật số thực (Floating-point jitter).
- [ ] **4.3.4** **Bộ Điều Chỉnh Độ Khó Động (DDA 3 Pha Răng Cưa):**
  - Chu kỳ $60 - 80\text{ giây}$: *Leo dốc (Ramp $60\%$) $\rightarrow$ Đỉnh điểm (Climax $25\%$) $\rightarrow$ Thư giãn/Hồi sức (Breather $15\%$)*.

---

## VII. NHẬT KÝ THỰC THI & NGHIỆM THU (EXECUTION & ACCEPTANCE LOG)

| Thời Gian | Task ID | Mô Tả Công Việc Thực Hiện | Tệp Tác Động | Kết Quả MSBuild | Người Duyệt |
| :---: | :---: | :--- | :--- | :---: | :---: |
| *08/10/2026* | **INIT** | Khởi tạo Kế hoạch tổng thể và Cơ chế theo dõi tiến độ | [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) | `0 Errors` | User |
| *08/10/2026* | **PHASE 1** | Hoàn thành AudioManager 0 GC, Bộ tổng hợp SFX Arcade Star Fox 64, BGM Dual-Crossfader, Rung chấn Camera Impulse Traumatic Model | [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailCameraRig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailCameraRig.cs), [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs), [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs), [`VirtualRailShip.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailShip.cs), [`Shield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/Shield.cs), [`ShieldUI.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/ShieldUI.cs), [`StageData.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData.cs) | `0 Errors` | User (Thực hiện) |
| *09/10/2026* | **BGM-LINK** | Tích hợp 4 bản nhạc nền MokkaMusic (Cryosleep, Pusher, Vision, Secrecy) vào StageData 1/2/3, nạp Playlist và MenuItem 44 | [`StageData_01.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_01.asset), [`StageData_02.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_02.asset), [`StageData_03.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_03.asset), [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs) | `0 Errors` | User (Yêu cầu thêm nhạc) |
| *09/10/2026* | **DUAL-SHUFFLE** | Xây dựng cơ chế Dual-Pool Shuffle: Stage Shuffle (4 bài, Deck Fisher-Yates, Auto-Crossfade 2.0s) & Boss Pool độc lập (tự động chuyển cảnh khi đấu Boss) | [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailStageDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailStageDirector.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs), [`StageData_01.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_01.asset) | `0 Errors` | User (Thực hiện) |
| *--* | *--* | *Sẵn sàng kích hoạt Giai đoạn 2 (Boss & Mini-Boss Ecosystem)* | *--* | *--* | *--* |

---

## VIII. HƯỚNG DẪN RA LỆNH THỰC THI CHO NGƯỜI DÙNG (COMMAND QUICK REFERENCE)

Khi bạn muốn bắt đầu triển khai bất kỳ hạng mục nào, chỉ cần ra lệnh bằng các câu lệnh mẫu sau:
* *"Bắt đầu triển khai Giai đoạn 1"* $\implies$ AI sẽ lập phương án chi tiết cho Task 1.1 (AudioManager & SFX Pool).
* *"Bắt đầu triển khai Task 1.1"* $\implies$ AI tập trung xây dựng module âm thanh tập trung.
* *"Bắt đầu triển khai Giai đoạn 2"* $\implies$ AI tập trung thiết kế Boss FSM và Mini-Boss Cruiser.
* *"Cập nhật tiến độ"* $\implies$ AI sẽ đọc trạng thái các file và cập nhật các checkbox `[x]` tương ứng trong file này.
