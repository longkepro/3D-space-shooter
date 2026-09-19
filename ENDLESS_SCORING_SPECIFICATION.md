# TÀI LIỆU THIẾT KẾ KỸ THUẬT: CHẾ ĐỘ CHƠI VÔ HẠN VÀ HỆ THỐNG TÍNH ĐIỂM CHUYÊN SÂU
**(ENDLESS PROCEDURAL CORRIDOR & DEEP SCORING ARCHITECTURE - TDD v2.0.0)**

> **Thể loại:** 3D Sci-Fi Virtual Rail Shooter  
> **Định dạng kiến trúc:** Vận hành vô hạn (Endless Mode / High-Score Chaser)  
> **Trọng tâm:** Giải thuật sinh đường ray thủ tục, bảo chứng hình học khả thi, điều chỉnh độ khó động (DDA) và động cơ tính điểm rủi ro - phần thưởng.  
> **Hiệu lực tối cao:** Nguồn Chân Lý Phát Triển (Source of Truth for Endless High-Score Architecture).

---

## I. KIẾN TRÚC SINH ĐỊA HÌNH THỦ TỤC & ĐƯỜNG RAY VÔ HẠN

Trong chế độ vô hạn, màn chơi không còn là một file dữ liệu tĩnh được đặt sẵn trong engine. Nó đòi hỏi một hệ thống sinh đường ray theo thời gian thực (Procedural Spline Generation) vừa đảm bảo sự biến hóa đa dạng, vừa loại bỏ hoàn toàn các lỗi phối cảnh hoặc góc chết vật lý.

```text
[Đoạn Đang Chơi: Active Chunks] ──► [Đoạn Đang Sinh: Ahead Buffer] ──► [Kiểm Định Hình Học]
           │                                                                  │
           ▼                                                                  ▼
 [Thu Hồi: Object Pool]                                            [Bảo Chứng Khả Thi: PASS]
```

### 1.1. Quy chuẩn Khối ray Ghép nối (Spline Chunking & Hermite Continuity)

Đường ray vô hạn được chia thành các đoạn tiêu chuẩn gọi là **Spline Chunks** có chiều dài cung cố định ($L_{chunk} \approx 400\text{m} - 600\text{m}$, tương đương $8 - 12\text{ giây}$ bay).

* **Bộ nhớ đệm đường ray (Look-ahead Buffer):** Hệ thống luôn duy trì tối thiểu **3 Chunks** trong bộ nhớ:
  * *Chunk $N-1$ (Phía sau):* Nơi các mảnh vỡ và thực thể đang hoàn tất pha thoát ly để thu hồi về Object Pool.
  * *Chunk $N$ (Hiện tại):* Vị trí người chơi đang trực tiếp điều khiển phi thuyền tác chiến.
  * *Chunk $N+1$ (Phía trước):* Đã được nướng xong dữ liệu hình học, bắt đầu kích hoạt tính toán điểm sinh của đợt quái tiếp theo.

* **Điều kiện trơn hình học cấp 2 ($C^2$ Continuity):**
  Để camera không bị khựng hoặc lật hướng đột ngột tại điểm nối giữa Chunk $N$ và Chunk $N+1$, hai đầu mút ghép phải đồng nhất tuyệt đối 3 yếu tố:
  1. *Tọa độ tiếp giáp ($C^0$):* Điểm cuối của Chunk $N$ trùng khớp điểm đầu Chunk $N+1$.
  2. *Vector tiếp tuyến ($C^1$):* Đạo hàm bậc nhất đồng hướng, đảm bảo vận tốc và hướng nhìn camera giữ nguyên vector tiến.
  3. *Độ cong và Pháp tuyến ($C^2$):* Đạo hàm bậc hai liên tục, đảm bảo góc nghiêng tự động của ray (Track Banking) và gia tốc ly tâm chuyển tiếp mượt mà, không giật khung hình.

---

### 1.2. Giải thuật Bảo chứng Khả thi Hình học (Solvability Verification)

Lỗi chí mạng của các hệ thống sinh thủ tục (Procedural Generation) là tạo ra các "Bức tường chết" (Unwinnable Scenarios) — tình huống mà người chơi dù phản xạ hoàn hảo vẫn không thể né tránh do chướng ngại vật bịt kín mọi khe hở.

