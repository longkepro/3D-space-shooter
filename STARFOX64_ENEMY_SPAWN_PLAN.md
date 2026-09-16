# KẾ HOẠCH TỔNG THỂ: TRIỂN KHAI TOÀN DIỆN HỆ THỐNG VIRTUAL RAIL SHOOTER THEO CHUẨN TDD v1.0.0

Kế hoạch này phân rã và hiện thực hóa toàn bộ **Tài Liệu Đặc Tả Thiết Kế Kỹ Thuật (TDD v1.0.0)** thành 3 giai đoạn kiến trúc tuần tự, đảm bảo tuân thủ nghiêm ngặt các nguyên tắc:
1. **0 GC Allocation:** 100% các thực thể (đạn, quái vật, hiệu ứng, đội hình) tái sử dụng qua Object Pool.
2. **Toán học chính xác:** Triệt tiêu sai số thị sai 3D, kiểm soát vận tốc tương đối $V_{closing}$, Frustum động và đường cong Cubic Bézier.
3. **Bảo lưu an toàn 3 tầng (3-Layer Safety Undo):** Mỗi giai đoạn đều có nhánh Git dự phòng độc lập, menu Unity 1-click và cờ bật/tắt trong ScriptableObject.

---

## User Review Required

> [!IMPORTANT]
> **Quy trình triển khai 3 Giai đoạn tuần tự:**
> - **GIAI ĐOẠN 1 (Combat & Counter-Play):** Đạn đạo kẻ địch $V_{closing}$, Lộn cánh Barrel Roll Deflection, Bom hủy diệt Smart Bomb, Tụ lực nổ lan Charge Shot & Combo điểm số.
> - **GIAI ĐOẠN 2 (Spatial Vectors & Lifecycle):** Hệ thống 10 Vector xuất hiện không gian, Toán học Frustum động, Máy trạng thái vòng đời 4 pha (Telegraph $\rightarrow$ Approach $\rightarrow$ Action $\rightarrow$ Exit).
> - **GIAI ĐOẠN 3 (Formation Choreography):** Đầu mối đội hình ảo `VirtualFormationAnchor`, Ráp nối Bézier 2 pha, Cơ chế phân rã hoảng loạn khi mất chỉ huy (Panic Scatter).

> [!TIP]
> Mỗi giai đoạn sẽ được kiểm thử biên dịch độc lập (**0 compile errors**), có menu Unity Editor để hoàn nguyên (Undo) về trạng thái cũ ngay lập tức nếu cần.

---

## GIAI ĐOẠN 1: HỆ THỐNG ĐẠN ĐẠO, KHẮC CHẾ PHÒNG THỦ & TÍNH ĐIỂM (PHẦN I TDD)

### 1.1. Module Đạn Địch & Vận Tốc Tương Đối ($V_{closing}$)
* **Mục tiêu:** Thay thế tia hitscan laser cũ của quái vật bằng hệ thống đạn vật lý dạng khối cầu **Plasma Orb** (lõi trắng + hào quang bloom cam/đỏ) và đạn con thoi **Dart** kéo dải hạt Ribbon Trail dài $5 - 8\text{m}$.
* **Toán học vận tốc tương đối:**
  - Đạn đối đầu (Head-on): Tàu người chơi tiến $V_{rail} = 40\text{ m/s}$, khống chế đạn địch $V_{bullet} \approx 20\text{ m/s} \implies V_{closing} \approx 60\text{ m/s}$ (Cửa sổ phản xạ $1.5\text{s}$ tại cự ly $90\text{m}$).
  - Đạn bám đuổi sau lưng (Rear Chaser): $V_{bullet} \ge V_{rail} + 30\text{ m/s}$ ($70 - 90\text{ m/s}$) để đuổi kịp tàu.
  - Cộng hưởng vector đường ray: $\vec{V}_{bullet\_world} = \vec{V}_{rail\_tangent} \cdot V_{rail} + \vec{V}_{bullet\_local}$.
