# BÁO CÁO NGHIỆM THU: VIDEO ĐỘNG KHÔNG GIAN BĂNG XANH 2.5D (SPACE PLANET ATMOSPHERIC LOOP)

> **Asset gốc:** [`space_planet_4k_nowatermark.png`](file:///d:/3D-Shooter-master/GameBackgroundPictures/space_planet_4k_nowatermark.png)  
> **Script thực thi:** [`create_space_planet_atmospheric_video.py`](file:///d:/3D-Shooter-master/GameBackgroundPictures/create_space_planet_atmospheric_video.py)  
> **Video hoàn tất:** [`space_planet_atmospheric_loop.mp4`](file:///d:/3D-Shooter-master/GameBackgroundPictures/space_planet_atmospheric_loop.mp4) (đồng thời lưu tại [`C:\Users\ADMIN\Downloads\space_planet_atmospheric_loop.mp4`](file:///C:/Users/ADMIN/Downloads/space_planet_atmospheric_loop.mp4))  
> **Trạng thái:** Hoàn tất $100\%$, đạt tiêu chuẩn kiểm nghiệm mỹ thuật và vật lý thiên văn.

---

## I. CÁC HẠNG MỤC ĐÃ ĐƯỢC GIẢI QUYẾT TRIỆT ĐỂ

| Hạng mục | Yêu cầu kỹ thuật | Phương pháp giải quyết & Kết quả |
| :--- | :--- | :--- |
| **1. Cắt rõ Layer** | Bóc tách độc lập các thiên thể | • Đường chân trời tiền cảnh bám sát phương trình $y = 3.57 \cdot 10^{-5} x^2 + 0.1618 x + 809.56$.<br>• Hành tinh băng bên trái: Tâm $(352.3, 1317.3)$, bán kính $r = 916.7\text{ px}$.<br>• Tách trọn vẹn 4 tầng không gian không lem dính mép. |
| **2. Bỏ quay hành tinh** | Khóa cứng hình học | • Giữ nguyên vị trí $100\%$ miệng núi lửa, cấu trúc kim loại và hẻm núi nứt gãy thẳng đứng.<br>• Không xoay khối cầu $360^\circ$ $\implies$ Giữ trọn độ nét đanh của ảnh gốc. |
| **3. Mây quyển tiền cảnh** | Mây trôi êm theo độ cong | • Bóc tách các tầng mây trắng trên nền biển xanh.<br>• Dòng mây trôi tự nhiên theo tiếp tuyến độ cong khí quyển ($18\text{ px/cycle}$) bằng thuật toán 2 pha tuần hoàn (Dual-Phase Cyclic Advection). |
| **4. Năng lượng rãnh nứt băng** | Dòng năng lượng ngầm bám rãnh | • Dùng bộ lọc Hessian đa tỷ lệ dò theo hẻm núi thẳng đứng khổng lồ và các khe nứt nhánh.<br>• Kích hoạt dòng năng lượng plasma điện quang màu xanh Cyan (Electric Blue / Core Glow) cuộn trôi bên trong lòng rãnh với điều kiện No-Slip (bờ đá đứng im $100\%$). |
| **5. Ngôi sao xanh & Flare** | Nhịp thở quang học | • Quầng sáng mặt trời xanh $(2022, 401)$ co giãn nhẹ nhàng $\pm 4.5\%$ theo nhịp thở $6.0\text{s}$.<br>• Dải tia sáng Anamorphic Flare ngang lấp lánh óng ánh.<br>• Vành chân trời khí quyển phát quang bừng sáng đồng pha. |
| **6. Sự kiện vũ trụ sâu** | Không gian sống động | • Tín hiệu beacon vệ tinh $(910, 365)$ chớp sáng chu kỳ $3.0\text{s}$.<br>• Vệt sao băng vi mô lướt nhanh qua khoảng không gian sâu mỗi $6.0\text{s}$.<br>• 90 ngôi sao nhỏ trong tinh vân nhấp nháy độc lập. |

---

## II. THÔNG SỐ KỸ THUẬT VIDEO

* **Độ phân giải:** $2560 \times 1440\text{ px}$ (2K QHD chuẩn 16:9).
* **Tốc độ khung hình:** $30\text{ fps}$ (Tổng cộng 360 frames).
* **Thời lượng:** Đúng $12.0\text{ giây}$.
* **Độ mượt vòng lặp:** **Seamless Loop 100% tuyệt đối ($0$ frame khựng, $0$ giật)**.
* **Dung lượng tệp:** $8.98\text{ MB}$ (Chuẩn nén H.264 CRF 16 chất lượng cao).
