# ĐẶC TẢ CHI TIẾT 20 ĐỘI HÌNH HÌNH HỌC VŨ TRỤ (GEOMETRIC FORMATIONS SPECIFICATION)
> **Dự án:** 3D Star Fox 64 Arcade Virtual Rail Shooter  
> **Kiến trúc:** Data-Driven `FormationPattern` (ScriptableObject) – 0 GC Allocation – Hệ quy chiếu cục bộ Anchor 3D  
> **Tài liệu tham chiếu:** TDD v1.0.0 Phần III & `FormationPattern.cs`

---

## 1. TỔNG QUAN HỆ THỐNG 20 HÌNH KHỐI HÌNH HỌC

Hệ thống cung cấp 20 mẫu đội hình hình học phân bố từ cơ bản đến phức tạp, được chia thành 4 nhóm chính:
- **Nhóm 1: Tam giác (Triangle)** – Tính đâm thủng, mũi nhọn xuyên phá (Hình 1 - 3).
- **Nhóm 2: Tứ giác (Quadrilateral)** – Bức tường phòng thủ, dàn trận cân bằng (Hình 4 - 10).
- **Nhóm 3: Đa giác đều (Regular Polygons)** – Kiến trúc quỹ đạo, phong tỏa đa góc (Hình 11 - 15).
- **Nhóm 4: Đường cong & Miền giới hạn (Curved & Regions)** – Vòng tròn, elip, nan quạt, vành khuyên (Hình 16 - 20).

### Bảng tóm tắt 20 Đội hình

| # | Tên đội hình | Nhóm | Số tàu | Kích thước $(W \times H)$ | Archetype chủ đạo | Motion Profile gợi ý | Entry Style gợi ý | Leader Death Reaction |
|---|---|---|:---:|:---:|---|---|---|---|
| **1** | **Tam giác thường** (Scalene Triangle) | Tam giác | 5 | $21\text{m} \times 13\text{m}$ | Scout & Flanker | `HorizontalSinusoid` | `FlankIn` | `PanicScatter` |
| **2** | **Tam giác đều** (Equilateral Delta) | Tam giác | 6 | $20\text{m} \times 15\text{m}$ | SquadLeader + ShieldBearer | `StationaryHold` | `FrontalHorizon` | `BreakShieldAndEngage` |
| **3** | **Tam giác vuông** (Right Wing Wedge) | Tam giác | 5 | $18\text{m} \times 12\text{m}$ | ShieldBearer tường chặn | `VerticalUndulation` | `FlankIn` | `KamikazeCharge` |
| **4** | **Hình vuông** (Iron Square Phalanx) | Tứ giác | 8 | $16\text{m} \times 16\text{m}$ | ShieldBearer 4 góc | `CorkscrewRoll` | `FrontalHorizon` | `MaintainFormation` |
| **5** | **Hình chữ nhật** (Wide Wall Screen) | Tứ giác | 8 | $24\text{m} \times 12\text{m}$ | Heavy Shield Screen | `HorizontalSinusoid` | `OverheadDive` | `BreakShieldAndEngage` |
| **6** | **Hình bình hành** (Skewed Parallelogram) | Tứ giác | 7 | $20\text{m} \times 12\text{m}$ | Flanker cơ động | `Lissajous3D` | `RearFlyby` | `PanicScatter` |
| **7** | **Hình thoi** (Diamond Guard Elite) | Tứ giác | 5 | $28\text{m} \times 16\text{m}$ | SquadLeader + ShieldBearer | `FigureEight` | `FrontalHorizon` | `RetreatWarp` |
| **8** | **Hình thang** (Broad Trapezoid) | Tứ giác | 6 | $22\text{m} \times 11\text{m}$ | Scout dồn hỏa lực | `HorizontalSinusoid` | `FlankIn` | `PanicScatter` |
| **9** | **Hình thang cân** (Isosceles Bastion) | Tứ giác | 7 | $24\text{m} \times 12\text{m}$ | ShieldBearer đáy bảo vệ | `StationaryHold` | `OverheadDive` | `MaintainFormation` |
| **10** | **Hình thang vuông** (Right Bastion Rampart) | Tứ giác | 6 | $18\text{m} \times 12\text{m}$ | ShieldBearer vách đứng | `VerticalUndulation` | `FlankIn` | `KamikazeCharge` |
| **11** | **Ngũ giác đều** (Pentagram Citadel) | Đa giác | 6 | $19\text{m} \times 18\text{m}$ | SquadLeader + ShieldBearer | `Pinwheel_CentroidSpin` | `FrontalHorizon` | `RetreatWarp` |
| **12** | **Lục giác đều** (Hexagonal Hive) | Đa giác | 7 | $20\text{m} \times 20\text{m}$ | 6 Tàu khiên tổ ong | `Pinwheel_CentroidSpin` | `OverheadDive` | `MaintainFormation` |
| **13** | **Bát giác đều** (Octagonal Ring) | Đa giác | 9 | $20\text{m} \times 20\text{m}$ | Vòng vây 8 hướng | `CorkscrewRoll` | `FrontalHorizon` | `KamikazeCharge` |
| **14** | **Thập giác đều** (Decagon Decashield) | Đa giác | 11 | $22\text{m} \times 22\text{m}$ | Siêu lá chắn 10 đỉnh | `FigureEight` | `FrontalHorizon` | `BreakShieldAndEngage` |
| **15** | **n-giác đều** (Star Dodecahedron - 12 Đỉnh) | Đa giác | 13 | $24\text{m} \times 24\text{m}$ | Cụm hạm đội sao | `Lissajous3D` | `OverheadDive` | `RetreatWarp` |
| **16** | **Hình tròn** (Orbital Halo Swarm) | Đường cong | 9 | $18\text{m} \times 18\text{m}$ | Vành đai bảo vệ lõi | `CorkscrewRoll` | `RearFlyby` | `RetreatWarp` |
| **17** | **Hình ellipse** (Elliptic Eclipse) | Đường cong | 9 | $28\text{m} \times 14\text{m}$ | Đĩa nghiêng trục | `Lissajous3D` | `OverheadDive` | `PanicScatter` |
| **18** | **Hình quạt tròn** (Radial Fan Sweep) | Đường cong | 7 | $20\text{m} \times 14\text{m}$ | Cánh quạt quét diện rộng | `HorizontalSinusoid` | `FlankIn` | `KamikazeCharge` |
| **19** | **Hình vành khuyên** (Dual Ring Annulus) | Đường cong | 11 | $22\text{m} \times 22\text{m}$ | Vành đai kép đồng tâm | `Pinwheel_CentroidSpin` | `FrontalHorizon` | `MaintainFormation` |
| **20** | **Hình viên phân** (Crescent Bow Segment) | Đường cong | 7 | $20\text{m} \times 10\text{m}$ | Cánh cung chắn dây cung | `VerticalUndulation` | `OverheadDive` | `BreakShieldAndEngage` |