* **Hành lang Thoát hiểm Ảo (Golden Path Raycast):**
  Trước khi một Chunk được phê duyệt hiển thị, hệ thống chạy một thuật toán quét trước (Pre-computation):
  * Mô phỏng một hình hộp chữ nhật có kích thước bằng sải cánh tàu cộng với biên an toàn ($W_{ship} \times H_{ship} \times 1.5$) di chuyển dọc theo Chunk.
  * Dùng giải thuật tìm đường trong không gian 3D (dựa trên giới hạn góc thoát hiểm $\theta \approx 26.5^\circ$ đã thiết lập ở tỷ lệ vận tốc $1:2$).
  * Nếu phát hiện tại bất kỳ mặt cắt vuông góc nào trên trục ray mà diện tích khe hở khả dụng bị triệt tiêu hoàn toàn, hệ thống lập tức loại bỏ (Discard) cấu hình vật cản đó và sinh lại một seed mới chỉ trong vài phần nghìn giây.

---

### 1.3. Triệt tiêu Sai số Dấu phẩy động (Floating Origin & World Space Wrapping)

Khi người chơi bay liên tục trong chế độ vô hạn đạt cự ly $S > 10,000\text{ mét}$, tọa độ thế giới (World Coordinates) vượt quá giới hạn biểu diễn chính xác của số thực 32-bit (Single-precision Float). Hiện tượng này sẽ gây ra:
* Rung lắc camera (Camera Jitter).
* Va chạm vật lý bị xuyên thấu hoặc tính toán sai lệch.
* Tia ngắm Reticle bị giật điểm ảnh.

**Giải pháp Quy chuẩn: Tái định vị Thế giới (World Origin Shifting):**
* Khi quãng đường tích lũy đạt mỗi mốc chu kỳ $4,000\text{ mét}$, hệ thống kích hoạt một sự kiện ngầm trong đúng 1 frame:
* Dịch chuyển toàn bộ thực thể sống (Tàu người chơi, Camera, Kẻ địch, Đạn đạo, Điểm Spline hiện hành) ngược về gốc tọa độ $(0, 0, 0)$ của không gian thế giới:

$$\vec{P}_{entity\_new} = \vec{P}_{entity\_old} - \vec{P}_{rail\_anchor}$$

* Biến đo quãng đường tổng ($S_{total}$) và bảng điểm được lưu trữ bằng một biến số thực 64-bit (`double`) độc lập, hoàn toàn không bị ảnh hưởng bởi phép dịch chuyển tọa độ thế giới cục bộ này.

---

## II. HỆ THỐNG ĐIỀU CHỈNH ĐỘ KHÓ ĐỘNG (DYNAMIC DIFFICULTY ADJUSTMENT - DDA)

Tăng độ khó vô hạn **không thể** thực hiện bằng cách đẩy tốc độ bay lên mãi mãi. Giới hạn phản xạ thần kinh của mắt và tay người chơi đạt ngưỡng bão hòa ở khoảng $150\text{ms} - 200\text{ms}$. Nếu vượt qua ngưỡng này, trò chơi sẽ trở thành bài toán may rủi.

Độ khó vô hạn được kiểm soát bằng **Mô hình Sóng Răng cưa Tăng tiến (Cyclical Escalation Architecture)**:

```text
Cường độ Căng thẳng (Stress Intensity)
 100% ▲                         [ĐỈNH CHU KỲ 2]
      │                               ▲
  80% │             [ĐỈNH CHU KỲ 1]  / \
      │                   ▲         /   \
  50% │       /\         / \       /     \   [HỒI SỨC]
      │      /  \  [HỒI] /   \    /       \─────*─────► (Chu kỳ 3...)
  20% │ [ĐÁY]    \───*──/     \──/
   0% ┴────────────────────────────────────────────────► Thời gian / Cự ly
      0km       2km    3km   5km 6km     8km   9km
```

### 2.1. Chu kỳ Răng cưa 3 Pha (The Tri-Phase Loop)

Mỗi chu kỳ sinh tồn kéo dài từ **$2.5\text{km}$ đến $3.5\text{km}$** (khoảng $60 - 80\text{ giây}$), bao gồm:

1. **Pha Leo dốc (Ramp Phase - $60\%$ thời lượng chu kỳ):**
   Tăng dần mật độ đạn, tốc độ tiếp cận của kẻ địch và tần suất xuất hiện các chướng ngại vật cắt ngang. Áp lực tăng tuyến tính.
