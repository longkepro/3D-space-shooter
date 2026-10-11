# ĐẶC TẢ CHI TIẾT 26 ĐỘI HÌNH CHỮ CÁI TIẾNG ANH (ALPHABET FORMATIONS SPECIFICATION: A - Z)
> **Dự án:** 3D Star Fox 64 Arcade Virtual Rail Shooter  
> **Kiến trúc:** Data-Driven `FormationPattern` (ScriptableObject) – 0 GC Allocation – Hệ quy chiếu cục bộ Anchor 3D  
> **Mục tiêu gameplay:** Triệu hồi đội hình ký tự đánh vần tên Boss, mật mã trạm không gian hoặc từ khóa thưởng (Special Glyph Waves).

---

## 1. TỔNG QUAN HỆ THỐNG 26 KÝ TỰ (A - Z)

- **Số lượng tàu mỗi chữ cái:** Tối ưu hóa trong khoảng **5 đến 11 tàu/ký tự** (trung bình 7 - 9 tàu).
- **Phạm vi không gian an toàn (Frustum Bounding Box):**
  - Chiều rộng: $X \in [-8.0\text{m}, 8.0\text{m}]$ (nằm gọn trong góc nhìn $60^\circ$ FOV).
  - Chiều cao: $Y \in [-7.0\text{m}, 7.0\text{m}]$.
  - Độ sâu: $Z \in [-2.0\text{m}, 0.0\text{m}]$ (Tàu chỉ huy Slot 0 đặt tại điểm cao nhất hoặc tâm điểm $Z = 0$).
- **Phân bổ vai trò Archetype:**
  - `SquadLeader` (Slot 0): Tâm điểm, mũi nhọn hoặc góc trên trái của chữ cái.
  - `ShieldBearer`: Đặt tại các góc bẻ, thanh ngang, hoặc các điểm chịu đạn chính của ký tự.
  - `Flanker` / `Interceptor` / `Scout`: Dàn đều trên các nét thẳng và nét cong.

### Bảng tóm tắt 26 Đội hình Ký tự (A - Z)