---

## 2. CHI TIẾT ĐẶC TẢ TỪNG HÌNH KHỐI VÀ TỌA ĐỘ 3D CHUẨN

---

### #1. TAM GIÁC THƯỜNG (Scalene Triangle)
* **Ý tưởng chiến thuật:** Tam giác bất đối xứng 3 cạnh khác nhau. Khi xuất hiện từ sườn, hình dáng lệch tâm khiến người chơi khó đoán trước quỹ đạo né đạn.
* **Số tàu:** 5 chiếc.

```text
               [0] Leader (2, 7)
              /   \
   (-4, 2) [1]     \
          /         \
(-10,-5)[2]----[3]----[4] (11, -3)
             (1,-6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Đỉnh Lệch (Leader) | `(2.0, 7.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.15 |
| **1** | Sườn Trái (Left Incline) | `(-4.0, 2.0, -1.0)` | Hộ vệ | `ShieldBearer` | 1.00 |
| **2** | Góc Đáy Trái (Far Left Corner) | `(-10.0, -5.0, -2.0)` | Tiêm kích | `Flanker` | 1.00 |
| **3** | Trung Điểm Đáy (Base Center) | `(1.0, -6.0, -1.5)` | Tấn công | `Scout` | 0.95 |
| **4** | Góc Đáy Phải (Right Corner) | `(11.0, -3.0, -2.5)` | Đột kích | `Interceptor` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.HorizontalSinusoid`, `FormationEntryStyle.FlankIn`, `LeaderDeathReaction.PanicScatter`.

---

### #2. TAM GIÁC ĐỀU (Equilateral Delta)
* **Ý tưởng chiến thuật:** Biểu tượng quân sự kinh điển. 3 cạnh bằng nhau tuyệt đối ($a = 20\text{m}$, góc $60^\circ$). Leader dẫn đầu tại đỉnh nhọn, đáy 3 tàu tạo hàng rào hỏa lực trực diện.
* **Số tàu:** 6 chiếc.

```text
                  [0] Leader (0, 8)
                 /        \
        (-5, 0)[1]          [2] (5, 0)
               /            \
   (-10, -7)[3]-----[4]------[5] (10, -7)
                  (0, -7)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Đỉnh Tiên Phong (Apex Leader) | `(0.0, 8.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.20 |
| **1** | Cạnh Trái (Left Flank) | `(-5.0, 0.0, -1.5)` | Hộ vệ | `ShieldBearer` | 1.00 |
| **2** | Cạnh Phải (Right Flank) | `(5.0, 0.0, -1.5)` | Hộ vệ | `ShieldBearer` | 1.00 |
| **3** | Đáy Cánh Trái (Bottom Left) | `(-10.0, -7.0, -3.0)` | Tiêm kích | `Flanker` | 0.95 |
| **4** | Đáy Trung Tâm (Bottom Center) | `(0.0, -7.0, -3.0)` | Bắn tỉa | `Scout` | 0.95 |
| **5** | Đáy Cánh Phải (Bottom Right) | `(10.0, -7.0, -3.0)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.StationaryHold`, `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.BreakShieldAndEngage`.

---

### #3. TAM GIÁC VUÔNG (Right Wing Wedge)
* **Ý tưởng chiến thuật:** Bức tường chắn thẳng đứng bên mạn trái tạo góc $90^\circ$ hoàn hảo với cạnh đáy, cạnh huyền dốc tạo đường trượt hỏa lực chéo.
* **Số tàu:** 5 chiếc.

```text
   (-8, 6) [1]
      |      \
   (-8, 0) [2]   [4] (1, 0)
      |            \
   (-8,-6) [0]------[3] (10, -6)
          Leader
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Góc Vuông Đáy (Right Angle Leader) | `(-8.0, -6.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.20 |
| **1** | Đỉnh Vách Đứng (Vertical Top) | `(-8.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Giữa Vách Đứng (Vertical Mid) | `(-8.0, 0.0, -0.5)` | Lá chắn | `ShieldBearer` | 1.00 |
| **3** | Đỉnh Đáy Ngang (Horizontal End) | `(10.0, -6.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **4** | Giữa Cạnh Huyền (Hypotenuse Mid) | `(1.0, 0.0, -1.5)` | Cơ động | `Interceptor` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.VerticalUndulation`, `FormationEntryStyle.FlankIn`, `LeaderDeathReaction.KamikazeCharge`.

---

### #4. HÌNH VUÔNG (Iron Square Phalanx)
* **Ý tưởng chiến thuật:** Khung thành 4 cạnh bằng nhau ($16\text{m} \times 16\text{m}$) có tàu chỉ huy trấn thủ chính giữa, 4 góc bọc khiên bảo vệ.
* **Số tàu:** 8 chiếc (4 góc + 4 cạnh, hoặc 1 tâm + 7 viền).

```text
   (-8, 8)[1]------[2](0, 8)------[3](8, 8)
       |                            |
   (-8, 0)[7]     [0] Leader        [4](8, 0)
       |           (0, 0)           |
   (-8,-8)[6]------[5](0,-8)------[X] (8,-8)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Trung Tâm Lõi (Citadel Core) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Góc Tây Bắc (North-West) | `(-8.0, 8.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh Bắc (North Mid) | `(0.0, 8.0, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Góc Đông Bắc (North-East) | `(8.0, 8.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **4** | Đỉnh Đông (East Mid) | `(8.0, 0.0, -1.0)` | Đột kích | `Interceptor` | 0.95 |
| **5** | Đỉnh Nam (South Mid) | `(0.0, -8.0, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **6** | Góc Tây Nam (South-West) | `(-8.0, -8.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **7** | Đỉnh Tây (West Mid) | `(-8.0, 0.0, -1.0)` | Đột kích | `Interceptor` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.CorkscrewRoll`, `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.MaintainFormation`.

---

### #5. HÌNH CHỮ NHẬT (Wide Wall Screen)
* **Ý tưởng chiến thuật:** Khung quét màn hình nằm ngang rộng $24\text{m}$, cao $12\text{m}$ (tương thích tỷ lệ 16:9), tạo thành bức tường lửa chặn đứng mọi góc ngắm của Arwing.
* **Số tàu:** 8 chiếc.

```text
   (-12, 6)[1]------[2](0, 6)------[3](12, 6)
        |                             |
   (-12, 0)[7]      [0] Leader        [4](12, 0)
        |             (0, 0)          |
   (-12,-6)[6]---------------------[5](12,-6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Trung Tâm Chỉ Huy | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Góc Trên Trái (Top-Left) | `(-12.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Cạnh Trên Giữa (Top-Center) | `(0.0, 6.0, -0.5)` | Bắn tỉa | `Scout` | 0.95 |
| **3** | Góc Trên Phải (Top-Right) | `(12.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **4** | Cạnh Phải Giữa (Right-Center) | `(12.0, 0.0, -1.5)` | Đột kích | `Interceptor` | 0.95 |
| **5** | Góc Dưới Phải (Bottom-Right) | `(12.0, -6.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **6** | Góc Dưới Trái (Bottom-Left) | `(-12.0, -6.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **7** | Cạnh Trái Giữa (Left-Center) | `(-12.0, 0.0, -1.5)` | Đột kích | `Interceptor` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.HorizontalSinusoid`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.BreakShieldAndEngage`.

---

### #6. HÌNH BÌNH HÀNH (Skewed Parallelogram)
* **Ý tưởng chiến thuật:** Hai cặp cạnh song song nghiêng góc $60^\circ$. Đội hình tạo ảo giác dịch chuyển chéo khi bay lượn Lissajous.
* **Số tàu:** 7 chiếc.

```text
          (-4, 6)[1]---------[2](6, 6)
          /                     /
   (-5, 0)[6]    [0] Leader   [3](5, 0)
        /          (0, 0)     /
   (-6,-6)[5]---------[4](4, -6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Trọng Tâm Chỉ Huy | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.15 |
| **1** | Đỉnh Trên Trái (Top-Left) | `(-4.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh Trên Phải (Top-Right) | `(6.0, 6.0, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Cạnh Phải Giữa (Right-Mid) | `(5.0, 0.0, -1.5)` | Đột kích | `Interceptor` | 1.00 |
| **4** | Đáy Dưới Phải (Bottom-Right) | `(4.0, -6.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | Đáy Dưới Trái (Bottom-Left) | `(-6.0, -6.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **6** | Cạnh Trái Giữa (Left-Mid) | `(-5.0, 0.0, -1.5)` | Đột kích | `Interceptor` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.Lissajous3D`, `FormationEntryStyle.RearFlyby`, `LeaderDeathReaction.PanicScatter`.

---

### #7. HÌNH THOI (Diamond Guard Elite)
* **Ý tưởng chiến thuật:** 4 cạnh bằng nhau đối xứng tâm, 2 đường chéo vuông góc. Leader ở trung tâm được 4 đỉnh nhọn bao bọc vững chắc.
* **Số tàu:** 5 chiếc.

```text
                  [1] Top (0, 8)
                 /              \
  (-14, 0) [3] Left -- [0] Leader -- [4] Right (14, 0)
                 \              /
                 [2] Bottom (0, -8)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Lõi Kim Cương (Core Leader) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Đỉnh Bắc (North Peak) | `(0.0, 8.0, -1.5)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Đỉnh Nam (South Peak) | `(0.0, -8.0, -1.5)` | Lá chắn | `ShieldBearer` | 1.05 |
| **3** | Đỉnh Tây Cánh Trái (West Wing) | `(-14.0, 0.0, -2.5)` | Tiêm kích | `Flanker` | 1.00 |
| **4** | Đỉnh Đông Cánh Phải (East Wing) | `(14.0, 0.0, -2.5)` | Tiêm kích | `Flanker` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.FigureEight`, `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.RetreatWarp`.

---

### #8. HÌNH THANG (Broad Trapezoid)
* **Ý tưởng chiến thuật:** Đáy bé trên ngắn, đáy lớn dưới mở rộng, 2 cạnh xiên bất đối xứng dồn người chơi vào bẫy hỏa lực dưới đáy.
* **Số tàu:** 6 chiếc.

```text
             (-5, 5)[1]------[2](3, 5)
             /                   \
            /     [0] Leader      \
           /        (0, 0)         \
   (-12,-6)[3]--------[4]----------[5](10, -6)
                    (-2, -6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Trọng Tâm Chỉ Huy | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.15 |
| **1** | Đáy Bé Trái (Top Minor Left) | `(-5.0, 5.0, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **2** | Đáy Bé Phải (Top Minor Right) | `(3.0, 5.0, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Đáy Lớn Góc Trái (Base Left) | `(-12.0, -6.0, -2.5)` | Lá chắn | `ShieldBearer` | 1.00 |
| **4** | Đáy Lớn Trung Gian (Base Mid) | `(-2.0, -6.0, -2.0)` | Bắn tỉa | `Scout` | 0.95 |
| **5** | Đáy Lớn Góc Phải (Base Right) | `(10.0, -6.0, -2.5)` | Lá chắn | `ShieldBearer` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.HorizontalSinusoid`, `FormationEntryStyle.FlankIn`, `LeaderDeathReaction.PanicScatter`.

---

### #9. HÌNH THANG CÂN (Isosceles Bastion)
* **Ý tưởng chiến thuật:** Đáy nhỏ trên, đáy lớn dưới, 2 góc đáy bằng nhau đối xứng gương qua trục Y. Dạng pháo đài phòng thủ kiên cố.
* **Số tàu:** 7 chiếc.

```text
               (-5, 6)[1]---[0]---[2](5, 6)
               /          Leader         \
              /           (0, 6)          \
        (-8, 0)[3]                      [4](8, 0)
            /                                \
   (-12,-6)[5]------------------------------[6](12, -6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Đỉnh Trung Tâm (Crown Leader) | `(0.0, 6.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Đáy Nhỏ Trái (Top Minor Left) | `(-5.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Đáy Nhỏ Phải (Top Minor Right) | `(5.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **3** | Eo Trái (Left Waist) | `(-8.0, 0.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **4** | Eo Phải (Right Waist) | `(8.0, 0.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | Đáy Lớn Cánh Trái (Base Far Left) | `(-12.0, -6.0, -3.0)` | Đột kích | `Interceptor` | 0.95 |
| **6** | Đáy Lớn Cánh Phải (Base Far Right)| `(12.0, -6.0, -3.0)` | Đột kích | `Interceptor` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.StationaryHold`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.MaintainFormation`.

---

### #10. HÌNH THANG VUÔNG (Right Bastion Rampart)
* **Ý tưởng chiến thuật:** Cạnh bên trái vuông góc $90^\circ$ với 2 đáy, tạo vách dựng đứng kiên cố kết hợp đáy lớn kéo dài sang phải.
* **Số tàu:** 6 chiếc.

```text
   (-8, 6)[1]---------[2](-1, 6)
      |                    \
   (-8, 0)[0]               [3](4, 0)
    Leader                   \
   (-8,-6)[4]-----------------[5](10, -6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Vách Trực Diện (Wall Center Leader) | `(-8.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.20 |
| **1** | Đỉnh Vách Vuông (Top Wall) | `(-8.0, 6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Đáy Nhỏ Trên (Top Ridge) | `(-1.0, 6.0, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Sườn Xiên Phải (Right Incline) | `(4.0, 0.0, -2.0)` | Đột kích | `Interceptor` | 1.00 |
| **4** | Đáy Vách Vuông Dưới (Bottom Wall) | `(-8.0, -6.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **5** | Mút Đáy Lớn Phải (Base Right End) | `(10.0, -6.0, -2.5)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.VerticalUndulation`, `FormationEntryStyle.FlankIn`, `LeaderDeathReaction.KamikazeCharge`.

---

### #11. NGŨ GIÁC ĐỀU (Pentagram Citadel)
* **Ý tưởng chiến thuật:** 5 đỉnh cách đều tâm $R = 10\text{m}$ ($\theta_k = 90^\circ + k \cdot 72^\circ$). Leader ngự tại tâm, 5 tàu cánh xoay quanh tạo trận địa phong tỏa sao năm cánh.
* **Số tàu:** 6 chiếc.

```text
                     [1] (0, 10)
                    /     \
    (-9.5, 3.1)[5] /       \ [2] (9.5, 3.1)
           \      [0] Leader  /
            \       (0, 0)   /
  (-5.9, -8.1)[4]-----------[3] (5.9, -8.1)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Tâm Lõi Ngũ Giác (Citadel Core) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Đỉnh Đỉnh Bắc ($90^\circ$) | `(0.0, 10.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh Đông Bắc ($18^\circ$) | `(9.51, 3.09, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Đỉnh Đông Nam ($-54^\circ$) | `(5.88, -8.09, -1.0)` | Đột kích | `Interceptor` | 0.95 |
| **4** | Đỉnh Tây Nam ($-126^\circ$) | `(-5.88, -8.09, -1.0)` | Đột kích | `Interceptor` | 0.95 |
| **5** | Đỉnh Tây Bắc ($162^\circ$) | `(-9.51, 3.09, -1.0)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.Pinwheel_CentroidSpin` (quay $30^\circ/\text{s}$), `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.RetreatWarp`.

---

### #12. LỤC GIÁC ĐỀU (Hexagonal Hive)
* **Ý tưởng chiến thuật:** Kết cấu tổ ong vững chắc nhất trong tự nhiên ($R = 10\text{m}$, $\theta_k = 90^\circ + k \cdot 60^\circ$). 6 lá chắn tạo màng lọc năng lượng bất khả xâm phạm.
* **Số tàu:** 7 chiếc.

```text
                     [1] (0, 10)
                   /             \
    (-8.7, 5.0)[6]                 [2] (8.7, 5.0)
          |          [0] Leader          |
          |            (0, 0)            |
   (-8.7, -5.0)[5]                 [3] (8.7, -5.0)
                   \             /
                     [4] (0, -10)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Chúa Tổ Ong (Hive Queen Leader) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.30 |
| **1** | Đỉnh Bắc ($90^\circ$) | `(0.0, 10.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Đỉnh Đông Bắc ($30^\circ$) | `(8.66, 5.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **3** | Đỉnh Đông Nam ($-30^\circ$) | `(8.66, -5.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **4** | Đỉnh Nam ($-90^\circ$) | `(0.0, -10.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **5** | Đỉnh Tây Nam ($-150^\circ$) | `(-8.66, -5.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **6** | Đỉnh Tây Bắc ($150^\circ$) | `(-8.66, 5.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |

* **Đề xuất chiến thuật:** `FormationMotionType.Pinwheel_CentroidSpin`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.MaintainFormation`.

---

### #13. BÁT GIÁC ĐỀU (Octagonal Ring)
* **Ý tưởng chiến thuật:** 8 cạnh bằng nhau bao quanh tâm ($R = 10\text{m}$, bước góc $45^\circ$). Tương đương vành đai Dyson thu gom năng lượng sao.
* **Số tàu:** 9 chiếc.

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Lõi Năng Lượng (Leader) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Đỉnh Bắc ($90^\circ$) | `(0.0, 10.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh Đông Bắc ($45^\circ$) | `(7.07, 7.07, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Đỉnh Đông ($0^\circ$) | `(10.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **4** | Đỉnh Đông Nam ($-45^\circ$) | `(7.07, -7.07, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | Đỉnh Nam ($-90^\circ$) | `(0.0, -10.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **6** | Đỉnh Tây Nam ($-135^\circ$) | `(-7.07, -7.07, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **7** | Đỉnh Tây ($180^\circ$) | `(-10.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **8** | Đỉnh Tây Bắc ($135^\circ$) | `(-7.07, 7.07, -1.0)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.CorkscrewRoll`, `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.KamikazeCharge`.

---

### #14. THẬP GIÁC ĐỀU (Decagon Decashield)
* **Ý tưởng chiến thuật:** 10 đỉnh cách đều tâm $R = 11\text{m}$ ($\Delta \theta = 36^\circ$). Tạo bức màn phòng thủ hình đa giác gần tiệm cận hình tròn.
* **Số tàu:** 11 chiếc.

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Lõi Chỉ Huy (Central Decagon Core) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.30 |
| **1** | Đỉnh 1 ($90^\circ$) | `(0.0, 11.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh 2 ($54^\circ$) | `(6.47, 8.90, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **3** | Đỉnh 3 ($18^\circ$) | `(10.46, 3.40, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **4** | Đỉnh 4 ($-18^\circ$) | `(10.46, -3.40, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **5** | Đỉnh 5 ($-54^\circ$) | `(6.47, -8.90, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **6** | Đỉnh 6 ($-90^\circ$) | `(0.0, -11.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **7** | Đỉnh 7 ($-126^\circ$) | `(-6.47, -8.90, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **8** | Đỉnh 8 ($-162^\circ$) | `(-10.46, -3.40, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **9** | Đỉnh 9 ($162^\circ$) | `(-10.46, 3.40, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **10**| Đỉnh 10 ($126^\circ$) | `(-6.47, 8.90, -1.0)` | Tiêm kích | `Flanker` | 0.90 |

* **Đề xuất chiến thuật:** `FormationMotionType.FigureEight`, `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.BreakShieldAndEngage`.

---

### #15. n-GIÁC ĐỀU TỔNG QUÁT (Parametric Regular n-Gon / Star Dodecahedron)
* **Ý tưởng chiến thuật:** Sử dụng công thức toán học tham số tổng quát:
  $$X_k = R \cdot \cos\left(\frac{2\pi k}{n} + \phi\right), \quad Y_k = R \cdot \sin\left(\frac{2\pi k}{n} + \phi\right)$$
  Cấu hình mẫu áp dụng **Thập Nhị Giác Đều (Dodecahedron - $n = 12$, $R = 12\text{m}$, $\Delta\theta = 30^\circ$)** đại diện cho đại hạm đội hắc tinh.
* **Số tàu:** 13 chiếc (1 Leader tại tâm + 12 tàu bao quanh).

#### Ma trận tọa độ cục bộ (Slot Matrix mẫu $n = 12$):
| Slot | Góc $\theta$ | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|:---:|:---:|:---:|---|:---:|
| **0** | Tâm | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.35 |
| **1** | $90^\circ$ | `(0.0, 12.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | $60^\circ$ | `(6.0, 10.39, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **3** | $30^\circ$ | `(10.39, 6.0, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **4** | $0^\circ$ | `(12.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **5** | $-30^\circ$ | `(10.39, -6.0, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **6** | $-60^\circ$ | `(6.0, -10.39, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **7** | $-90^\circ$ | `(0.0, -12.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **8** | $-120^\circ$ | `(-6.0, -10.39, -1.0)` | Tiêm kích | `Flanker` | 0.90 |
| **9** | $-150^\circ$ | `(-10.39, -6.0, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **10**| $180^\circ$ | `(-12.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **11**| $150^\circ$ | `(-10.39, 6.0, -1.0)` | Đột kích | `Interceptor` | 0.90 |
| **12**| $120^\circ$ | `(-6.0, 10.39, -1.0)` | Tiêm kích | `Flanker` | 0.90 |

* **Đề xuất chiến thuật:** `FormationMotionType.Lissajous3D`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.RetreatWarp`.

---

### #16. HÌNH TRÒN (Orbital Halo Swarm)
* **Ý tưởng chiến thuật:** Tập hợp điểm cách đều tâm $R = 9\text{m}$. Khác với đa giác nhọn, hình tròn có mật độ phân bố đồng đều, xoay tròn theo trục Z tạo thành khiên cầu động.
* **Số tàu:** 9 chiếc.

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Lõi Thiên Thể (Halo Core) | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Đỉnh $90^\circ$ | `(0.0, 9.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Đỉnh $45^\circ$ | `(6.36, 6.36, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Đỉnh $0^\circ$ | `(9.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **4** | Đỉnh $-45^\circ$ | `(6.36, -6.36, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | Đỉnh $-90^\circ$ | `(0.0, -9.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **6** | Đỉnh $-135^\circ$ | `(-6.36, -6.36, -1.0)` | Tiêm kích | `Flanker` | 0.95 |
| **7** | Đỉnh $180^\circ$ | `(-9.0, 0.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **8** | Đỉnh $135^\circ$ | `(-6.36, 6.36, -1.0)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.CorkscrewRoll`, `FormationEntryStyle.RearFlyby`, `LeaderDeathReaction.RetreatWarp`.

---

### #17. HÌNH ELLIPSE (Elliptic Eclipse)
* **Ý tưởng chiến thuật:** Hình bầu dục dẹt theo phương ngang với bán trục lớn $a = 14\text{m}$ và bán trục nhỏ $b = 7\text{m}$ theo công thức $\frac{x^2}{a^2} + \frac{y^2}{b^2} = 1$. Mô phỏng đĩa thiên thạch quỹ đạo elip Kepler.
* **Số tàu:** 9 chiếc.

```text
                     [1] (0, 7)
               /                    \
   (-10, 4.9)[8]                    [2] (10, 4.9)
        |                                |
(-14, 0)[7]         [0] Leader           [3] (14, 0)
        |             (0, 0)             |
   (-10,-4.9)[6]                    [4] (10, -4.9)
               \                    /
                    [5] (0, -7)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Góc $\theta$ | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|:---:|:---:|:---:|---|:---:|
| **0** | Tiêu Điểm / Tâm | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | $90^\circ$ | `(0.0, 7.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | $45^\circ$ | `(9.90, 4.95, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | $0^\circ$ | `(14.0, 0.0, -2.0)` | Đột kích | `Interceptor` | 1.00 |
| **4** | $-45^\circ$ | `(9.90, -4.95, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | $-90^\circ$ | `(0.0, -7.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.00 |
| **6** | $-135^\circ$ | `(-9.90, -4.95, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **7** | $180^\circ$ | `(-14.0, 0.0, -2.0)` | Đột kích | `Interceptor` | 1.00 |
| **8** | $135^\circ$ | `(-9.90, 4.95, -1.5)` | Tiêm kích | `Flanker` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.Lissajous3D`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.PanicScatter`.

---

### #18. HÌNH QUẠT TRÒN (Radial Fan Sweep)
* **Ý tưởng chiến thuật:** Miền hình quạt giới hạn bởi góc mở $90^\circ$ ($45^\circ \to 135^\circ$) với Leader tại tâm góc nhọn đáy, các nan quạt xòe rộng về phía trên đón đầu người chơi.
* **Số tàu:** 7 chiếc.

```text
   (-8, 5)[3]----(-4, 7)[4]----(0, 8)[5]----(4, 7)[6]----(8, 5)[7] (Vòng Cung)
          \            |          |          |           /
           \           |          |          |          /
            \--(-4, 0)[1]---------+---------[2](4, 0)--/  (Nan Quạt)
                                  |
                              [0] Leader
                               (0, -6)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Tâm Quạt (Fan Pivot Leader) | `(0.0, -6.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Nan Trái (Left Spoke) | `(-4.0, 0.0, -1.0)` | Hộ vệ | `ShieldBearer` | 1.00 |
| **2** | Nan Phải (Right Spoke) | `(4.0, 0.0, -1.0)` | Hộ vệ | `ShieldBearer` | 1.00 |
| **3** | Mép Cung Trái (Arc Far Left) | `(-8.0, 5.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **4** | Cung Trái (Arc Left) | `(-4.0, 7.0, -2.0)` | Bắn tỉa | `Scout` | 0.95 |
| **5** | Đỉnh Vòm Cung (Arc Crown) | `(0.0, 8.0, -2.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **6** | Cung Phải (Arc Right) | `(4.0, 7.0, -2.0)` | Bắn tỉa | `Scout` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.HorizontalSinusoid`, `FormationEntryStyle.FlankIn`, `LeaderDeathReaction.KamikazeCharge`.

---

### #19. HÌNH VÀNH KHUYÊN (Dual Ring Annulus)
* **Ý tưởng chiến thuật:** Hai vòng tròn đồng tâm: Vòng trong $R_1 = 5\text{m}$ (4 lá chắn bảo vệ sát sườn) và Vòng ngoài $R_2 = 11\text{m}$ (6 tiêm kích quay so le). Tạo thành pháo đài 2 lớp phòng ngự dày đặc.
* **Số tàu:** 11 chiếc.

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Lớp Vòng | Góc $\theta$ | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|:---:|:---:|:---:|:---:|---|:---:|
| **0** | Lõi | Tâm | `(0.0, 0.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.30 |
| **1** | Trong ($R_1=5$) | $90^\circ$ | `(0.0, 5.0, -0.8)` | Lá chắn | `ShieldBearer` | 1.00 |
| **2** | Trong ($R_1=5$) | $0^\circ$ | `(5.0, 0.0, -0.8)` | Lá chắn | `ShieldBearer` | 1.00 |
| **3** | Trong ($R_1=5$) | $-90^\circ$ | `(0.0, -5.0, -0.8)` | Lá chắn | `ShieldBearer` | 1.00 |
| **4** | Trong ($R_1=5$) | $180^\circ$ | `(-5.0, 0.0, -0.8)` | Lá chắn | `ShieldBearer` | 1.00 |
| **5** | Ngoài ($R_2=11$) | $90^\circ$ | `(0.0, 11.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **6** | Ngoài ($R_2=11$) | $30^\circ$ | `(9.53, 5.50, -2.0)` | Đột kích | `Interceptor` | 0.95 |
| **7** | Ngoài ($R_2=11$) | $-30^\circ$ | `(9.53, -5.50, -2.0)` | Đột kích | `Interceptor` | 0.95 |
| **8** | Ngoài ($R_2=11$) | $-90^\circ$ | `(0.0, -11.0, -2.0)` | Tiêm kích | `Flanker` | 0.95 |
| **9** | Ngoài ($R_2=11$) | $-150^\circ$ | `(-9.53, -5.50, -2.0)` | Đột kích | `Interceptor` | 0.95 |
| **10**| Ngoài ($R_2=11$) | $150^\circ$ | `(-9.53, 5.50, -2.0)` | Đột kích | `Interceptor` | 0.95 |

* **Đề xuất chiến thuật:** `FormationMotionType.Pinwheel_CentroidSpin` (vòng trong & ngoài tự quay quanh tâm), `FormationEntryStyle.FrontalHorizon`, `LeaderDeathReaction.MaintainFormation`.

---

### #20. HÌNH VIÊN PHÂN (Crescent Bow Segment)
* **Ý tưởng chiến thuật:** Miền hình học khép kín giới hạn bởi một cung tròn phía trên và một dây cung ngang bên dưới. Tựa như cây cung vũ trụ với Leader nằm ngay trung tâm dây cung kéo căng.
* **Số tàu:** 7 chiếc.

```text
                  (-5, 6)[2]----(0, 7.5)[3]----(5, 6)[4]   (Đỉnh Cung Tròn)
                 /                                      \
   (-10, 2)[1] /                                          \ [5] (10, 2)
       |===== [6](-5, 2) ===== [0] Leader ===== [X](5, 2) =====|
                                 (0, 2)
                               (Dây Cung Ngang)
```

#### Ma trận tọa độ cục bộ (Slot Matrix):
| Slot | Tên vị trí | Tọa độ Local Offset $(X, Y, Z)$ | Vai trò | Archetype | Scale |
|:---:|---|:---:|:---:|---|:---:|
| **0** | Tâm Dây Cung (Bowstring Center Leader) | `(0.0, 2.0, 0.0)` |  Chỉ huy | `SquadLeader` | 1.25 |
| **1** | Mút Cung Trái (Left Notch) | `(-10.0, 2.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **2** | Cung Tròn Trái (Left Arch) | `(-5.0, 6.0, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **3** | Đỉnh Cung (Crown Peak) | `(0.0, 7.5, -2.0)` | Đột kích | `Interceptor` | 1.00 |
| **4** | Cung Tròn Phải (Right Arch) | `(5.0, 6.0, -1.5)` | Tiêm kích | `Flanker` | 0.95 |
| **5** | Mút Cung Phải (Right Notch) | `(10.0, 2.0, -1.0)` | Lá chắn | `ShieldBearer` | 1.05 |
| **6** | Dây Cung Trái (Left String Guard) | `(-5.0, 2.0, -0.5)` | Hộ vệ | `ShieldBearer` | 1.00 |

* **Đề xuất chiến thuật:** `FormationMotionType.VerticalUndulation`, `FormationEntryStyle.OverheadDive`, `LeaderDeathReaction.BreakShieldAndEngage`.

---

## 3. HƯỚNG DẪN TÍCH HỢP VÀO GAMEPLAY & MÀN CHƠI

Mỗi hình khối trên tương ứng với 1 file ScriptableObject `FormationPattern.asset`. Để tích hợp vào màn chơi, Game Designer có 2 cách thức thực hiện trong Unity Editor:

### Cách 1: Thiết lập theo kịch bản Timeline (`StageTimelineEvent`)
1. Chọn file màn chơi (ví dụ: `StageData_01.asset` hoặc `StageData_02.asset`).
2. Trong danh sách **Timeline Events**, nhấn `+` để thêm sự kiện:
   - **Event Name:** `Wave_Geometry_01_Square` (hoặc tên tương ứng).
   - **Event Type:** `SpawnScriptedSquad`.
   - **Trigger Mode:** `TimeInSeconds` (hoặc `DistanceInMeters`).
   - **Trigger Value:** Ví dụ `35.0` (tại giây thứ 35).
   - **Scripted Formation Pattern:** Kéo thả file `Formation_Square.asset`.
   - **Wait Mode:** `WaitForClearOrTimeout` (chờ dọn sạch phi đội hoặc sau 14s mới kích hoạt wave tiếp theo).

### Cách 2: Thiết lập theo Bể bơi ngẫu nhiên của màn chơi (`StageEnemySettings`)
1. Trong mục **Enemy Settings** của `StageData`:
   - Bật `Enable Formations = true`.
   - Đặt `Formation Interval Waves = 2` hoặc `3` (cứ sau 2 - 3 đợt lính lẻ sẽ có 1 đợt bay hình khối).
2. Kéo thả các mẫu hình học muốn xuất hiện vào mảng **Stage Formation Pool**.
3. Bộ điều phối [`VirtualRailWaveSpawner.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWaveSpawner.cs) sẽ tự động luân chuyển và sinh các hình khối theo chu kỳ.