2. **Pha Đỉnh điểm (Climax Encounter - $25\%$ thời lượng chu kỳ):**
   Kích hoạt tình huống nghẹt thở: Hành lang bị bóp hẹp kết hợp sự xuất hiện của một tàu Chiến hạm Mini-boss hoặc bầy Interceptor phóng ra từ phía sau lưng.
3. **Pha Thư giãn & Thu hoạch (Breather / Restock - $15\%$ thời lượng chu kỳ):**
   Ngay sau đỉnh điểm, mọi áp lực giao tranh lập tức biến mất. Màn chơi mở ra một khoảng không gian rộng lớn, xuất hiện dày đặc các chuỗi vòng nhẫn điểm số, vật phẩm hồi máu và nâng cấp. Pha này giúp nhịp tim người chơi hạ xuống mức an toàn trước khi bước vào chu kỳ kế tiếp.

---

### 2.2. Ma trận Tham số Leo thang theo Chu kỳ (Cycle Parameter Scaling)

Sau mỗi chu kỳ vượt qua thành công, "mức đáy" và "mức đỉnh" của chu kỳ tiếp theo được tịnh tiến lên theo ma trận toán học có kiểm soát:

| Tham số Hệ thống | Công thức Tăng tiến theo Chu kỳ ($k$) | Giới hạn Cực đại (Hard Cap) |
| :--- | :--- | :--- |
| **Vận tốc tiến cơ sở ($V_z$)** | $V_z(k) = V_{base} \cdot (1 + 0.05 \cdot k)$ | Tối đa $75\text{ m/s}$ ($+50\%$ so với gốc) |
| **Vận tốc cơ động ($V_x, V_y$)** | Tự động tăng theo tỷ lệ vàng $1 : 2$ so với $V_z$ | Đồng bộ hóa theo $V_z$ |
| **Ngân sách Mối nguy (Threat Budget)** | $B(k) = B_{base} \cdot (1.12)^k$ | Giới hạn tối đa $24$ điểm đe dọa cùng frame |
| **Bán kính Phễu Từ tính ($R_{mag}$)** | $R_{mag}(k) = R_{base} \cdot (0.92)^k$ | Thu hẹp tối thiểu còn $0.8\text{ mét}$ |
| **Thời gian Tồn tại Cửa sổ Né đạn** | Giảm dần thời gian trễ trước khi đạn nổ | Tối thiểu $0.8\text{ giây}$ phản xạ |

---

### 2.3. Cơ chế Ngân sách Nguy hiểm (Threat Budgeting System)

Để tránh việc máy tính tự động dồn quá nhiều loại lính nguy hiểm vào một thời điểm, hệ thống gán cho mỗi loại kẻ địch một mức "Điểm Đe dọa":
* *Drone trinh sát bay thẳng (Fodder):* 1 Điểm.
* *Tiêm kích tạt sườn (Flanker):* 3 Điểm.
* *Tàu bổ nhào thả bom (Dive-bomber):* 4 Điểm.
* *Chiến hạm hạng nặng (Heavy Gunship):* 8 Điểm.

Tại mỗi mét trên đường ray, tổng điểm đe dọa của các thực thể đang hoạt động không bao giờ được phép vượt quá trần **Ngân sách $B(k)$** của chu kỳ hiện tại. Điều này đảm bảo nhịp độ giao tranh luôn có tính toán, không trở thành một mớ hỗn loạn không thể giải quyết.

---

## III. ĐỘNG CƠ TÍNH ĐIỂM SÂU (ADVANCED SCORING ENGINE)

Trong một tựa game vô hạn, sinh tồn chỉ là điều kiện cần; **Tối đa hóa Điểm số (Score Maximization)** mới là điều kiện đủ để giữ chân người chơi cạnh tranh thứ hạng (Leaderboards).

Điểm số được xây dựng dựa trên triết lý cốt lõi: **Rủi ro càng lớn, Điểm số càng bùng nổ (High Risk = Exponential Reward).**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                            TỔNG ĐIỂM CUỐI CÙNG                              │
│  = [ Điểm Quãng Đường + Điểm Diệt Địch + Điểm Lướt Cận Kề ] × Hệ Số Multiplier │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.1. Hệ số Nhân Tích lũy (Dynamic Combo Multiplier)

