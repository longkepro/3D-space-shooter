# TÀI LIỆU THIẾT KẾ MÀN CHƠI (LEVEL DESIGN SPECIFICATION)
# STAGE 1: OPERATION METEOR BREACH

> **Phiên bản:** v1.0.0  
> **Thể loại:** On-Rail 3D Space Shooter (Chuẩn Star Fox 64 Arcade)  
> **Thời lượng mục tiêu:** 4.5 Phút ($270\text{ giây}$)  
> **Trạng thái Trùm:** Không có Trùm (Boss-less Stage - Cao trào bằng Bão thiên thạch & Phi đội tinh nhuệ)  
> **Vận tốc đường ray cơ sở ($V_{rail}$):** $28\text{ m/s}$ (Tăng tốc lên $35\text{ m/s}$ ở đoạn cao trào)  
> **Tổng quãng đường dự kiến ($Z$):** $\approx 7,800\text{ m}$  

---

## I. TRIẾT LÝ THIẾT KẾ CỐT LÕI (CORE DESIGN PHILOSOPHY)

Màn 1 đóng vai trò là **Màn Chơi Nhập Môn & Khởi Động Hưng Phấn (Gateway & Flow Initialization)**:
1. **Thiên thạch (Hazards) - Ngẫu nhiên có kiểm soát (Bounded Randomness):**
   * Đóng vai trò là chướng ngại tự nhiên và nhiễu không gian.
   * Spawn ngẫu nhiên tọa độ $(X, Y)$, kích thước và tốc độ xoay trục, nhưng **luôn bảo đảm chừa lại một hành lang an toàn (Safe Corridor)** để người chơi có kỹ năng né tránh, không bao giờ tạo thành bức tường bít kín $100\%$.
2. **Kẻ địch (Enemies) - Biên đạo thủ công 100% (Handcrafted Choreography):**
   * Tuyệt đối không spawn quái bừa bãi. Mỗi đợt xuất hiện đều được định sẵn: Hướng vào (Vector), Đội hình (Formation), Số lượng quái và Khoảng nghỉ nhặt vật phẩm.
   * **Cơ chế thưởng dọn sạch đợt (Squad Clear Bonus):** Bắn hạ $100\%$ số quái trong một phi đội trước khi chúng bay thoát sẽ kích hoạt rơi Vòng Năng Lượng hoặc Bom Smart Bomb.

---

## II. LỘ TRÌNH 4 PHÂN KHU NHỊP ĐỘ (PACING & EMOTIONAL CURVE)

```mermaid
journey
    title BIỂU ĐỒ NHỊP ĐỘ CỦA STAGE 1 (4.5 PHÚT)
    section 00:00 - 01:00
      Xuất Kích & Thăm Dò: 3: Người chơi
      Làm quen ngắm bắn & Nhặt vòng vàng: 4: Người chơi
    section 01:00 - 02:15
      Vành đai đá vụn & Đội hình chữ V: 6: Người chơi
      Tụ lực Charge Shot nổ lan: 7: Người chơi
    section 02:15 - 03:15
      Đột kích sau lưng (Rear Ambush): 8: Người chơi
      Lộn cánh Barrel Roll hất bão đạn: 9: Người chơi
    section 03:15 - 04:15
      CAO TRÀO: Bão thiên thạch & Phi đội tinh nhuệ: 10: Người chơi
      Kích nổ Smart Bomb dọn sạch màn hình: 10: Người chơi
    section 04:15 - 04:30
      Thoát ly & Nhảy vọt Hyperspace Warp: 9: Người chơi
```

---

## III. BẢNG BIÊN ĐẠO KỊCH BẢN CHI TIẾT TỪNG GIÂY (TIMELINE CHOREOGRAPHY)

### 1. PHÂN KHU 1: XUẤT KÍCH & THĂM DÒ (00:00 – 01:00 | $Z: 0 - 1,680\text{m}$)
* **Bối cảnh:** Không gian mở yên bình, nền chân trời hành tinh xanh 4K, hạt bụi sao trôi êm đềm.
* **Mật độ thiên thạch:** Rất thưa thớt (1 cụm mỗi $80\text{m}$), $100\%$ là loại bắn vỡ được trong 1 phát.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **00:05s** | Radio Transmission | Thông điệp chỉ huy: *"Command: Arwing initialized. Entering perimeter."* | Giới thiệu bối cảnh, người chơi làm quen tay lái. |
| **00:15s** | **ĐỢT QUÁI 1: Tiên Phong** | • Hướng: `FrontalHeadOn` (Chính diện)<br>• Đội hình: Bay thẳng hàng<br>• Số lượng: **3 Scout** | Tập bắn tỉa bằng súng Laser cơ bản. |
| **00:30s** | Thu Thập Vật Phẩm | Chuỗi 3 Vòng Năng Lượng Vàng xếp uốn lượn hình sin. | Hướng dẫn luồn lách để hồi máu và tăng Shield. |
| **00:45s** | **ĐỢT QUÁI 2: Cánh Én** | • Hướng: `LateralFlankingLeft` (Tạt sườn trái)<br>• Đội hình: Bay lượn chữ S<br>• Số lượng: **4 Flanker** | Tập ngắm đón đầu mục tiêu di chuyển ngang. |

---

