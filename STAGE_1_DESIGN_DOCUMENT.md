# TÀI LIỆU THIẾT KẾ KỊCH BẢN CHU KỲ KHỞI ĐẦU: OPERATION METEOR BREACH
**(STARTER BIOME & ONBOARDING RUN - CYCLE 1 SPECIFICATION v2.0.0)**

> **Phiên bản:** v2.0.0 (Endless Virtual Rail Standard)  
> **Thể loại:** On-Rail 3D Space Shooter (Chuẩn Star Fox 64 Arcade & Endless Survival)  
> **Vai trò kiến trúc:** Chu kỳ nhập môn & khởi tạo nhịp độ (Cycle 1 Initiation) trong Vòng lặp Sinh tồn Vô hạn  
> **Thời lượng mục tiêu:** $\approx 4.5\text{ Phút}$ hành lang ($270\text{ giây}$) + Đột kích Trùm Corvette F3  
> **Vận tốc đường ray cơ sở ($V_{rail}$):** $28\text{ m/s}$ (Tăng tốc lên $35\text{ m/s}$ ở đoạn bão đá và hãm về $10\text{ m/s}$ khi đấu Boss)  
> **Đỉnh cao trào (Climax Incursion):** Chiến Hạm Tuần Duyên Liên Bang **Corvette F3 ("Crimson Titan")**  
> **Tài liệu tham chiếu:** [`ENDLESS_SCORING_SPECIFICATION.md`](file:///d:/3D-Shooter-master/ENDLESS_SCORING_SPECIFICATION.md) • [`LEVEL_DESIGN_SPECIFICATION.md`](file:///d:/3D-Shooter-master/LEVEL_DESIGN_SPECIFICATION.md) • [`MASTER_ROADMAP_TRACKER.md`](file:///d:/3D-Shooter-master/MASTER_ROADMAP_TRACKER.md)

---

## I. TRIẾT LÝ THIẾT KẾ CỐT LÕI (CORE DESIGN PHILOSOPHY)

Trong kiến trúc trò chơi Sinh Tồn Vô Hạn (Endless Virtual Rail Shooter), Chu kỳ 1 (Starter Biome: Meteo Asteroid Belt) đóng vai trò là **Hành Lang Khởi Động Hưng Phấn & Xây Dựng Hệ Số Nhân (Gateway & Multiplier Builder)**:
1. **Thiên Thạch (Hazards) - Ngẫu Nhiên Có Kiểm Soát (Bounded Randomness):**
   * Đóng vai trò là chướng ngại tự nhiên và bài kiểm tra phản xạ.
   * Spawn ngẫu nhiên tọa độ $(X, Y)$, kích thước và tốc độ xoay trục, nhưng **luôn bảo đảm chừa lại một hành lang an toàn (Safe Corridor)** theo thuật toán Golden Path Raycast để người chơi lách tàu an toàn, không bao giờ tạo thành bức tường bít kín $100\%$.
2. **Kẻ Địch (Enemies) - Biên Đạo Thủ Công Kết Hợp DDA (Choreographed Formations):**
   * Từng đợt quái xuất hiện theo quỹ đạo Bézier định sẵn: Hướng vào (Vector), Đội hình (Formation), Số lượng quái và Khoảng nghỉ nhặt vật phẩm.
   * **Cơ Chế Thưởng Dọn Sạch Đội Hình (Squad Clear Bonus):** Bắn hạ $100\%$ số quái trong một phi đội trước khi chúng thoát ly sẽ kích hoạt rơi Vòng Năng Lượng, Bom Smart Bomb hoặc tăng ngay $+1\text{ Cấp Multiplier}$!
3. **Mục Tiêu Tác Chiến Của Chu Kỳ 1:**
   * Cho phép người chơi làm quen với hệ thống hồng tâm kép (Dual Reticle), thao tác lộn cánh (Barrel Roll) và sạc khiên lục giác (Hex Shield).
   * Tạo điều kiện tích lũy hệ số nhân điểm số lên mức cao ($16\times \to 32\times$) và nâng cấp vũ khí lên Twin Laser trước khi đối đầu với đợt Đột kích Trùm đầu tiên.

---

## II. LỘ TRÌNH 5 PHÂN KHU NHỊP ĐỘ (PACING & EMOTIONAL CURVE)

```mermaid
journey
    title BIỂU ĐỒ NHỊP ĐỘ CHU KỲ 1 (STARTER BIOME RUN)
    section 00:00 - 01:00
      Xuất Kích & Thăm Dò: 3: Người chơi
      Làm quen ngắm bắn & Nhặt vòng vàng: 4: Người chơi
    section 01:00 - 02:15
      Vành đai đá vụn & Đội hình chữ V: 6: Người chơi
      Tụ lực Charge Shot nổ lan x3 Multiplier: 7: Người chơi
    section 02:15 - 03:15
      Đột kích sau lưng (Rear Ambush): 8: Người chơi
      Lộn cánh Barrel Roll hất bão đạn: 9: Người chơi
    section 03:15 - 04:15
      Cao Trào: Bão thiên thạch & Phi đội tinh nhuệ: 10: Người chơi
      Kích nổ Smart Bomb dọn sạch màn hình: 10: Người chơi
    section 04:15 - Đấu Trùm
      ĐỘT KÍCH TRÙM: Corvette F3 ("Crimson Titan"): 10: Người chơi
      Hạ Trùm, Nhận +50K PTS & Warp vào Chu Kỳ 2: 9: Người chơi
```

---

## III. BẢNG BIÊN ĐẠO KỊCH BẢN CHI TIẾT TỪNG GIÂY (TIMELINE CHOREOGRAPHY)

### 1. PHÂN KHU 1: XUẤT KÍCH & THĂM DÒ (00:00 – 01:00 | $Z: 0 - 1,680\text{m}$)
* **Bối cảnh:** Không gian mở yên bình, nền chân trời hành tinh xanh 4K, hạt bụi sao trôi êm đềm.
* **Mật độ thiên thạch:** Rất thưa thớt (1 cụm mỗi $80\text{m}$), $100\%$ là loại bắn vỡ được trong 1 phát đạn.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **00:05s** | Radio Transmission | Thông điệp chỉ huy: *"Command: Arwing initialized. Entering perimeter."* | Giới thiệu bối cảnh, người chơi làm quen tay lái. |
| **00:15s** | **ĐỢT QUÁI 1: Tiên Phong** | • Hướng: `FrontalHeadOn` (Chính diện)<br>• Đội hình: Bay thẳng hàng<br>• Số lượng: **3 Scout** | Tập bắn tỉa bằng súng Laser cơ bản. |
| **00:30s** | Thu Thập Vật Phẩm | Chuỗi 3 Vòng Năng Lượng Vàng xếp uốn lượn hình sin. | Hướng dẫn luồn lách để hồi máu và sạc Khiên Lục Giác. |
| **00:45s** | **ĐỢT QUÁI 2: Cánh Én** | • Hướng: `LateralFlankingLeft` (Tạt sườn trái)<br>• Đội hình: Bay lượn chữ S<br>• Số lượng: **4 Flanker** | Tập ngắm đón đầu mục tiêu di chuyển ngang (Lead Aiming). |

---

### 2. PHÂN KHU 2: HẺM NỨT THIÊN THẠCH (01:00 – 02:15 | $Z: 1,680 - 3,780\text{m}$)
* **Bối cảnh:** Tàu bay sâu vào vùng đá vụn. Tỉ lệ xuất hiện thiên thạch bất hoại Monolith là $20\%$.
* **Mật độ thiên thạch:** Trung bình (1 cụm mỗi $55\text{m}$), bắt đầu có đá xoay chậm chắn đường.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **01:05s** | Vật cản Monolith đầu tiên | 1 Khối đá khổng lồ Monolith trôi chậm lệch phải. | Buộc người chơi nghiêng cánh ($Z/R$ Banking) lách sang trái. |
| **01:20s** | **ĐỢT QUÁI 3: Mũi Tên Đột Kích** | • Hướng: `OverheadDiveBomb` (Lao từ trần xuống)<br>• Đội hình: Chữ V (Wedge V)<br>• Số lượng: **1 Leader + 4 Scout (5 chiếc)** | Thử thách giữ nút bắn tụ lực **Charge Shot** để bắn nổ lan cả đàn 5 chiếc. Quét sạch đàn $\implies$ Rơi 1 Vòng Vàng và tăng Combo Multiplier. |
| **01:45s** | Cụm Thủy Lôi Không Gian | 3 Quả mìn không gian trôi nổi giữa đường bay. | Bắn nổ 1 quả mìn $\implies$ Kích nổ dây chuyền phá sạch đá xung quanh. |

---

### 3. PHÂN KHU 3: BẪY ĐỘT KÍCH SAU LƯNG (02:15 – 03:15 | $Z: 3,780 - 5,460\text{m}$)
* **Bối cảnh:** Ánh sáng mặt trời bị che khuất một phần, bụi vũ trụ dày hơn.
* **Mật độ thiên thạch:** Xuất hiện xen kẽ giữa các khe nứt đá lớn.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **02:15s** | Radio Transmission | Cảnh báo: *"Alert: Hostile bogeys closing in on your tail!"* | Nhắc nhở người chơi quan sát phía sau. |
| **02:20s** | **ĐỢT QUÁI 4: Phục Kích Đuôi** | • Hướng: `RearAmbush` (Bay đuổi sau lưng, $V_{closing} = 70\text{m/s}$)<br>• Đội hình: 2 bên cánh Arwing<br>• Số lượng: **3 Interceptor** xả bão đạn đỏ. | Bài kiểm tra kỹ năng lộn cánh: Nhấp đúp **Barrel Roll** để tạo từ trường hất văng bão đạn. |
| **02:45s** | **ĐỢT QUÁI 5: Hộ Vệ Khiên** | • Hướng: `FrontalHeadOn`<br>• Đội hình: Quả trám (Diamond)<br>• Số lượng: **1 Shield Bearer + 2 Flanker** | Tập trung hỏa lực bắn sập khiên lục giác của Shield Bearer. Rơi 1 Quả Bom Smart Bomb! |

---

### 4. PHÂN KHU 4: CAO TRÀO BÃO ĐÁ & ÁT CHỦ BÀI (03:15 – 04:15 | $Z: 5,460 - 7,800\text{m}$)
* **Bối cảnh:** Vùng sụp đổ thiên thạch dày đặc nhất. Nhạc nền dồn dập đến cực điểm.
* **Vận tốc ray:** Tăng tốc lên $35\text{ m/s}$ (Tạo cảm giác bứt tốc nghẹt thở).

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **03:15s** | Radio Transmission | Cảnh báo đỏ: *"Warning: Meteor storm density critical! Full thrust engaged!"* | Tàu tăng tốc, chuyển sang trạng thái sinh tồn tốc độ cao. |
| **03:25s** | Thử thách lách bão đá | Các cột đá Monolith xuất hiện so le trái - phải liên tục. | Đảo cánh và lách liên hoàn ở tốc độ cao. |
| **03:40s** | **ĐỢT QUÁI 6: Phi Đội Át Chủ Bài** | • Hướng: Bủa vây từ 3 hướng (Trần + Trái + Phải)<br>• Đội hình: Gọng kìm<br>• Số lượng: **6 Interceptor** xả đạn dày đặc | **Cao trào giải phóng năng lượng:** Người chơi bấm nút thả **Smart Bomb**, sóng xung kích nổ tung toàn màn hình, bùng nổ combo điểm số $32\times$. |
| **04:10s** | Bão đá tan dần | Thiên thạch giảm đột ngột, còi báo động trùm vang dội. | Chuẩn bị bước vào trận chiến sinh tử với Trùm Tuần Duyên. |

---

### 5. PHÂN KHU 5: ĐỘT KÍCH TRÙM ĐỈNH ĐIỂM (APEX BOSS INCURSION: CORVETTE F3)
* **Bối cảnh:** Chiến hạm cự phách Corvette F3 ("Crimson Titan") hạ độ cao chặn đường bay. Vận tốc ray giảm về $10\text{ m/s}$.
* **Cơ chế 3 Pha của Boss:**
  * **Pha 1 (Bảo vệ Khiên & Hỏa lực Sườn):** Bắn sập khiên lục giác $300\text{ HP}$ ([`VirtualRailHexShield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHexShield.cs)) và tiêu diệt 2 tháp pháo cánh.
  * **Pha 2 (Hẻm Lò Phản Ứng Plasma):** Nhắm bắn khe nhiệt plasma cuộn chảy ([`VirtualRail_ThermalReactorTrench.shader`](file:///d:/3D-Shaders/VirtualRail_ThermalReactorTrench.shader)) để ngắt nhịp tích tụ năng lượng của Boss.
  * **Pha 3 (Quá Tải Lõi & Hyper Laser):** Luồn lách né tia laser quét $60\%$ màn hình, dứt điểm lõi trùm bằng toàn bộ đạn đôi và bom dự trữ!
* **Kết thúc Trận Đấu:**
  * Corvette F3 nổ tung dữ dội, dọn sạch toàn bộ đạn trên màn hình thành điểm sao.
  * Thưởng tức thì: $+50,000\text{ PTS} \times \mathbf{M}$.
  * Xuất hiện chuỗi 5 Vòng Hồi Phục, mở Cổng Warp Hyperspace Jump nhảy vọt sang **Chu Kỳ 2 (Quần Xã Tiếp Theo)** với Threat Level $k = 2$!

---

## IV. BẢNG TỔNG KẾT THÔNG SỐ CẤU HÌNH (SPEC MATRIX)

* **Tổng thời lượng hành lang Chu kỳ 1:** Đúng $270\text{ giây}$ ($4\text{ phút } 30\text{ giây}$) + Thời gian hạ Trùm ($\approx 60 - 90\text{ giây}$).
* **Tổng số quái hành lang:** $23\text{ chiếc}$ (chia làm 6 đợt có nhịp thở rõ ràng).
* **Trùm cuối chu kỳ:** Federation Corvette F3 "Crimson Titan" ($1,000\text{ HP}$ phân bổ 3 pha).
* **Tổng số bom Smart Bomb nhặt được:** Tối đa 2 quả (thưởng khi quét sạch đợt quái 3 và đợt quái 5).
* **Tỉ lệ thiên thạch Monolith trung bình:** $20\%$ (tăng dần từ $0\% \to 35\%$ ở đoạn cao trào).
* **Cơ chế chuyển tiếp:** Tiêu diệt Boss Corvette F3 $\implies$ Mở cổng Hyperspace Warp tự động sang Chu kỳ tiếp theo của hệ thống Endless.