* **Hitbox dung thứ bất đối xứng (Forgiving Hitbox):**
  - Bán kính thị giác: $R_{visual} \approx 1.2\text{m} - 1.5\text{m}$.
  - Bán kính va chạm vật lý: $R_{hitbox} \approx 0.4\text{m} - 0.5\text{m}$ (chiếm $30\% - 35\%$ kích thước nhìn thấy, tạo cảm giác lách đạn cận kề thót tim mà không chết oan).
  - Thuật toán bù trừ cự ly: $Scale_{visual} = \text{Clamp}(1.0 + \frac{Distance}{D_{ref}} \cdot K_{scale}, 1.0, 3.5)$.

#### [NEW] [`EnemyProjectile.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/EnemyProjectile.cs)
- Thực thể đạn địch triển khai `IPoolableEntity`.
- Quản lý chuyển động vật lý, vệt Ribbon Trail, kiểm tra va chạm với tàu người chơi và tính toán bù trừ tỷ lệ nhìn xa.

#### [NEW] [`EnemyProjectilePool.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/EnemyProjectilePool.cs)
- Bộ đệm Object Pool tái sử dụng 40 viên đạn Plasma Orb và Dart (0 GC Alloc).

#### [MODIFY] [`EnemyAttack.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/EnemyAttack.cs)
- Chuyển từ bắn LineRenderer tức thời sang triệu gọi đạn từ `EnemyProjectilePool`.

---

### 1.2. Hệ Thống Phòng Thủ Khắc Chế Chủ Động (Active Counter-Play)
* **Barrel Roll Deflection (Lộn cánh hất đạn $90^\circ$):**
  - Thao tác: Nhấp đúp phím lộn trái/phải (`A`/`D` hoặc `Q`/`E`, hoặc nút gạt mobile).
  - Thân tàu xoay $360^\circ$ quanh trục Z trong $0.35\text{s} - 0.45\text{s}$.
  - Kích hoạt trường từ tính: Bất kỳ đạn plasma nào va chạm trong cửa sổ này bị bẻ ngoặt $90^\circ$ văng ra ngoài, miễn nhiễm sát thương $100\%$.
* **Destructible Projectiles (Bắn phá đạn hạng nặng):**
  - Tên lửa tầm nhiệt và thủy lôi không gian được gán `HitPoints`. Người chơi có thể bắn nổ chúng giữa đường bay.
* **Smart Bomb Screen-Clear (Bom thông minh dọn sạch màn hình):**
  - Bấm phím Bom (`B` / `Space` / Nút Bom UI): Phát nổ sóng xung kích giãn nở bán kính $150\text{m}$, triệt tiêu $100\%$ đạn địch trong khung nhìn và gây sát thương diện rộng cực lớn.

#### [MODIFY] [`VirtualRailShip.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailShip.cs)
- Tích hợp máy trạng thái Barrel Roll, phát hiện nhấp đúp (Double Tap detection), xoay lộn $360^\circ$ và kích hoạt cờ `IsDeflecting = true`.

#### [NEW] [`SmartBombModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SmartBombModule.cs)
- Quản lý số lượng bom dự trữ, hiệu ứng sóng xung kích $150\text{m}$ và quét dọn đạn địch.

---

### 1.3. Cơ Chế Tụ Lực Nổ Lan (Charge Shot AoE) & Động Cơ Điểm Số Combo
* **Charge Shot Splash AoE:**
  - Nhấn giữ nút bắn $\ge 0.75\text{s}$ để tích tụ quả cầu năng lượng trước mũi tàu.
  - Vòng quét khóa mục tiêu Lock-on bắt dính kẻ địch gần nhất trong hình nón quét $18^\circ$.
  - Nhả bắn: Quả cầu phóng tới mục tiêu và kích hoạt vụ nổ hình cầu bán kính $R_{splash} \approx 8\text{m} - 12\text{m}$.
* **Động cơ tính điểm Combo Hit Count Engine:**
  - Bắn hạ bằng đạn thường: $+1\text{ Hit}$ mỗi đơn vị.
  - Nổ lan diệt cả cụm bằng Charge Shot: $TotalHits = N_{killed} + Bonus(N_{killed} - 1)$.
  - Bảng UI hiển thị chuỗi Combo nổi bật (+3 Hits, +5 Hits, +9 Hits!).

#### [MODIFY] [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs)
- Tích hợp vùng nổ lan `Physics.OverlapSphere` bán kính $10\text{m}$ và gửi thông điệp tiêu diệt theo cụm cho động cơ tính điểm.