| Ký tự | Tên Pattern | Số tàu | Kích thước $(W \times H)$ | Archetype chủ đạo | Motion Profile gợi ý | Entry Style | Phản ứng mất Leader |
|:---:|---|:---:|:---:|---|---|---|---|
| **A** | `Formation_A` | 8 | $15\text{m} \times 14\text{m}$ | SquadLeader + ShieldBearer | `Stationary_Guard` | `FrontalHorizon` | `BreakShieldAndEngage` |
| **B** | `Formation_B` | 9 | $12\text{m} \times 14\text{m}$ | Heavy Shield Spine | `HorizontalSinusoid` | `OverheadDive` | `MaintainFormation` |
| **C** | `Formation_C` | 7 | $14\text{m} \times 14\text{m}$ | Scout & Flanker | `VerticalUndulation` | `FlankIn` | `PanicScatter` |
| **D** | `Formation_D` | 8 | $14\text{m} \times 14\text{m}$ | Shield Arc Guard | `Corkscrew_Spiral3D` | `FrontalHorizon` | `RetreatWarp` |
| **E** | `Formation_E` | 9 | $12\text{m} \times 14\text{m}$ | 3-Tier Barrier | `HorizontalSinusoid` | `OverheadDive` | `BreakShieldAndEngage` |
| **F** | `Formation_F` | 7 | $12\text{m} \times 14\text{m}$ | Interceptor Flank | `VerticalUndulation` | `FlankIn` | `KamikazeCharge` |
| **G** | `Formation_G` | 9 | $14\text{m} \times 14\text{m}$ | Core Leader | `FigureEight_Infinity`| `RearFlyby` | `RetreatWarp` |
| **H** | `Formation_H` | 8 | $12\text{m} \times 14\text{m}$ | Twin Column Bastion | `Stationary_Guard` | `FrontalHorizon` | `MaintainFormation` |
| **I** | `Formation_I` | 7 | $10\text{m} \times 14\text{m}$ | Linear Column Screen | `HorizontalSinusoid` | `OverheadDive` | `PanicScatter` |
| **J** | `Formation_J` | 6 | $9\text{m} \times 14\text{m}$ | Hook Strike | `VerticalUndulation` | `FlankIn` | `KamikazeCharge` |
| **K** | `Formation_K` | 7 | $12\text{m} \times 14\text{m}$ | Fork Branching | `Lissajous_Cosmic` | `RearFlyby` | `PanicScatter` |
| **L** | `Formation_L` | 5 | $10\text{m} \times 14\text{m}$ | Right Angle Base | `Stationary_Guard` | `FlankIn` | `KamikazeCharge` |
| **M** | `Formation_M` | 9 | $16\text{m} \times 14\text{m}$ | Twin Apex Delta | `FigureEight_Infinity`| `FrontalHorizon` | `BreakShieldAndEngage` |
| **N** | `Formation_N` | 7 | $14\text{m} \times 14\text{m}$ | Z-Axis Incline | `Lissajous_Cosmic` | `RearFlyby` | `PanicScatter` |
| **O** | `Formation_O` | 8 | $15\text{m} \times 14\text{m}$ | Halo Perimeter | `Corkscrew_Spiral3D` | `FrontalHorizon` | `RetreatWarp` |
| **P** | `Formation_P` | 7 | $12\text{m} \times 14\text{m}$ | Top Loop Guard | `HorizontalSinusoid` | `OverheadDive` | `BreakShieldAndEngage` |
| **Q** | `Formation_Q` | 9 | $16\text{m} \times 16\text{m}$ | Halo with Tail Spur | `Corkscrew_Spiral3D` | `FrontalHorizon` | `KamikazeCharge` |
| **R** | `Formation_R` | 8 | $12\text{m} \times 14\text{m}$ | Stride Guard | `HorizontalSinusoid` | `OverheadDive` | `MaintainFormation` |
| **S** | `Formation_S` | 9 | $12\text{m} \times 14\text{m}$ | Dual Sine Curve | `HorizontalSinusoid` | `FlankIn` | `PanicScatter` |
| **T** | `Formation_T` | 7 | $16\text{m} \times 14\text{m}$ | Hammerhead Bar | `VerticalUndulation` | `FrontalHorizon` | `KamikazeCharge` |
| **U** | `Formation_U` | 7 | $12\text{m} \times 14\text{m}$ | Cradle Sweep | `FigureEight_Infinity`| `OverheadDive` | `RetreatWarp` |
| **V** | `Formation_V` | 6 | $14\text{m} \times 14\text{m}$ | Apex Wedge | `Pinwheel_CentroidSpin`| `FrontalHorizon`| `BreakShieldAndEngage` |
| **W** | `Formation_W` | 9 | $16\text{m} \times 14\text{m}$ | Inverted Twin Delta | `FigureEight_Infinity`| `OverheadDive` | `RetreatWarp` |
| **X** | `Formation_X` | 9 | $14\text{m} \times 14\text{m}$ | Crossroads Matrix | `Lissajous_Cosmic` | `FrontalHorizon` | `PanicScatter` |
| **Y** | `Formation_Y` | 7 | $14\text{m} \times 14\text{m}$ | Triad Stem Branch | `VerticalUndulation` | `OverheadDive` | `KamikazeCharge` |
| **Z** | `Formation_Z` | 9 | $14\text{m} \times 12\text{m}$ | Zig-Zag Lightning | `HorizontalSinusoid` | `RearFlyby` | `BreakShieldAndEngage` |

---

## 2. CHI TIẾT ĐẶC TẢ TỪNG KÝ TỰ (A - Z)

---

### #A. CHỮ A (8 Tàu)
* **Đặc tính:** Đỉnh nhọn chữ A hướng thẳng về phía Arwing, thanh ngang nối 2 bên chắn đạn.
* **Sơ đồ Slot:**
```text
               [0] Leader (0, 7)
              /                 \
     (-3.5, 3.5)[1]          [2](3.5, 3.5)
       /                               \
     (-5, 0)[3]=====[4](0, 0)=====[5](5, 0)  (Thanh ngang)
       /                               \
     (-7.5, -7)[6]                  [7](7.5, -7)
```
* **Ma trận tọa độ:**
  - `Slot 0` (Leader): `(0.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Upper Left): `(-3.5, 3.5, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Upper Right): `(3.5, 3.5, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Crossbar Left): `(-5.0, 0.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Crossbar Mid): `(0.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 5` (Crossbar Right): `(5.0, 0.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Foot Left): `(-7.5, -7.0, -1.5)` | `Interceptor` | Scale 1.0
  - `Slot 7` (Foot Right): `(7.5, -7.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #B. CHỮ B (9 Tàu)
* **Đặc tính:** Cột sống dọc thẳng đứng bên trái chịu đòn, 2 vòm tròn cánh cung vươn sang phải.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Spine): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Mid Spine): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Bottom Spine): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 3` (Top Bar): `(-1.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Upper Arch): `(4.5, 4.0, -1.2)` | `Interceptor` | Scale 0.95
  - `Slot 5` (Center Cusp): `(-1.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 6` (Lower Arch): `(4.5, -3.5, -1.2)` | `Interceptor` | Scale 0.95
  - `Slot 7` (Bottom Bar): `(-1.0, -7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 8` (Right Apex): `(5.5, 0.0, -1.5)` | `ShieldBearer` | Scale 1.0