### 2. PHÂN KHU 2: HẺM NỨT THIÊN THẠCH (01:00 – 02:15 | $Z: 1,680 - 3,780\text{m}$)
* **Bối cảnh:** Tàu bay sâu vào vùng đá vụn. Tỉ lệ xuất hiện thiên thạch bất hoại Monolith là $20\%$.
* **Mật độ thiên thạch:** Trung bình (1 cụm mỗi $55\text{m}$), bắt đầu có đá xoay chậm chắn đường.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **01:05s** | Vật cản Monolith đầu tiên | 1 Khối đá khổng lồ Monolith trôi chậm lệch phải. | Buộc người chơi nghiêng cánh ($Z/R$ Banking) lách sang trái. |
| **01:20s** | **ĐỢT QUÁI 3: Mũi Tên Đột Kích** | • Hướng: `OverheadDiveBomb` (Lao từ trần xuống)<br>• Đội hình: Chữ V (Wedge V)<br>• Số lượng: **1 Leader + 4 Scout (5 chiếc)** | Thử thách giữ nút bắn tụ lực **Charge Shot** để bắn nổ lan cả đàn 5 chiếc. Quét sạch đàn $\implies$ Rơi 1 Vòng Vàng. |
| **01:45s** | Cụm Thủy Lôi Không Gian | 3 Quả mìn không gian trôi nổi giữa đường bay. | Bắn nổ 1 quả mìn $\implies$ Kích nổ dây chuyền phá sạch đá xung quanh. |

---

### 3. PHÂN KHU 3: BẪY ĐỘT KÍCH SAU LƯNG (02:15 – 03:15 | $Z: 3,780 - 5,460\text{m}$)
* **Bối cảnh:** Ánh sáng mặt trời bị che khuất một phần, bụi vũ trụ dày hơn.
* **Mật độ thiên thạch:** Xuất hiện xen kẽ giữa các khe nứt đá lớn.

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **02:15s** | Radio Transmission | Cảnh báo: *"Alert: Hostile bogeys closing in on your tail!"* | Nhắc nhở người chơi quan sát phía sau. |
| **02:20s** | **ĐỢT QUÁI 4: Phục Kích Đuôi** | • Hướng: `RearAmbush` (Bay đuổi sau lưng, $V_{closing} = 70\text{m/s}$)<br>• Đội hình: 2 bên cánh Arwing<br>• Số lượng: **3 Interceptor** xả bão đạn đỏ. | Bài kiểm tra kỹ năng lộn cánh: Nhấp đúp **Barrel Roll** để tạo từ trường hất văng bão đạn. |
| **02:45s** | **ĐỢT QUÁI 5: Hộ Vệ Khiên** | • Hướng: `FrontalHeadOn`<br>• Đội hình: Quả trám (Diamond)<br>• Số lượng: **1 Shield Bearer + 2 Flanker** | Tập trung hỏa lực bắn sập khiên lục giác của Shield Bearer. |

---

### 4. PHÂN KHU 4: CAO TRÀO BÃO ĐÁ & THOÁT LY (03:15 – 04:30 | $Z: 5,460 - 7,800\text{m}$)
* **Bối cảnh:** Vùng sụp đổ thiên thạch dày đặc nhất. Nhạc nền dồn dập đến cực điểm.
* **Vận tốc ray:** Tăng tốc lên $35\text{ m/s}$ (Tạo cảm giác bứt tốc nghẹt thở).

| Mốc Giây | Sự Kiện Kịch Bản | Chi Tiết Biên Đạo & Đội Hình | Mục Đích Gameplay |
| :---: | :--- | :--- | :--- |
| **03:15s** | Radio Transmission | Cảnh báo đỏ: *"Warning: Meteor storm density critical! Full thrust engaged!"* | Tàu tăng tốc, chuyển sang trạng thái sinh tồn tốc độ cao. |
| **03:25s** | Thử thách lách bão đá | Các cột đá Monolith xuất hiện so le trái - phải liên tục. | Đảo cánh và lách liên hoàn ở tốc độ cao. |
| **03:40s** | **ĐỢT QUÁI 6: Phi Đội Át Chủ Bài** | • Hướng: Bủa vây từ 3 hướng (Trần + Trái + Phải)<br>• Đội hình: Gọng kìm<br>• Số lượng: **6 Interceptor** xả đạn dày đặc | **Cao trào giải phóng năng lượng:** Người chơi bấm nút thả **Smart Bomb**, sóng xung kích nổ tung toàn màn hình, combo điểm số $10\text{x}$. |
| **04:15s** | Kết thúc bão đá | Không còn quái vật, đá thưa dần, cổng Warp xuất hiện phía trước. | Giải tỏa căng thẳng, hít thở sâu. |
| **04:20s** | **Hyperspace Warp Jump** | Toàn màn hình lóe sáng hiệu ứng Warp Speed trong $2.5\text{s}$. | Chuyển tiếp mượt mà sang **Stage 2**. |

---

## IV. BẢNG TỔNG KẾT THÔNG SỐ CẤU HÌNH (SPEC MATRIX)

* **Tổng thời lượng:** Đúng $270\text{ giây}$ ($4\text{ phút } 30\text{ giây}$).
* **Tổng số quái xuất hiện:** $23\text{ chiếc}$ (chia làm 6 đợt có nhịp thở rõ ràng).
* **Tổng số bom Smart Bomb nhặt được:** Tối đa 2 quả (thưởng khi quét sạch đợt quái 3 và đợt quái 5).
* **Tỉ lệ thiên thạch Monolith trung bình:** $20\%$ (tăng dần từ $0\% \rightarrow 35\%$ ở đoạn cao trào).
* **Cơ chế Climax:** `StageClimaxType.TimeElapsed` $\rightarrow$ Tự động mở cổng Warp khi hết $270\text{s}$.