#### [NEW] [`ComboScoringEngine.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/ComboScoringEngine.cs)
- Quản lý hệ số nhân điểm số, hiển thị thông báo thưởng chuỗi tiêu diệt combo trên HUD.

---

## GIAI ĐOẠN 2: HỆ THỐNG 10 VECTOR KHÔNG GIAN & VÒNG ĐỜI 4 PHA (PHẦN II TDD)

### 2.1. Định Vị Điểm Sinh Theo Frustum Động & Chuẩn Hóa 10 Vector
Toàn bộ điểm spawn được tính toán tức thời theo hình học Camera Frustum:
$$H_{half} = Z_{target} \cdot \tan\left(\frac{FOV}{2}\right), \quad W_{half} = H_{half} \cdot Aspect$$

* **5 Vector Động học Không gian cốt lõi:**
  1. **Trực diện chân trời (Frontal Head-on):** Lao tới từ $Z + 300\text{m} \sim 400\text{m}$.
  2. **Đột kích sau lưng (Rear Ambush):** Xuất hiện từ $Z - 30\text{m} \sim -60\text{m}$, tăng tốc vượt mặt hoặc ghìm đuôi xả đạn. Đuôi tàu có ống xả phát quang đóng vai trò Điểm yếu chí mạng ($200\%$ damage).
  3. **Cắt ngang sườn (Lateral Flanking):** Lao vuông góc $90^\circ$ từ mép ngoài $X$, cắt ngang hành lang bay ($0.8\text{s} - 1.5\text{s}$). Thân tàu dạng cá mập/cánh đứng cao để mở rộng tiết diện nhận diện.
  4. **Bổ nhào từ trần mây (Overhead Dive-bomb):** Rơi dốc từ $+Y$ theo góc $45^\circ - 70^\circ$, cắm xuống đáy rồi ngóc mũi bay vút lên.
  5. **Phóng trồi đáy sâu (Sub-surface Breach):** Vọt lên từ $-Y$ theo động lực học ném đứng, đạt đỉnh cực đại rồi rơi xuống.
* **5 Vector Môi trường, Phân rã & Viễn tưởng:**
  6. Lộ diện góc cua khuất (Blind Curve Reveal).
  7. Vật cản sụp đổ động học (Dynamic Kinetic Hazards).
  8. Chui ra từ khoang hầm ngầm (Structural Emergence).
  9. Vỡ mảnh thiên thạch / Tàu mẹ (Cluster Fracture: nổ văng 3-5 mảnh con).
  10. Cổng dịch chuyển & Hủy tàng hình (Warp-in & Decloak tại $Z \approx 80 - 120\text{m}$).

#### [NEW] [`SpatialVectorFactory.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SpatialVectorFactory.cs)
- Nhà máy tính toán vị trí, góc xoay và vector sơ cấp cho cả 10 phương thức xuất hiện.

---

### 2.2. Vòng Đời Trạng Thái Bốn Pha Của Thực Thể (Four-Phase State Machine)
Mọi quái vật sinh ra đều vận hành tuần tự qua 4 pha:
1. **Telegraph ($0.4\text{s} - 0.8\text{s}$):** Báo hiệu âm thanh định hướng, vệt sáng ngoài màn hình, Collider TẮT.
2. **Approach ($1.0\text{s} - 1.5\text{s}$):** Lao vào hành lang bay, Collider BẬT. Hitbox trục Z kéo dài gấp 1.5 lần chống đạn lọt khung hình.
3. **Action ($2.0\text{s} - 3.0\text{s}$):** Đồng tốc với tàu, xả đạn, bộc lộ điểm yếu (cửa sổ vàng để bắn hạ).
4. **Exit ($1.0\text{s}$):** Peel-off lộn cánh $90^\circ$, bứt tốc thoát ly và tự động thu hồi về Object Pool.

#### [NEW] [`EnemyLifecycleFSM.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/EnemyLifecycleFSM.cs)
- Bộ điều khiển máy trạng thái gắn lên từng kẻ địch, đảm bảo nhịp độ chiến đấu công bằng và chuẩn mực.

---

## GIAI ĐOẠN 3: BIÊN ĐẠO ĐỘI HÌNH & CHỈ HUY (PHẦN III TDD)

