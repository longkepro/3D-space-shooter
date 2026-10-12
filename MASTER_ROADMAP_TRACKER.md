# KẾ HOẠCH TỔNG THỂ & THEO DÕI TIẾN ĐỘ DỰ ÁN (MASTER ROADMAP & PROGRESS TRACKER)
> **Dự án:** 3D Sci-Fi Virtual Rail Shooter (Chuẩn Star Fox 64 Arcade & Endless Survival)  
> **Tài liệu nguồn chân lý:** [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md) • [`LEVEL_DESIGN_SPECIFICATION.md`](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md) • [`STAGE_1_DESIGN_DOCUMENT.md`](file:///d:/3D-Shooter-master/STAGE_1_DESIGN_DOCUMENT.md) • [`STARFOX64_AIMING_PLAN.md`](file:///d:/3D-Shooter-master/STARFOX64_AIMING_PLAN.md) • [`STARFOX64_ENEMY_SPAWN_PLAN.md`](file:///d:/3D-Shooter-master/STARFOX64_ENEMY_SPAWN_PLAN.md)  
> **Quy chuẩn hiệu lực:** Tuân thủ 100% Bộ Quy Tắc Phát Triển Dự Án ([`AGENTS.md`](file:///d:/3D-Shooter-master/AGENTS.md) / [`GEMINI.md`](file:///d:/3D-Shooter-master/GEMINI.md)) — An toàn 3 tầng, 0 GC Allocation, Kiểm thử biên dịch `0 Errors`.

---

## I. BẢNG ĐIỀU KHIỂN TIẾN ĐỘ TỔNG THỂ (EXECUTIVE DASHBOARD)

```text
TIẾN ĐỘ TOÀN DỰ ÁN HƯỚNG TỚI BẢN HOÀN CHỈNH (PRODUCTION-READY ENDLESS ARCADE):
[███████████████████████████░░░░░] 84% HOÀN TẤT
```

| Phân Hệ / Giai Đoạn | Trọng Tâm Phát Triển | Trạng Thái | Số Task | Tỉ Lệ | Ghi Chú Nghiệm Thu |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Giai đoạn 0: Cốt Lõi Bay & Vũ Khí** | Bay Arwing, Vũ khí, Đội hình Bézier, DDA Director, Điểm số | **HOÀN TẤT** | 12/12 | $100\%$ | Biên dịch 0 Errors, 0 GC Game Loop |
| **Giai đoạn 1: Âm Thanh & Xúc Giác** | AudioManager 0 GC, SFX N64, Dual-BGM Shuffle, Camera Shake | **HOÀN TẤT** | 4/4 | $100\%$ | 0 GC Pool, Procedural SFX + Dual Shuffle |
| **Giai đoạn 2: Đột Kích Trùm Tuần Hoàn** | Trùm Tuần Duyên Corvette F3 ("Crimson Titan") 3 Pha | **HOÀN TẤT** | 6/6 | $100\%$ | Toàn bộ 6 task hoàn tất: Khiên Hex, Dual HUD, Shader Pha 2, Sửa Va Chạm & Splash 4X, Tháp Pháo & Buồng Lái Stun, Pha 3 Hyper Laser & Endless Loop |
| **Giai đoạn 3: Vòng Lặp & UI/UX Endless** | Modern Cockpit HUD, Multiplier Gauge, Menu, Bảng Vinh Danh | **CHỜ DUYỆT** | 0/6 | $0\%$ | Game Loop Endless thương mại |
| **Giai đoạn 4: Hành Lang Thủ Tục & Quần Xã** | Spline Chunker, Solvability Verifier, Chuyển Quần Xã, Cổng Rủi Ro | **CHỜ DUYỆT** | 0/4 | $0\%$ | Procedural Hermite Spline + World Origin Shifting |

---

## II. CƠ CHẾ THEO DÕI & QUY ƯỚC TRẠNG THÁI (TRACKING PROTOCOL)

Để người dùng và AI theo dõi chính xác từng bước, các đầu việc tuân theo quy ước ký hiệu:
* `[ ]` **Chờ thực hiện (Pending):** Đã lập kế hoạch, chưa có lệnh phê duyệt thực thi từ người dùng.
* `[-]` **Đang triển khai (In Progress):** Đã được phê duyệt, đang tạo mã nguồn, asset và kiểm thử.
* `[x]` **Đã hoàn thành & Nghiệm thu (Completed):** Đã chạy MSBuild đạt **`0 Errors`**, kiểm thử thực tế đạt yêu cầu và đã cập nhật nhật ký.

> [!IMPORTANT]
> **QUY TRÌNH KÍCH HOẠT THAY ĐỔI THEO ĐIỀU 1 BỘ QUY TẮC:**
> 1. AI không tự ý sửa code hoặc tạo asset khi người dùng chưa ra lệnh trực tiếp (*"thực hiện Giai đoạn 2"*, *"tiến hành sửa xung đột"*, v.v.).
> 2. Mỗi thay đổi đều phải tạo điểm checkpoint, bảo lưu prefab gốc và cung cấp MenuItem Editor hoàn nguyên (Undo).
> 3. Sau khi hoàn thành một task, AI cập nhật dấu `[x]` và ghi nhật ký vào Mục VII của tài liệu này.

---

## III. GIAI ĐOẠN 1: HỆ THỐNG ÂM THANH & PHẢN HỒI XÚC GIÁC (AUDIO & POLISH CORE)
> **Mục tiêu:** Mang lại phản hồi âm thanh và xúc giác mạnh mẽ chuẩn phong cách Arcade Star Fox 64.

### Task 1.1: Kiến Trúc Quản Lý Âm Thanh Tập Trung (AudioManager 0 GC)
- [x] **1.1.1** Tạo [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs) theo mô hình Singleton, Unidirectional Data Flow, cấu hình sẵn 3 AudioMixer Groups: `Master`, `Music (BGM)`, `Sound Effects (SFX)`.
- [x] **1.1.2** Xây dựng bộ đệm Object Pool tái sử dụng 16 `AudioSource` 2D và 16 `AudioSource` 3D (0 GC Allocation, không gọi `AudioSource.PlayClipAtPoint` gây cấp phát rác).
- [x] **1.1.3** Kết nối sự kiện bắn đạn trong [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs) và nòng súng [`VirtualRailMuzzle.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailMuzzle.cs).

### Task 1.2: Thư Viện Sound Effects (SFX) Chuẩn Star Fox 64 Arcade
- [x] **1.2.1** **Hỏa lực:**
  - SFX Laser bắn chuẩn: Single Laser, Twin Laser, Hyper Laser.
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
- [x] **1.3.1** BGM Stage Shuffle Playlist: Tích hợp 4 bản nhạc nền MokkaMusic (Cryosleep, Pusher, Vision, Secrecy) qua Deck Fisher-Yates không trùng lặp và Auto-Crossfade $2.0\text{s}$.
- [x] **1.3.2** BGM Boss Encounter độc lập: Tự động chuyển tiếp trơn tru qua `AudioMixerSnapshot` khi chạm trán Boss Incursion.
- [x] **1.3.3** BGM Victory Fanfare (Mừng chiến thắng qua chu kỳ) & BGM Game Over.

### Task 1.4: Rung Chấn Camera & Cảnh Báo Viền Màn Hình (Polish Feel)
- [x] **1.4.1** Tích hợp module rung chấn Camera Impulse (0 GC Alloc) vào [`VirtualRailCameraRig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailCameraRig.cs): Micro-shake khi Boost/Roll; Heavy-shake khi trúng đạn hoặc dính bom.
- [x] **1.4.2** Tạo hiệu ứng viền màn hình nhấp nháy đỏ (Vignette Low-Health Pulse) trên Canvas UI khi khiên tàu rơi vào tình trạng nguy cấp.

---

## IV. GIAI ĐOẠN 2: ĐỘT KÍCH TRÙM TUẦN HOÀN (APEX CLIMAX BOSS INCURSION)
> **Mục tiêu:** Chiến hạm Tuần duyên Liên bang F3 ("Crimson Titan") đóng vai trò là Trùm đỉnh cao trào (Apex Climax Encounter) xuất hiện định kỳ tại đỉnh mỗi chu kỳ DDA Endless theo đúng đặc tả [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md).

### Task 2.1: Máy Trạng Thái 3 Pha & Neo Đấu Trường Trùm
- [x] **2.1.1** Xây dựng [`BossLifecycleFSM.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossLifecycleFSM.cs): Quản lý chuyển tiếp trạng thái `Phase1_Shielded` $\rightarrow$ `Phase2_ExposedTrench` $\rightarrow$ `Phase3_OverdriveCore` $\rightarrow$ `Defeated`.
- [x] **2.1.2** Kết nối với [`VirtualRailStageDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailStageDirector.cs) để tự động ghìm tốc độ ray đường bay về $5 - 10\text{ m/s}$ khi bắt đầu giao tranh Boss.

### Task 2.2: Lớp Khiên Lục Giác & Dual-Gauge Cockpit HUD
- [x] **2.2.1** Tách biệt độ bền khiên: Xây dựng [`VirtualRailHexShield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHexShield.cs) với quỹ máu riêng $300\text{ HP}$, hấp thụ $100\%$ sát thương và hiệu ứng chớp sáng Neon White (`_HitFlashIntensity`) khi trúng đạn.
- [x] **2.2.2** Tích hợp hiển thị song song 2 thanh đo trên [`ModernCockpitHUD.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/ModernCockpitHUD.cs): Thanh Thân Tàu (Hull - Xanh lục) và thanh Khiên Năng Lượng (Shield - Xanh lam), đồng bộ mượt mà ở `LateUpdate`.

### Task 2.3: Visual Hẻm Lò Phản Ứng Pha 2 (Thermal Reactor Trench Presentation)
- [x] **2.3.1** Tạo Shader chuyên dụng [`VirtualRail_ThermalReactorTrench.shader`](file:///d:/3D-Shooter-master/Assets/Shaders/VirtualRail_ThermalReactorTrench.shader) & Material [`ReactorTrench_HotPlasma.mat`](file:///d:/3D-Shooter-master/Assets/Materials/ReactorTrench_HotPlasma.mat) mô phỏng dòng dung nham plasma cuộn chảy.
- [x] **2.3.2** Xây dựng controller [`VirtualRailReactorTrenchVisual.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailReactorTrenchVisual.cs): Điều khiển cường độ tỏa nhiệt `_CoreGlowIntensity`, nhịp thở nạp năng lượng `_ChargingPulseRate`, và xung chớp giật gián đoạn khi bị người chơi bắn trúng (0 GC `MaterialPropertyBlock`).

### Task 2.4: Khắc Phục Xung Đột Va Chạm & Sát Thương Đa Điểm (Collision & Splash Decoupling)
- [x] **2.4.1** **Tách biệt Collider Khiên & Vỏ Tàu:** Hiệu chỉnh BoxCollider khiên ($Y = 4.5\text{m}, \text{Scale} = [36, 10.5, 114]$) và hạ đỉnh vỏ tàu ($Y = 1.5\text{m}, \text{Size} = [30, 10, 108]$) tạo biên độ đệm an toàn $+3.25\text{m}$ trên trục Y; chuyển hướng $100\%$ sát thương đạn vào khiên khi khiên đang hoạt động.
- [x] **2.4.2** **Khắc phục Bug Nổ Lan Tên Lửa 4X Sát Thương:** Phân nhóm multi-hit suppression 0 GC (lọc ID Boss và mảng đệm 32 EnemyHealth) trong `VirtualRailHomingMissile.cs`, đảm bảo quả cầu nổ lan chỉ gây sát thương 1 lần duy nhất lên Boss.
- [x] **2.4.3** **Cân Bằng Lại HP Hardpoint vs Máu Boss & Đồng Bộ 3 Pha:** Đồng bộ hóa chuyển pha và giới hạn máu từng pha (Pha 1 chặn ở 660 HP, Pha 2 chặn ở 330 HP, Pha 3 dứt điểm về 0); giữ mở collider rãnh lò phản ứng khi bị phá hủy để cho phép ngắm bắn lõi nóng chảy ở Pha 3.

### Task 2.5: Khắc Phục Logic Tháp Pháo & Điểm Yếu Buồng Lái (Turret & Bridge Logic)
- [x] **2.5.1** **Khắc phục Tháp Pháo Bị Đóng Băng ở Pha 2:** Cập nhật FSM để tháp pháo cánh còn sống tiếp tục bắn đạn phòng không (Flak) mỗi $1.8\text{s}$ ở Pha 2 nếu chưa bị phá hủy ở Pha 1, tạo áp lực chiến thuật buộc người chơi phải cân nhắc giữa bắn Lò Phản Ứng hay tiêu diệt tháp pháo.
- [x] **2.5.2** **Kích Hoạt Buồng Lái Chỉ Huy (`_mainBridge`):** Kết nối biến `_mainBridge` làm điểm yếu phụ, bắn nổ buồng lái kích hoạt hàm `TriggerBridgeStun()` làm tê liệt ngắt toàn bộ hệ thống sạc/vũ khí của Boss trong $4.0\text{s}$ kèm tia lửa điện `_stunSparksPrefab` và rung chấn camera $0.45\text{f}$.
- [x] **2.5.3** **Bổ Sung Âm Thanh, Hiệu Ứng & Điểm Số Phá Hủy Hardpoint:** Kích hoạt SFX nổ lớn kim loại (`AudioSFXType.ExplosionLarge`), hiệu ứng nổ tung `_explosionPrefab` tại vị trí hardpoint, rung camera $0.35\text{f}$ và cộng thưởng điểm số tức thì: Tháp pháo $+2,500\text{ PTS}$, Buồng lái $+5,000\text{ PTS}$, Khe nhiệt $+10,000\text{ PTS}$.

### Task 2.6: Hoàn Thiện Pha 3 Cuồng Nộ & Kết Nối Vòng Lặp Endless (Phase 3 & Loop Link)
- [x] **2.6.1** **Khắc phục Bug Tắt Vĩnh Viễn Collider Lò Phản Ứng:** Đảm bảo khi máu khe nhiệt chạm 0 ở Pha 2, collider được bảo lưu và giữ mở ở Pha 3 (`SetHardpointColliderActive(true)`) để người chơi bắn vào vùng lõi nóng chảy dứt điểm Boss.
- [x] **2.6.2** **Hiện Thực Hóa Cơ Chế Quá Tải Pha 3:** Kích hoạt chu kỳ sạc $4.5\text{s}$ với cảnh báo âm thanh $1.8\text{s}$ (`AudioSFXType.ChargeHum`), giải phóng chùm 11 tia Hyper Laser (`AudioSFXType.LaserHyper`) quét ngang $X \in [-18\text{m}, +18\text{m}]$ ở vận tốc $55\text{ m/s}$ bao phủ hành lang trung tâm kèm rung chấn camera $0.5\text{f}$.
- [x] **2.6.3** **Kết Nối Sự Kiện Hạ Boss với Endless Loop:** Khi Boss bị tiêu diệt:
  - Thưởng ngay $+50,000\text{ PTS}$, dọn sạch toàn bộ đạn đạo trên màn hình (`EnemyProjectilePool.ClearAllActiveProjectiles()`).
  - Thông báo cho [`VirtualRailEndlessDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailEndlessDirector.cs) qua `OnBossIncursionDefeated()`: tăng Threat Level chu kỳ tiếp theo ($+1$), chuyển trạng thái sang Thư Giãn (BreathingRoom $5.0\text{s}$), phát âm RadioChirp và thông báo HUD "BOSS ELIMINATED! SECTOR THREAT LEVEL UP".

---

## V. GIAI ĐOẠN 3: VÒNG LẶP TRÒ CHƠI & GIAO DIỆN ENDLESS (ENDLESS LOOP & UI/UX)
> **Mục tiêu:** Xây dựng vòng lặp trải nghiệm Endless hoàn chỉnh từ Menu $\rightarrow$ Gameplay Săn Điểm $\rightarrow$ Bảng Vinh Danh & Lưu Trữ Kỷ Lục.

### Task 3.1: Menu Chính Phong Cách Sci-Fi Arcade (Main Menu)
- [ ] **3.1.1** Thiết kế Menu hiện đại tập trung vào chế độ Endless:
  - Nút **Launch Endless Run** (Xuất kích Sinh tồn Vô hạn).
  - Nút **Settings / Options** (Cài đặt âm thanh, đồ họa, đảo trục Y, độ nhạy).
  - Nút **Hall of Fame & Medals** (Bảng vinh danh điểm cao kỷ lục, quãng đường xa nhất, huân chương).
  - Nút **Quit Game** (Thoát game).
- [ ] **3.1.2** Tích hợp cảnh tàu Arwing bay tuần tra 3D làm phông nền menu động.

### Task 3.2: HUD Sinh Tồn Săn Điểm Chuyên Sâu (Endless Scoring HUD)
- [ ] **3.2.1** **Thanh Multiplier Gauge ($1\times \to 32\times$):** Hiển thị hệ số nhân hiện tại và thanh tụt năng lượng trong $3.5\text{s}$.
- [ ] **3.2.2** **Hiệu Ứng Lướt Cận Kề (Graze Indicator):** Báo hiệu tia lửa điện dọc cánh kèm chữ số nổi $+500 \times \mathbf{M}$ khi lướt sát đạn.
- [ ] **3.2.3** **Huy Hiệu Threat Level & Distance Tracker:** Hiển thị cấp độ đe dọa chu kỳ hiện tại và số mét đã chinh phục theo thời gian thực.
- [ ] **3.2.4** **Thanh Máu Boss Đa Phân Đoạn:** Tự động trượt vào đáy màn hình khi Boss Incursion xuất hiện và hiển thị rõ 3 pha máu.

### Task 3.3: Menu Tạm Dừng Trong Trận (Pause Menu - ESC / P)
- [ ] **3.3.1** Tạm dừng trò chơi (`Time.timeScale = 0`), làm mờ nền màn hình, cung cấp tùy chọn Tiếp tục, Xuất kích lại, Cài đặt, Thoát về Menu.

### Task 3.4: Bảng Tổng Kết & Xếp Hạng Kỷ Lục (Run Debriefing & High Score)
- [ ] **3.4.1** Màn hình kết thúc lượt chạy hiển thị chi tiết:
  - Tổng quãng đường bay được ($\text{Distance Reached}$).
  - Cấp độ đe dọa tối đa vượt qua ($\text{Max Threat Level / Cycles Survived}$).
  - Tổng số mục tiêu bị tiêu diệt ($\text{Enemy Hit Count}$).
  - Số lần kích hoạt Graze thành công ($\text{Graze Count}$).
  - Hệ số nhân cực đại chạm tới ($\text{Peak Multiplier}$).
  - Tổng điểm số cuối cùng ($\text{Final Score}$).
- [ ] **3.4.2** Thuật toán xếp hạng kỹ năng: **Rank D, Rank C, Rank B, Rank A, Rank S**.
- [ ] **3.4.3** Trao tặng Huân Chương Sinh Tồn Vô Hạn (Endless Ace Medals) theo các cột mốc điểm số và quãng đường.

### Task 3.5: Hệ Thống Lưu Trữ Dữ Liệu Bền Vững (SaveDataManager)
- [ ] **3.5.1** Xây dựng [`SaveDataManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SaveDataManager.cs) lưu trữ độc lập điểm cao kỷ lục, quãng đường xa nhất, danh sách huân chương và cấu hình người chơi.

---

## VI. GIAI ĐOẠN 4: HÀNH LANG THỦ TỤC & CHUYỂN TIẾP QUẦN XÃ (PROCEDURAL CORRIDOR & BIOMES)
> **Mục tiêu:** Mở rộng trải nghiệm Endless với đường ray sinh thủ tục theo thời gian thực và chuyển tiếp quần xã liền mạch theo [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md).

### Task 4.1: Bộ Ghép Nối Đường Ray Vô Hạn (Hermite Spline Chunker)
- [ ] **4.1.1** Sinh đường ray liên tục theo từng đoạn Chunk $400 - 600\text{m}$, duy trì tối thiểu 3 Chunks đồng thời trong bộ nhớ.
- [ ] **4.1.2** Đảm bảo tính trơn hình học $C^2$ tại hai đầu ghép nối (tiếp giáp vị trí, tiếp tuyến vận tốc và pháp tuyến độ cong).

### Task 4.2: Thuật Toán Bảo Chứng Khả Thi Hình Học (Golden Path Raycast)
- [ ] **4.2.1** Chạy thuật toán mô phỏng hành lang an toàn $W_{ship} \times H_{ship} \times 1.5$ dọc theo Chunk mới sinh.
- [ ] **4.2.2** Tự động loại bỏ và sinh lại seed mới nếu phát hiện "bức tường chết" (Unwinnable scenarios) không có lối thoát.

### Task 4.3: Tái Định Vị Thế Giới (World Origin Shifting)
- [ ] **4.3.1** Tự động dịch chuyển toàn bộ thực thể ngược về gốc tọa độ $(0, 0, 0)$ mỗi mốc chu kỳ $4,000\text{m}$ để triệt tiêu hoàn toàn rung giật số thực 32-bit.
- [ ] **4.3.2** Biến đo quãng đường tích lũy và bảng điểm sử dụng số thực 64-bit (`double`) độc lập, không bị ảnh hưởng bởi phép dịch chuyển.

### Task 4.4: Chuyển Tiếp Quần Xã Liền Mạch & Cổng Rủi Ro (Biome Transitions & Risk Gates)
- [ ] **4.4.1** **Hầm Chuyển Tiếp / Cổng Warp:** Sau mỗi 3 chu kỳ sinh tồn, đường ray dẫn vào hầm năng lượng để dỡ bộ asset cũ và nạp bộ asset quần xã mới (Vành đai Thiên thạch $\rightarrow$ Vùng Nham Thạch $\rightarrow$ Tinh Vân Không Gian Sâu).
- [ ] **4.4.2** **Cổng Thách Thức Rủi Ro (Risk Gates):** Sinh các khe nứt piston hẹp chứa Huy hiệu Bạch kim, vượt qua thành công đóng băng thanh Multiplier trong $10\text{s}$.

---

## VII. NHẬT KÝ THỰC THI & NGHIỆM THU (EXECUTION & ACCEPTANCE LOG)

| Thời Gian | Task ID | Mô Tả Công Việc Thực Hiện | Tệp Tác Động | Kết Quả MSBuild | Người Duyệt |
| :---: | :---: | :--- | :--- | :---: | :---: |
| *08/10/2026* | **INIT** | Khởi tạo Kế hoạch tổng thể và Cơ chế theo dõi tiến độ | [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) | `0 Errors` | User |
| *08/10/2026* | **PHASE 1** | Hoàn thành AudioManager 0 GC, Bộ tổng hợp SFX Arcade Star Fox 64, BGM Dual-Crossfader, Rung chấn Camera Impulse Traumatic Model | [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailCameraRig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailCameraRig.cs), [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs), [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs), [`VirtualRailShip.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailShip.cs), [`Shield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/Shield.cs), [`ShieldUI.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/ShieldUI.cs), [`StageData.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData.cs) | `0 Errors` | User (Thực hiện) |
| *09/10/2026* | **BGM-LINK** | Tích hợp 4 bản nhạc nền MokkaMusic (Cryosleep, Pusher, Vision, Secrecy) vào StageData 1/2/3, nạp Playlist và MenuItem 44 | [`StageData_01.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_01.asset), [`StageData_02.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_02.asset), [`StageData_03.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_03.asset), [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs) | `0 Errors` | User (Yêu cầu thêm nhạc) |
| *09/10/2026* | **DUAL-SHUFFLE** | Xây dựng cơ chế Dual-Pool Shuffle: Stage Shuffle (4 bài, Deck Fisher-Yates, Auto-Crossfade 2.0s) & Boss Pool độc lập (tự động chuyển cảnh khi đấu Boss) | [`AudioManager.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AudioManager.cs), [`VirtualRailStageDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailStageDirector.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs), [`StageData_01.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_01.asset) | `0 Errors` | User (Thực hiện) |
| *12/10/2026* | **DDA-FIX-B** | Khắc phục triệt để logic DDA: Nối dây tốc độ đạn quái tức thời vào EnemyAttack, bổ sung leo thang mật độ thiên thạch & tỉ lệ bất hoại, tăng tốc độ ray mượt mà theo Threat Level, cung cấp cặp MenuItem 49 & 50 | [`VirtualRailWaveSpawner.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWaveSpawner.cs), [`VirtualRailAsteroidSpawner.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailAsteroidSpawner.cs), [`VirtualRailEndlessDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailEndlessDirector.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs) | `0 Errors` | User (Thực hiện phương án B) |
| *12/10/2026* | **HEX-SHIELD** | Tách biệt quỹ máu khiên 300 HP độc lập, hiệu ứng chớp sáng Neon White khi trúng đạn, tích hợp hiển thị 2 thanh đo song song Hull & Shield trên Cockpit HUD | [`VirtualRailHexShield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHexShield.cs), [`ModernCockpitHUD.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/ModernCockpitHUD.cs), [`VirtualRailShip.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailShip.cs) | `0 Errors` | User (Thực hiện) |
| *12/10/2026* | **PHASE2-VISUAL** | Xây dựng Shader & Controller hiển thị Pha 2 Lò Phản Ứng: plasma cuộn chảy, nhịp thở nạp năng lượng, xung chớp giật gián đoạn khi trúng đạn, tự động tương thích fallback runtime | [`VirtualRail_ThermalReactorTrench.shader`](file:///d:/3D-Shooter-master/Assets/Shaders/VirtualRail_ThermalReactorTrench.shader), [`ReactorTrench_HotPlasma.mat`](file:///d:/3D-Shooter-master/Assets/Materials/ReactorTrench_HotPlasma.mat), [`VirtualRailReactorTrenchVisual.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailReactorTrenchVisual.cs) | `0 Errors` | User (Thực hiện) |
| *12/10/2026* | **BOSS-RCA** | Khảo sát kiểm toán Root Cause Analysis logic Boss Corvette F3: phát hiện 5 logic bị mất và 5 xung đột va chạm/sát thương | [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) | `0 Errors` | User (Yêu cầu kiểm tra) |
| *12/10/2026* | **ENDLESS-DOCS** | Đồng bộ hóa toàn diện tài liệu kiến trúc sang mô hình 100% Endless Virtual Rail Shooter & Deep Scoring Engine | [`README.md`](file:///d:/3D-Shooter-master/README.md), [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md), [`LEVEL_DESIGN_SPECIFICATION.md`](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md), [`STAGE_1_DESIGN_DOCUMENT.md`](file:///d:/3D-Shooter-master/STAGE_1_DESIGN_DOCUMENT.md) | `0 Errors` | User (Yêu cầu cập nhật) |
| *12/10/2026* | **DECOUPLE-EVASION** | Tách rời 100% tốc độ né ngang/dọc (60 m/s & 42 m/s) khỏi tốc độ tiến Z, tắt Golden Ratio, bảo toàn độ trễ lò xo 0.1s êm ái khi Boost/DDA, cung cấp cặp MenuItem 51 & 52 | [`DefaultVirtualRailConfig.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/DefaultVirtualRailConfig.asset), [`VirtualRailConfig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailConfig.cs), [`VirtualRailModeToggle.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailModeToggle.cs) | `0 Errors` | User (Thực hiện phương án 1) |
| *12/10/2026* | **BASE-SPEED-70** | Đồng bộ hóa tốc độ tiến cơ sở Z ban đầu về 70 m/s trên toàn hệ thống (StageData_01, DefaultConfig, EndlessDirector fallback, HUD Speedometer, AimAssist, SmartBomb) | [`StageData_01.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/StageData_01.asset), [`DefaultVirtualRailConfig.asset`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/DefaultVirtualRailConfig.asset), [`VirtualRailConfig.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailConfig.cs), [`VirtualRailEndlessDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailEndlessDirector.cs), [`ModernCockpitHUD.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/ModernCockpitHUD.cs) | `0 Errors` | User (Yêu cầu tốc độ Z ban đầu là 70) |
| *12/10/2026* | **BOSS-DECOUPLE-2.4** | Hoàn thành Task 2.4: Tách biệt Collider Khiên (+3.25m đệm Y) & Vỏ tàu, triệt tiêu bug tên lửa gây sát thương nổ lan 4X qua multi-hit suppression 0 GC, cân bằng và đồng bộ hóa giới hạn máu 3 pha (660 HP / 330 HP / 0 HP) | [`VirtualRailHomingMissile.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHomingMissile.cs), [`BossF3CorvetteController.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossF3CorvetteController.cs), [`BossHardpoint.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossHardpoint.cs), [`VirtualRailBossEditorTool.cs`](file:///d:/3D-Shooter-master/Assets/Editor/VirtualRailBossEditorTool.cs), [`Boss_F3_Corvette.prefab`](file:///d:/3D-Shooter-master/Assets/Prefabs/Boss_F3_Corvette.prefab) | `0 Errors` | User (Thực hiện phương án 1) |
| *12/10/2026* | **BOSS-NHIP-2** | Hoàn thành Nhịp 2 (Task 2.5 & 2.6): Tháp pháo cánh bắn Flak 1.8s ở Pha 2, Buồng lái kích hoạt Stun 4.0s ngắt chiêu, SFX nổ lớn & VFX & Điểm thưởng (+2.5k/+5k/+10k), Pha 3 Hyper Laser 11 tia quét trung tâm 55 m/s, liên kết Endless Director nâng Threat Level khi diệt Boss | [`BossF3CorvetteController.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/BossF3CorvetteController.cs), [`VirtualRailEndlessDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailEndlessDirector.cs), [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) | `0 Errors` | User (Thực hiện nhịp 2) |
| *12/10/2026* | **DDA-BULLET-CADENCE** | Chuyển đổi DDA: Ghim cố định tốc độ đạn quái ở mức chuẩn cơ sở 20 m/s (không tăng theo cấp), thay bằng tăng tần suất bắn đạn (-8% cooldown/cấp) với trần hãm kép (tối đa 2.0x, sàn tuyệt đối 0.60s); áp dụng tự động 100% trong game loop | [`EnemyAttack.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/EnemyAttack.cs), [`VirtualRailWaveSpawner.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWaveSpawner.cs), [`VirtualRailEndlessDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailEndlessDirector.cs), [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md) | `0 Errors` | User (Yêu cầu áp dụng trực tiếp) |

---

## VIII. HƯỚNG DẪN RA LỆNH THỰC THI CHO NGƯỜI DÙNG (COMMAND QUICK REFERENCE)

Khi bạn muốn bắt đầu triển khai bất kỳ hạng mục nào, chỉ cần ra lệnh bằng các câu lệnh mẫu sau:
* *"Bắt đầu triển khai Task 2.4"* $\implies$ AI sẽ lập phương án chi tiết tách biệt Collider Khiên & Vỏ tàu, sửa bug splash damage 4x.
* *"Bắt đầu triển khai Task 2.5"* $\implies$ AI tập trung khắc phục lỗi tháp pháo đóng băng và kết nối điểm yếu buồng lái `_mainBridge`.
* *"Bắt đầu triển khai Task 2.6"* $\implies$ AI tập trung hoàn thiện Pha 3 cuồng nộ và kết nối sự kiện hạ Boss với vòng lặp Endless.
* *"Bắt đầu triển khai Giai đoạn 3"* $\implies$ AI tập trung thiết kế HUD Endless (Multiplier 32x, Graze, Threat Level).