---

### #C. CHỮ C (7 Tàu)
* **Đặc tính:** Cánh cung bán nguyệt mở toang mạn phải đón đạn.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Tip): `(6.0, 6.0, 0.0)` | `SquadLeader` | Scale 1.2
  - `Slot 1` (Top Arch): `(0.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Upper Left): `(-6.0, 4.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 3` (Mid Spine): `(-7.5, 0.0, -1.2)` | `ShieldBearer` | Scale 1.05
  - `Slot 4` (Lower Left): `(-6.0, -4.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 5` (Bottom Arch): `(0.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 6` (Bottom Tip): `(6.0, -6.0, 0.0)` | `Interceptor` | Scale 1.0

---

### #D. CHỮ D (8 Tàu)
* **Đặc tính:** Cột sống dọc kết hợp vòm cầu bao quanh bảo vệ bên phải.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Spine Top): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 3` (Top Arch): `(0.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Upper Right): `(6.0, 4.0, -1.2)` | `Interceptor` | Scale 0.95
  - `Slot 5` (Far Right Peak): `(7.5, 0.0, -1.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 6` (Lower Right): `(6.0, -4.0, -1.2)` | `Interceptor` | Scale 0.95
  - `Slot 7` (Bottom Arch): `(0.0, -7.0, -1.0)` | `Flanker` | Scale 0.95

---

### #E. CHỮ E (9 Tàu)
* **Đặc tính:** Cột dọc trái bọc khiên với 3 thanh ngang xòe ra tạo thành rào chắn 3 tầng.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Left): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Upper): `(-6.0, 3.5, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Spine Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Spine Lower): `(-6.0, -3.5, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 4` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 5` (Top Arm End): `(5.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Top Arm Mid): `(-0.5, 7.0, -0.8)` | `Scout` | Scale 0.95
  - `Slot 7` (Mid Arm End): `(2.0, 0.0, -1.0)` | `Interceptor` | Scale 0.95
  - `Slot 8` (Bottom Arm End): `(5.0, -7.0, -1.0)` | `Flanker` | Scale 0.95

---

### #F. CHỮ F (7 Tàu)
* **Đặc tính:** Tương tự chữ E nhưng khuyết thanh ngang dưới, tạo thế thon gọn linh hoạt.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Left): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Mid): `(-6.0, 1.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Top Arm Mid): `(-0.5, 7.0, -0.8)` | `Scout` | Scale 0.95
  - `Slot 4` (Top Arm End): `(5.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 5` (Mid Arm Mid): `(-1.0, 1.0, -0.8)` | `Scout` | Scale 0.95
  - `Slot 6` (Mid Arm End): `(3.0, 1.0, -1.0)` | `Interceptor` | Scale 0.95

---

### #G. CHỮ G (9 Tàu)
* **Đặc tính:** Vòng cong chữ C kết hợp ngạnh chữ T thu vào tâm. Leader nằm tại ngạnh tâm.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Inner Spur): `(1.0, -2.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top Arch): `(0.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Top Right): `(5.0, 6.0, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 3` (Upper Left): `(-6.0, 4.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Mid Left): `(-7.5, 0.0, -1.2)` | `ShieldBearer` | Scale 1.05
  - `Slot 5` (Lower Left): `(-6.0, -4.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Bottom Arch): `(0.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 7` (Bottom Right Corner): `(6.0, -7.0, -1.0)` | `Interceptor` | Scale 0.95
  - `Slot 8` (Right Stem): `(6.0, -2.0, -0.5)` | `ShieldBearer` | Scale 1.0

---

### #H. CHỮ H (8 Tàu)
* **Đặc tính:** Hai trụ đứng đối xứng song song nối nhau bởi nhịp cầu ngang chính giữa.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Left Top): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Left Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Crossbar Left): `(-2.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 4` (Crossbar Right): `(2.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 5` (Right Top): `(6.0, 7.0, 0.0)` | `Flanker` | Scale 1.05
  - `Slot 6` (Right Mid): `(6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 7` (Right Bottom): `(6.0, -7.0, -0.5)` | `Interceptor` | Scale 1.0

---

### #I. CHỮ I (7 Tàu)
* **Đặc tính:** Cột trụ đơn đối xứng tuyệt đối với hai bệ đỡ ngang trên và dưới.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Center): `(0.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top Bar Left): `(-5.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Top Bar Right): `(5.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Spine Upper): `(0.0, 2.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 4` (Spine Lower): `(0.0, -3.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 5` (Bottom Bar Left): `(-5.0, -7.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 6` (Bottom Bar Right): `(5.0, -7.0, -0.5)` | `Interceptor` | Scale 0.95

---

### #J. CHỮ J (6 Tàu)
* **Đặc tính:** Trụ dọc bên phải thả xuống rồi uốn móc câu sang mạn trái.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Right): `(4.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.2
  - `Slot 1` (Stem Upper): `(4.0, 2.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Stem Lower): `(4.0, -3.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Bottom Arch): `(0.0, -7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Hook Left): `(-4.0, -6.0, -1.0)` | `Interceptor` | Scale 0.95
  - `Slot 5` (Hook Tip): `(-5.0, -2.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #K. CHỮ K (7 Tàu)
* **Đặc tính:** Cột dọc trái với 2 nhánh chéo chữ V mở rộng sang mạn phải.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Spine Top): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 3` (Upper Diagonal Mid): `(-1.0, 3.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 4` (Upper Diagonal Tip): `(6.0, 7.0, -1.5)` | `Flanker` | Scale 1.0
  - `Slot 5` (Lower Diagonal Mid): `(-1.0, -3.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 6` (Lower Diagonal Tip): `(6.0, -7.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #L. CHỮ L (5 Tàu)
* **Đặc tính:** Góc vuông mở chữ L đơn giản, tốc độ lướt nhanh và cơ động.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Tip): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.2
  - `Slot 1` (Spine Upper): `(-6.0, 2.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Spine Lower): `(-6.0, -3.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Corner Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 4` (Foot End): `(4.0, -7.0, -1.0)` | `Interceptor` | Scale 1.0

---

### #M. CHỮ M (9 Tàu)
* **Đặc tính:** Đôi đỉnh nhọn hai bên kết hợp đỉnh chữ V lõm ở giữa. Leader ngự tại đáy chữ V.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Center Valley): `(0.0, 0.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Peak): `(-8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Left Mid Stem): `(-8.0, 0.0, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 3` (Left Foot): `(-8.0, -7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Left Incline): `(-4.0, 3.5, -0.5)` | `Scout` | Scale 0.95
  - `Slot 5` (Right Incline): `(4.0, 3.5, -0.5)` | `Scout` | Scale 0.95
  - `Slot 6` (Right Peak): `(8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 7` (Right Mid Stem): `(8.0, 0.0, -0.8)` | `Interceptor` | Scale 0.95
  - `Slot 8` (Right Foot): `(8.0, -7.0, -1.0)` | `Interceptor` | Scale 0.95

---

### #N. CHỮ N (7 Tàu)
* **Đặc tính:** 2 cột thẳng đứng song song liên kết bởi đường chéo nghiêng dốc.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Left Top): `(-7.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Mid): `(-7.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Left Foot): `(-7.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Diagonal Upper): `(-2.5, 2.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Diagonal Lower): `(2.5, -2.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 5` (Right Top): `(7.0, 7.0, -0.5)` | `Interceptor` | Scale 1.0
  - `Slot 6` (Right Foot): `(7.0, -7.0, 0.0)` | `Interceptor` | Scale 1.0

---

### #O. CHỮ O (8 Tàu)
* **Đặc tính:** Vòng elip khép kín bảo vệ tuyệt đối chu vi.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Apex): `(0.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top-Right): `(5.5, 5.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 2` (Right Mid): `(7.5, 0.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Bottom-Right): `(5.5, -5.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 4` (Bottom Base): `(0.0, -7.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 5` (Bottom-Left): `(-5.5, -5.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 6` (Left Mid): `(-7.5, 0.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 7` (Top-Left): `(-5.5, 5.0, -0.5)` | `Flanker` | Scale 0.95

---

### #P. CHỮ P (7 Tàu)
* **Đặc tính:** Cột dọc trái với vòng khép kín ở nửa trên.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Spine Top): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Loop Top Bar): `(0.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Loop Far Right): `(5.0, 3.5, -1.5)` | `Interceptor` | Scale 1.0
  - `Slot 5` (Loop Mid Bar): `(0.0, 0.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Loop Shoulder): `(3.5, 6.0, -1.0)` | `Scout` | Scale 0.95

---

### #Q. CHỮ Q (9 Tàu)
* **Đặc tính:** Vòng tròn chữ O có thêm chiếc đuôi chéo phóng ra góc đông nam.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Apex): `(0.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top-Right): `(5.5, 5.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 2` (Right Mid): `(7.5, 0.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Bottom-Right): `(5.5, -5.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 4` (Bottom Base): `(0.0, -7.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 5` (Bottom-Left): `(-5.5, -5.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 6` (Left Mid): `(-7.5, 0.0, -1.0)` | `ShieldBearer` | Scale 1.0
  - `Slot 7` (Top-Left): `(-5.5, 5.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 8` (Tail Spur): `(8.5, -8.5, -1.5)` | `Interceptor` | Scale 1.05

---

### #R. CHỮ R (8 Tàu)
* **Đặc tính:** Cấu trúc chữ P kết hợp chân xiên vững chắc mở rộng sang phải.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Spine Top): `(-6.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Spine Mid): `(-6.0, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Spine Bottom): `(-6.0, -7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Loop Top): `(0.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Loop Apex): `(5.0, 3.5, -1.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 5` (Loop Base): `(0.0, 0.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Stride Mid): `(2.5, -3.5, -1.0)` | `Interceptor` | Scale 0.95
  - `Slot 7` (Stride Foot): `(6.0, -7.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #S. CHỮ S (9 Tàu)
* **Đặc tính:** Đường cong chữ S kép mềm mại uốn lượn liên tục.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Upper Spine): `(-6.0, 3.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top Left Arch): `(-4.0, 7.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 2` (Top Crown): `(0.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Top Right Hook): `(5.0, 5.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Center Bridge): `(-2.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 5` (Center Mid): `(2.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 6` (Lower Spine): `(6.0, -3.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 7` (Bottom Base): `(0.0, -7.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 8` (Bottom Left Hook): `(-5.0, -7.0, -1.0)` | `Interceptor` | Scale 0.95

---

### #T. CHỮ T (7 Tàu)
* **Đặc tính:** Thanh búa ngang trên đỉnh đón đầu kết hợp cán dọc ở giữa.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Crown Center): `(0.0, 7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Crossbar Far Left): `(-8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Crossbar Mid Left): `(-4.0, 7.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 3` (Crossbar Mid Right): `(4.0, 7.0, -0.5)` | `Flanker` | Scale 0.95
  - `Slot 4` (Crossbar Far Right): `(8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 5` (Stem Mid): `(0.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 6` (Stem Base): `(0.0, -7.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #U. CHỮ U (7 Tàu)
* **Đặc tính:** Máng chữ U võng đáy đón trọn đường bay của đối phương.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Bottom Center): `(0.0, -7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Top): `(-6.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Left Mid): `(-6.0, 0.0, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 3` (Bottom Left Curve): `(-4.5, -5.5, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Bottom Right Curve): `(4.5, -5.5, -1.0)` | `Interceptor` | Scale 0.95
  - `Slot 5` (Right Mid): `(6.0, 0.0, -0.8)` | `Interceptor` | Scale 0.95
  - `Slot 6` (Right Top): `(6.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.0

---

### #V. CHỮ V (6 Tàu)
* **Đặc tính:** Đội hình mũi tên chữ V nhọn chúc ngược xuống dưới hoặc đâm tới.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Apex Bottom): `(0.0, -7.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Mid): `(-3.5, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Left Wingtip): `(-7.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 3` (Right Mid): `(3.5, 0.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 4` (Right Wingtip): `(7.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 5` (Rear Guard): `(0.0, -3.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #W. CHỮ W (9 Tàu)
* **Đặc tính:** Chữ M đảo ngược với 2 mũi chúc đáy và 1 đỉnh nhọn ở giữa. Leader ngự tại đỉnh giữa.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Center Peak): `(0.0, 0.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Wingtip): `(-8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Left Mid): `(-6.0, -3.5, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 3` (Left Cusp Bottom): `(-4.0, -7.0, -1.0)` | `Interceptor` | Scale 1.0
  - `Slot 4` (Left Incline Mid): `(-2.0, -3.5, -0.5)` | `Scout` | Scale 0.95
  - `Slot 5` (Right Incline Mid): `(2.0, -3.5, -0.5)` | `Scout` | Scale 0.95
  - `Slot 6` (Right Cusp Bottom): `(4.0, -7.0, -1.0)` | `Interceptor` | Scale 1.0
  - `Slot 7` (Right Mid): `(6.0, -3.5, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 8` (Right Wingtip): `(8.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05

---

### #X. CHỮ X (9 Tàu)
* **Đặc tính:** Hai đường chéo đối xứng cắt nhau tại tâm. Leader trấn thủ ngay tâm giao điểm.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Core Intersection): `(0.0, 0.0, 0.0)` | `SquadLeader` | Scale 1.3
  - `Slot 1` (Top-Left End): `(-7.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 2` (Top-Left Mid): `(-3.5, 3.5, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 3` (Top-Right End): `(7.0, 7.0, -0.5)` | `ShieldBearer` | Scale 1.05
  - `Slot 4` (Top-Right Mid): `(3.5, 3.5, -0.8)` | `Flanker` | Scale 0.95
  - `Slot 5` (Bottom-Left End): `(-7.0, -7.0, -0.5)` | `Interceptor` | Scale 1.0
  - `Slot 6` (Bottom-Left Mid): `(-3.5, -3.5, -0.8)` | `Scout` | Scale 0.95
  - `Slot 7` (Bottom-Right End): `(7.0, -7.0, -0.5)` | `Interceptor` | Scale 1.0
  - `Slot 8` (Bottom-Right Mid): `(3.5, -3.5, -0.8)` | `Scout` | Scale 0.95

---

### #Y. CHỮ Y (7 Tàu)
* **Đặc tính:** Nhánh chạc 3 với 2 cánh xòe trên và chân trụ thẳng đứng bên dưới.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Fork Center): `(0.0, 1.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Left Branch Mid): `(-3.5, 4.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Left Branch Tip): `(-7.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 3` (Right Branch Mid): `(3.5, 4.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 4` (Right Branch Tip): `(7.0, 7.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 5` (Stem Mid): `(0.0, -3.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 6` (Stem Base): `(0.0, -7.0, -1.5)` | `Interceptor` | Scale 1.0

---

### #Z. CHỮ Z (9 Tàu)
* **Đặc tính:** Đường zig-zag tia sét kinh điển với 2 thanh ngang và đường chéo nối.
* **Ma trận tọa độ:**
  - `Slot 0` (Leader - Top Center): `(0.0, 6.0, 0.0)` | `SquadLeader` | Scale 1.25
  - `Slot 1` (Top-Left End): `(-7.0, 6.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 2` (Top-Right End): `(7.0, 6.0, -0.5)` | `ShieldBearer` | Scale 1.0
  - `Slot 3` (Diagonal Upper): `(3.5, 2.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 4` (Diagonal Center): `(0.0, 0.0, -1.0)` | `Scout` | Scale 0.95
  - `Slot 5` (Diagonal Lower): `(-3.5, -2.0, -1.0)` | `Flanker` | Scale 0.95
  - `Slot 6` (Bottom-Left End): `(-7.0, -6.0, -0.5)` | `Interceptor` | Scale 1.0
  - `Slot 7` (Bottom Center): `(0.0, -6.0, -0.5)` | `Interceptor` | Scale 0.95
  - `Slot 8` (Bottom-Right End): `(7.0, -6.0, -0.5)` | `ShieldBearer` | Scale 1.0