### 3.1. Mô Hình Đầu Mối Ảo (`VirtualFormationAnchor.cs`) & Ma Trận Slot
* Tách rời hoàn toàn vị trí tàu vật lý và quỹ đạo cả đội.
* Đầu mối ảo `FormationAnchor` vô hình trôi dọc theo hành lang ray.
* Các mẫu đội hình định nghĩa sẵn (Ma trận Slot):
  - **Đội hình chữ V (Wedge):** Slot 0 (Chỉ huy tại tâm), Slot 1-2 (Cánh trái/phải lệch sau).
  - **Đội hình hàng ngang (Echelon/Line):** Dàn hàng ngang quét màn hình.
  - **Đội hình xoắn ốc (Spiral Stream):** Rồng bay uốn lượn nối đuôi nhau.

#### [NEW] [`VirtualFormationAnchor.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualFormationAnchor.cs)
- Quản lý danh sách Slot, vị trí tương đối và trạng thái sống sót của cả phi đội.

---

### 3.2. Ráp Nối Đội Hình Hai Pha Bằng Đường Cong Bézier 3D
* **Pha 1 (Free Assembly - $1.0\text{s} - 1.5\text{s}$):**
  - Từng tàu xuất phát từ các vector khác nhau (con từ trần mây, con từ mép trái) bay theo đường cong tham số **Cubic Bézier 3D**:
    $$\vec{P}_{ship}(t) = \text{EvaluateBezier}\left(\vec{P}_{spawn}, \vec{P}_{ctrl1}, \vec{P}_{ctrl2}, \vec{P}_{slot\_world}(t), \frac{t}{T_{assemble}}\right)$$
* **Pha 2 (Synchronized Lock):**
  - Ngay khi $t \ge T_{assemble}$, tàu khóa cứng vào Slot, bay đồng điệu tuyệt đối cùng Anchor.

---

### 3.3. Cơ Chế Phân Rã Hoảng Loạn Khi Mất Chỉ Huy (Leader Kill & Panic Scatter)
* **Bắn hạ Tàu Cánh (Slot 1 hoặc 2):** Đội hình KHÔNG vỡ. Slot bị hủy, các tàu còn lại tiếp tục giữ cự ly bay.
* **Bắn hạ Tàu Chỉ Huy (Slot 0):** KÍCH HOẠT HIỆU ỨNG TÁN LOẠN:
  1. Hủy bỏ toàn bộ liên kết Slot với `FormationAnchor`.
  2. Tàu cánh nhận vector gia tốc ly tâm:
     $$\vec{V}_{panic} = \text{Normalize}(P_{ship.xy} - P_{anchor.xy}) \cdot V_{boost} + \vec{Forward}_{rail} \cdot V_{rail}$$
  3. Tàu cánh lộn vòng bỏ chạy gấp sang 2 mép biên màn hình và biến mất.

---

### 3.4. Động Cơ Đánh Đổi Rủi Ro - Phần Thưởng (Risk vs. Reward Engine)
* **Chiến thuật an toàn (Tiêu diệt sớm trong Pha 1):** Bắn tỉa lẻ tẻ từng con khi đang nhập đội $\implies$ Rất an toàn nhưng chỉ được điểm tối thiểu ($+1\text{ Hit}$ mỗi con).
* **Chiến thuật rủi ro cao (Gom cụm trong Pha 2):** Chờ cả 5 con khóa cứng đội hình chữ V, chịu áp lực bão đạn đỏ, né đạn và bắn 1 phát Charge Shot nổ lan $\implies$ Ăn trọn combo $+9\text{ Hits}$!

---

## BẢNG TỔNG KẾT FILE DỰ KIẾN THAY ĐỔI / TẠO MỚI