Hệ số nhân ($\mathbf{M}$) là biến số quan trọng nhất quyết định một lượt chạy đạt điểm thường hay điểm kỷ lục:
* Hệ số bắt đầu từ $1\times$, tăng dần qua các mốc: $2\times \to 4\times \to 8\times \to 16\times \to \mathbf{32\times}$ (Cực đại).
* **Điều kiện Tăng bậc Multiplier:**
  * Tiêu diệt trọn vẹn một đợt lính (Squadron Wipeout).
  * Thực hiện thành công chuỗi bắn tụ lực nổ lan (Charge Shot Multikill).
  * Thu thập đủ chuỗi 5 Nhẫn Vàng (Gold Ring Chain) không bị đứt đoạn.
* **Thanh Thời gian Duy trì (Multiplier Decay Gauge):**
  * Hệ số nhân không tồn tại vĩnh viễn. Một thanh năng lượng sẽ liên tục cạn kiệt trong vòng **$3.5\text{ giây}$**.
  * Để duy trì hệ số, người chơi buộc phải liên tục thực hiện hành động tạo áp lực: bắn trúng mục tiêu, tiêu diệt địch hoặc kích hoạt cơ chế lướt sát (Graze).
  * Nếu thanh năng lượng chạm đáy $\implies$ Multiplier bị tụt ngay một bậc ($32\times \to 16\times$) và thời gian hồi quy về đầu bậc đó.

---

### 3.2. Cơ chế Thưởng Lướt Cận kề (Graze / Near-Miss Mechanic)

Đây là cơ chế kỹ năng cao kế thừa từ thể loại Bullet Hell (Danmaku), khuyến khích người chơi không chỉ né tránh đạn mà phải né ở cự ly **sát sạt**.

```text
                           [Vùng Graze: R = 1.8m]
                       ┌────────────────────────────┐
                       │    [Hitbox Tàu: R = 0.5m]  │
                       │          ┌──────┐          │
[Viên Đạn Bay Qua] ────┼─────────►│ TÀU  │          │ (KHÔNG MẤT MÁU!)
                       │          └──────┘          │
                       │                            │ ──► KÍCH HOẠT:
                       └────────────────────────────┘     • +500 Điểm Thưởng
                                                          • Đầy lại Thanh Multiplier
                                                          • Âm thanh Rít Quang Học
```

* **Cấu trúc Hitbox Lồng ghép (Nested Hitboxes):**
  * *Lõi Va Chạm Vật Lý (Inner Hurtbox - Bán kính $0.5\text{m}$):* Nếu đạn chạm vào lõi này $\implies$ Tàu nhận sát thương, gãy Combo, reset Multiplier về $1\times$.
  * *Vỏ Lướt Cận Kề (Outer Graze Box - Bán kính $1.8\text{m}$):* Nếu viên đạn hoặc chướng ngại vật đi xuyên qua lớp vỏ này mà không chạm vào lõi trong:
    1. Người chơi không mất máu.
    2. Kích hoạt hiệu ứng âm thanh xé gió (Graze Sizzle) và tia lửa điện quang học dọc thân cánh.
    3. Thưởng điểm số tức thời: $+500\text{ Điểm} \times \mathbf{M}$.
    4. Hồi phục ngay lập tức $30\%$ dung lượng thanh thời gian Multiplier Decay.

* **Ý nghĩa Game Design:** Một người chơi đẳng cấp sẽ chủ động lách tàu vào giữa các chùm đạn dày đặc để "vắt kiệt" điểm số thay vì chọn lộ trình bay an toàn ở góc màn hình.

---

### 3.3. Cổng Thách Thức Kỹ Năng Rủi Ro (Risk Gates & High-Risk Lanes)

Dọc theo hành lang vô hạn, hệ thống thủ tục thỉnh thoảng sẽ sinh ra các **Cổng Rủi Ro**:
* Một cấu trúc hẹp xuất hiện (ví dụ: khe nứt giữa hai piston khổng lồ đang dập mạnh, hoặc khe hẹp sát mặt dung nham).
* Đường bay chính an toàn nằm ở phía trên, nhưng bên trong khe hẹp chứa **Huy hiệu Điểm Bạch kim (Platinum Medals)**.
* Nếu người chơi dám phanh tốc độ, lộn cánh $90^\circ$ luồn qua khe piston:
  * Nhận lượng điểm thưởng tương đương tiêu diệt cả một bầy tàu địch.
  * Tự động khóa thanh Multiplier không bị tụt trong $10\text{ giây}$ tiếp theo.

