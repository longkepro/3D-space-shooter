# TÀI LIỆU NGHIỆM THU: CINEMATIC DESIGNER MOTION LOOP (STAGE 1)

> **Dự án**: 3D Space Shooter - Dynamic Cosmic Background  
> **Nguồn ảnh**: [`stage1.png`](file:///d:/3D-Shooter-master/GameBackgroundPictures/stage1.png)  
> **Tệp video đích**: [`stage1_cinematic_designer.mp4`](file:///d:/3D-Shooter-master/GameBackgroundPictures/stage1_cinematic_designer.mp4)  
> **Bản sao Downloads**: [`stage1_cinematic_designer.mp4`](file:///C:/Users/ADMIN/Downloads/stage1_cinematic_designer.mp4)  
> **Kịch bản thực thi**: [`create_stage1_cinematic_designer_video.py`](file:///d:/3D-Shooter-master/GameBackgroundPictures/create_stage1_cinematic_designer_video.py)  

---

## 1. TỔNG QUAN & NGUYÊN TẮC THIẾT KẾ ĐIỆN ẢNH (CINEMAGRAPH REALISM)

Bức ảnh Stage 1 có bố cục không gian sâu cực kỳ ấn tượng với 3 thực thể chính: Hành tinh khí quyển khổng lồ tiền cảnh (foreground), Vệ tinh băng giá ở trung cảnh (midground), và Ngôi sao siêu khổng lồ (hypergiant star) cùng dải lóe sáng Anamorphic flare trên nền vũ trụ sâu (background).

Thay vì áp dụng các hiệu ứng xoay cầu 3D giả lập khiến bề mặt méo mó hoặc "giả trân", phương pháp **Hollywood Cinemagraph Designer** tuân thủ triệt để 5 trụ cột vật lý và thị giác điện ảnh:

1. **Parallax đa tầng vi mô (Sub-pixel Camera Parallax):** Mô phỏng góc nhìn từ khoang lái tàu vũ trụ đang trôi chậm rãi trong quỹ đạo. Tốc độ trôi dạt phụ thuộc chính xác vào cự ly phối cảnh: tiền cảnh trôi nhanh hơn ($0.8\text{ px/s}$), trung cảnh trôi trung bình ($0.3\text{ px/s}$), hậu cảnh sâu trôi siêu chậm ($0.6\text{ px/s}$).
2. **Không quay hành tinh dạng quả bóng bida:** Giữ nguyên 100% hình học bề mặt, các miệng hố va chạm và cấu trúc thạch quyển của vệ tinh băng; loại bỏ hoàn toàn hiện tượng méo UV.
3. **Mây tầng đối lưu trôi trượt tự nhiên (Puppet-Pin Cloud Advection):** Các dải mây khí quyển chuyển động cuộn xoáy mượt mà theo phương tiếp tuyến với độ cong chân trời, triệt tiêu vận tốc ở đường ranh giới tối (terminator line) và tầng đối lưu cao.
4. **Đồng bộ quang học toàn cục (Global Optical Lighting Sync):** Khi ngôi sao mẹ thở/nhấp nháy với chu kỳ $T=4.0\text{s}$ ($\pm 2.8\%$), quầng sáng Halo, chùm tia Anamorphic, viền sáng Rim Light của mặt trăng và viền sáng khí quyển của hành tinh lớn bừng sáng **hoàn toàn đồng pha**.
5. **Sự kiện vũ trụ tinh tế & Xử lý hậu kỳ Analog:** Một vệt sao băng duy nhất xuất hiện tại thời điểm $t=6.0\text{s}$, các vì sao lấp lánh quang học hữu cơ, lớp nhiễu hạt phim 35mm ($1.2\%$) và góc tối quang học (Optical Vignette) chuẩn điện ảnh.

---

## 2. THÔNG SỐ KỸ THUẬT VIDEO

| Thông số | Giá trị chi tiết | Ghi chú kỹ thuật |
| :--- | :--- | :--- |
| **Độ phân giải** | **2K QHD ($2560 \times 1440$)** | Tỷ lệ vàng 16:9, chuẩn sắc nét cho màn hình gaming & desktop |
| **Tốc độ khung hình** | **30.0 FPS** | Đảm bảo chuyển động mượt mà điện ảnh, không giật lag |
| **Thời lượng vòng lặp** | **12.0 giây (360 frames)** | Vòng lặp tuần hoàn toán học hoàn hảo ($e^{i \omega_0 t}$ với $\omega_0 = \frac{2\pi}{12.0}$) |
| **Điểm nối vòng lặp** | **Seamless Loop 100%** | Độ lệch quang học giữa Frame 0 và Frame 359 tiến về 0 |
| **Chu kỳ nhịp thở sao** | **4.0 giây ($T = 4.0\text{s}$)** | Đúng chính xác 3 nhịp thở trọn vẹn trong 12 giây |
| **Chu kỳ trôi dạt mây** | **12.0 giây ($T = 12.0\text{s}$)** | Khép kín chu kỳ trường vector dịch chuyển |
| **Bộ mã hóa** | **H.264 High Profile (libx264)** | CRF 17, `preset=slow`, `pix_fmt=yuv420p`, GOP = 30 |

---

## 3. BẢNG PHÂN TÍCH CHI TIẾT TỪNG LỚP (LAYER BREAKDOWN)

```
+-------------------------------------------------------------------------+
| [Layer 3: Background - Deep Space]                                      |
| - Ngôi sao mẹ siêu khổng lồ (Hypergiant Star): Nhịp thở ±2.8% (T=4.0s)  |
| - Dải lóe sáng ngang (Anamorphic Streak Flare): Co giãn ±5.0%           |
| - Khói tinh vân vũ trụ (Ethereal Nebula Smoke): Sóng điều hòa 6s/12s    |
| - Sao nền lấp lánh (Organic Scintillation): Fade độ trong suốt tuần hoàn|
| - Sao băng đơn lẻ (Lone Meteor Streak): Lướt qua tại t=5.6s - 6.4s     |
| - Camera Parallax Drift: 0.6 px/s (Biên độ 3.6px)                       |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
| [Layer 2: Midground - Icy Satellite Moon]                               |
| - Tách lớp & Inpainting Clean Plate: Loại bỏ hoàn toàn viền xé rách    |
| - Quỹ đạo trôi dạt vi mô (Micro-Drift): 0.3 px/s (Biên độ 1.8px)        |
| - Bề mặt thạch quyển đóng băng: Cố định 100%, KHÔNG xoay bida giả lập   |
| - Viền sáng Rim Light: Tăng giảm đồng pha với nhịp thở ngôi sao mẹ      |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
| [Layer 1: Foreground - Giant Blue Planet]                               |
| - Khí quyển & Đường chân trời (Atmospheric Horizon):                    |
|   + Rim Light xanh lam bừng sáng đồng pha với ngôi sao mẹ                |
|   + Parallax trôi dạt tiền cảnh: 0.8 px/s (Biên độ 4.8px)               |
| - Mây tầng đối lưu (Tropospheric Clouds):                               |
|   + Biến dạng trường vector trôi trượt tự nhiên (Puppet-Pin)            |
|   + Vận tốc tiếp tuyến đỉnh 1.5 px/frame tại vĩ độ trung tâm            |
|   + Triệt tiêu tại viền chân trời và đường ranh giới râm tối            |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
| [Master Color Grading & Optical Post-Processing]                        |
| - 35mm Analog Film Grain: Biên độ 1.2%, triệt tiêu dải màu (banding)    |
| - Optical Vignette: Suy giảm ánh sáng mép ống kính (0.82 tại 4 góc)     |
+-------------------------------------------------------------------------+
```

---

## 4. HƯỚNG DẪN KIỂM TRA & SỬ DỤNG

1. **Xem trực tiếp trên Windows:**
   - Mở thư mục Downloads cá nhân: [`C:\Users\ADMIN\Downloads\stage1_cinematic_designer.mp4`](file:///C:/Users/ADMIN/Downloads/stage1_cinematic_designer.mp4).
   - Video tương thích 100% với Windows Media Player, VLC, Premiere Pro, After Effects, DaVinci Resolve.
2. **Kiểm tra tính tuần hoàn Seamless Loop:**
   - Bật chế độ "Repeat" / "Lặp lại" trên trình phát media.
   - Quan sát thời điểm chuyển từ giây 11.9 sang giây 0.0: Không có bất kỳ hiện tượng giật hình (stutter), đứt đoạn ánh sáng hay xê dịch tọa độ nào.
3. **Tích hợp vào Unity Engine:**
   - Đưa video vào `Assets/Resources/` hoặc gắn vào thành phần `UnityEngine.Video.VideoPlayer` trong Scene nền vũ trụ với chế độ `Is Looping = true`.