| Phân hệ | File | Thao tác | Chức năng chính |
| :--- | :--- | :---: | :--- |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/EnemyProjectile.cs` | **[NEW]** | Thực thể đạn plasma/dart, tính $V_{closing}$, hitbox dung thứ 35% |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/EnemyProjectilePool.cs` | **[NEW]** | Object Pool đạn địch 0 GC Alloc (40 viên) |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/SmartBombModule.cs` | **[NEW]** | Bom thông minh sóng xung kích 150m quét sạch đạn |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/ComboScoringEngine.cs` | **[NEW]** | Động cơ tính điểm combo nổ lan $N + (N-1)$ |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/VirtualRailShip.cs` | **[MODIFY]** | Tích hợp Barrel Roll Deflection xoay 360° hất đạn 90° |
| **Giai đoạn 1** | `Assets/Scripts/EnemyAttack.cs` | **[MODIFY]** | Đổi sang bắn đạn vật lý đón đầu qua Pool |
| **Giai đoạn 1** | `Assets/Scripts/VirtualRail/AimAssistModule.cs` | **[MODIFY]** | Thêm vùng nổ lan Charge Shot Splash $8 - 12\text{m}$ |
| **Giai đoạn 2** | `Assets/Scripts/VirtualRail/SpatialVectorFactory.cs` | **[NEW]** | Nhà máy 10 Vector xuất hiện không gian theo Frustum |
| **Giai đoạn 2** | `Assets/Scripts/VirtualRail/EnemyLifecycleFSM.cs` | **[NEW]** | Máy trạng thái 4 pha (Telegraph/Approach/Action/Exit) |
| **Giai đoạn 2** | `Assets/Scripts/VirtualRail/VirtualRailWaveSpawner.cs` | **[MODIFY]** | Tích hợp sinh quái theo 10 Vector đa hướng |
| **Giai đoạn 3** | `Assets/Scripts/VirtualRail/VirtualFormationAnchor.cs` | **[NEW]** | Đầu mối ảo quản lý đội hình & Slot ma trận |
| **Giai đoạn 3** | `Assets/Scripts/VirtualRail/FormationBezierTrajectory.cs`| **[NEW]** | Đường cong Bézier ráp nối đội hình 2 pha |
| **Giai đoạn 3** | `Assets/Scripts/VirtualRail/FormationLeaderModule.cs` | **[NEW]** | Cơ chế phân rã hoảng loạn Panic Scatter khi mất chỉ huy |
| **Hệ thống** | `Assets/Editor/VirtualRailModeToggle.cs` | **[MODIFY]** | Menu 1-click Undo/Redo cho từng giai đoạn |
| **Cấu hình** | `Assets/Scripts/VirtualRail/VirtualRailConfig.cs` | **[MODIFY]** | Cờ bật/tắt module hóa toàn bộ các phân hệ |

---

## Kế Hoạch Xác Minh (Verification Plan)

### Kiểm Thử Tự Động & Biên Dịch
- Biên dịch qua Roslyn Compiler sau mỗi Giai đoạn: Đảm bảo **0 lỗi (Exit Code 0)** cho cả `Assembly-CSharp.dll` và `Assembly-CSharp-Editor.dll`.
- Quản lý bộ nhớ Profiler: Không phát sinh bất kỳ `GC.Alloc` nào trong quá trình xả đạn, sinh đạn địch hay lộn vòng Barrel Roll.

### Kiểm Thử Trải Nghiệm (Game Feel Verification)
1. **Kiểm thử Giai đoạn 1:**
   - Nhấp đúp phím `A`/`D` hoặc `Q`/`E`: Tàu xoay $360^\circ$, đạn địch bắn tới chạm vào thân tàu bị hất văng góc $90^\circ$ kèm tiếng 'ping' kim loại và không mất máu.
   - Bấm phím Bom: Toàn bộ đạn trên màn hình bị sóng xung kích xóa sổ ngay lập tức.
   - Giữ tụ lực bắn vào 1 đám quái: Vụ nổ lan $10\text{m}$ tiêu diệt cả cụm, điểm số hiện Combo `+9 Hits`.
2. **Kiểm thử Giai đoạn 2:**
   - Quái xuất hiện đa dạng: từ trần mây cắm xuống, từ mép biên sườn lao qua, từ sau lưng vượt mặt bộc lộ điểm yếu động cơ phát sáng.
   - Quái không bị bắn hạ sẽ tự động lộn cánh $90^\circ$ bứt tốc thoát ly ra ngoài màn hình.
3. **Kiểm thử Giai đoạn 3:**
   - Quái bay từ nhiều hướng hội tụ mượt mà theo đường cong Bézier vào đội hình chữ V.
   - Bắn chết con chỉ huy đầu đàn: Toàn bộ các con cánh lập tức hoảng loạn bẻ lái dạt sang 2 bên màn hình rút lui.