---

## IV. BẢNG MA TRẬN PHÂN BỔ ĐIỂM SỐ CHI TIẾT

| Hành vi Tác chiến | Điểm Cơ sở (Base) | Tác động lên Multiplier | Đánh giá Mức độ Rủi ro |
| :--- | :--- | :--- | :--- |
| **Duy trì quãng đường sinh tồn** | $10\text{ Điểm / mét}$ | Không tác động | Tối thiểu (Bị động) |
| **Bắn hạ lính thường bằng đạn đơn** | $100\text{ Điểm}$ | Duy trì thanh thời gian | Rất thấp |
| **Bắn hạ Tàu Chỉ huy (Scatter đàn)** | $1,000\text{ Điểm}$ | $+1$ Nấc thanh thời gian | Trung bình |
| **Charge Shot diệt cả đội hình (5 con)** | $5,000\text{ Điểm}$ | Tăng ngay $+1\text{ Cấp Multiplier}$ | Trung bình - Cao |
| **Kích hoạt Lướt Cận kề (Graze)** | $500\text{ Điểm / đạn}$ | Hồi $30\%$ thanh thời gian | Cao |
| **Phản xạ hất đạn (Barrel Roll Reflect)** | $800\text{ Điểm / đạn}$ | Đẩy đạn dội ngược diệt địch | Cao |
| **Luồn lách Cổng Rủi Ro (Risk Gate)** | $10,000\text{ Điểm}$ | Đóng băng Multiplier $10\text{s}$ | Cực đại (Cận kề tử vong) |

---

## V. ĐỘT KÍCH TRÙM THỦ TỤC & CHUYỂN TIẾP QUẦN XÃ (BIOME TRANSITION)

Để giữ cho chế độ vô hạn không bị đơn điệu về mặt thị giác sau 15–20 phút chơi liên tục, trò chơi sử dụng kỹ thuật **Chuyển tiếp Quần xã Liền mạch (Seamless Biome Transition)** kết hợp các đợt Đột kích Trùm (Incursions).

```text
[QUẦN XÃ A: VŨ TRỤ] ──► [HẦM SÂU / CỔNG WARP] ──► [QUẦN XÃ B: HẺM HÀNH TINH]
         ▲                       ▲                            ▲
      Chu kỳ 1-3             Giao tranh Trùm               Chu kỳ 4-6
```

1. **Cấu trúc Cổng Warp / Hầm Chuyển Tiếp (Transition Conduit):**
   * Sau mỗi 3 chu kỳ (khoảng $10\text{km}$), đường ray Spline dẫn thẳng vào một đường ống năng lượng khổng lồ hoặc một tầng mây bão dày đặc.
   * Trong đường ống này, toàn bộ texture môi trường của Quần xã cũ được dỡ khỏi bộ nhớ (Unload), và bộ asset của Quần xã mới (ví dụ: chuyển từ Vũ trụ tối sang Hành tinh dung nham rực lửa) được nạp vào RAM.

2. **Đột kích Trùm Ngẫu nhiên (Procedural Boss Incursions):**
   * Trận đấu Trùm diễn ra ngay trong quá trình chuyển tiếp quần xã.
   * Boss không xuất hiện với thanh máu cố định theo cốt truyện, mà là một cỗ máy chiến tranh dạng module được lắp ráp ngẫu nhiên các bộ phận: thân chính kết hợp ngẫu nhiên với 2 mô hình cánh laser hoặc 2 khoang phóng tên lửa.
   * **Áp lực Thời gian Thực (Enrage Timer):** Người chơi có đúng $45\text{ giây}$ để tiêu diệt Trùm. Nếu hết thời gian mà chưa diệt được, Trùm sẽ kích hoạt động cơ phản lực vọt mất dạng, người chơi mất cơ hội nhận điểm thưởng Boss khổng lồ nhưng vẫn tiếp tục hành trình sinh tồn của mình.

---

> [!IMPORTANT]
> Toàn bộ hệ thống này biến tựa game Rail Shooter vô hạn thành một vòng lặp gây nghiện cao: Người chơi liên tục bị giằng xé giữa bản năng **Bay an toàn để sống sót đi xa hơn** và lòng tham **Lao vào hiểm nguy để đạt bội số điểm kỷ lục thế giới**.
