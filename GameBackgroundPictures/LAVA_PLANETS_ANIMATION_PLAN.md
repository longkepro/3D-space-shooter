# KẾ HOẠCH CẢI TIẾN DIỄN HOẠT ẢNH ĐỘNG HÀNH TINH DUNG NHAM (V2)
> **Asset gốc:** [`lava_planets_4k_lossless.png`](file:///D:/3D-Shooter-master/GameBackgroundPictures/lava_planets_4k_lossless.png)  
> **Mục tiêu:** Tạo video loop $12.0\text{ giây}$ chuẩn $2\text{K QHD } (2560 \times 1440 \text{ px})$, $30\text{ fps}$, đạt độ chân thực vật lý cao, bảo tồn $100\%$ độ nét chi tiết và không bao giờ bị khựng khi lặp lại (Seamless 100%).

---

## I. TỔNG QUAN NGUYÊN TẮC CẢI TIẾN

| Hạng mục | Phiên bản cũ (V1) | Phiên bản cải tiến (V2) |
| :--- | :--- | :--- |
| **Mặt trăng** | Lắc 2D $\pm 1.5^\circ$, biên độ $1-2\text{ px}$ $\implies$ Mắt thấy bất động | **Nhân bản $360^\circ$ (Duplicate Texture Wrap), tự quay 1 chiều vĩnh cửu trên mặt cầu 3D, nét đanh $100\%$, không bóng ma** |
| **Dòng chảy Magma** | Rãnh hẹp đơn sắc, bị lấn át bởi nhấp nháy sáng/tối $\implies$ Mắt chỉ thấy bật/tắt đèn | **Dòng sông magma cuộn trôi rõ rệt trong lòng rãnh nứt ($36-44\text{ px/cycle}$), có vân nhiệt đối lưu, giảm nhấp nháy chói mắt** |
| **Bờ đá nứt** | Dễ bị hiểu lầm/co giãn như cao su | **Khóa cứng $100\%$ vỏ đá bazan đen. Chỉ có chất lỏng magma di chuyển bên trong bờ đá cố định** |
| **Hệ màu sắc** | Đề xuất đổi màu vàng bị bác bỏ | **Giữ nguyên $100\%$ dải màu đỏ cam nguyên bản của ảnh gốc (Lựa chọn A)** |
| **Vòng lặp** | 12s Seamless Loop | **12s Seamless Loop khép kín toán học tuyệt đối ($0$ frame khựng)** |

---

## II. CHI TIẾT KỸ THUẬT 6 TẦNG DIỄN HOẠT

### 1. Hành tinh vệ tinh (Mặt trăng nhỏ) - Đột phá kỹ thuật Duplicate $360^\circ$
* **Cơ chế Unwrap & Mirror Wrap:**
  * Trích xuất nửa bán cầu nhìn thấy ($180^\circ$ mặt trước) của mặt trăng từ ảnh gốc.
  * Nhân bản (duplicate) và lật đối xứng sang nửa bán cầu phía sau ($180^\circ$ mặt sau), hàn mịn $2$ đường kinh tuyến mép nối để tạo thành một khối cầu có vân bề mặt hoàn chỉnh $360^\circ$.
* **Chuyển động tự quay 1 chiều:**
  * Toàn bộ quả cầu tự quay liên tục từ **Trái sang Phải** với chu kỳ góc đồng bộ chính xác với thời lượng $12.0\text{ giây}$.
  * Sử dụng công thức chiếu trực giao hình cầu (*Orthographic Sphere Projection*): Vân đá ở giữa tâm trôi nhanh nhất, càng ra sát viền mép càng nén dẹp lại theo luật phối cảnh lượng giác $\cos(\lambda)$ và cuộn khuất dần ra mặt sau.
* **Độ nét 100% (Zero Ghosting):**
  * Tuyệt đối không dùng kỹ thuật hòa trộn cross-fade giữa 2 ảnh. Mọi thời điểm đều lấy mẫu trực tiếp từ texture gốc $\implies$ **Từng miệng núi lửa sắc nhọn, gờ đá bazan nét đanh như ảnh 4K gốc**.
* **Tách rời Shading & Ánh sáng:**
  * Nửa trên bên trái của mặt trăng luôn được chiếu sáng bởi Mặt Trời, nửa dưới bên phải luôn chìm trong bóng tối.
  * Khi mặt trăng tự quay, các miệng núi lửa lần lượt trôi từ vùng sáng sang vùng tối và ngược lại một cách tự nhiên.

---

### 2. Hành tinh trung tâm - Dòng nham thạch cuộn trôi bên trong lòng rãnh cố định
* **Khóa cứng bờ đá đen bazan (Fixed Crust):**
  * Toàn bộ các mảng kiến tạo đất đá đen, ranh giới nứt gãy và các lục địa nguội lạnh được **ghim chết $100\%$**, giữ nguyên độ kiên cố vĩnh cửu của hành tinh đất đá, tuyệt đối không co giãn hay đóng mở mép rãnh.
* **Dòng chảy Magma tương phản cao (High-Contrast Viscous Advection):**
  * Tách riêng dòng chất lỏng đỏ cam bên trong lòng rãnh nứt.
  * Bổ sung các vệt vân nhiệt trôi dạt dọc theo các con sông magma lớn ở bán cầu phải với vận tốc hiển thị rõ rệt bằng mắt thường ($36 \to 44\text{ px/cycle}$).
  * Tại các hồ magma lớn: Tạo các cuộn xoáy đối lưu nhiệt (*Thermal Convective Churn*), chất lỏng nóng trào lên từ tâm hồ và cuộn tản sang hai bên mép đá.
* **Giảm nhấp nháy sáng/tối toàn cục:**
  * Hạ biên độ nhấp nháy toàn cục từ $\pm 22\%$ xuống mức thở êm dịu $\pm 6\%$, loại bỏ cảm giác "bật/tắt bóng đèn" và tôn lên chuyển động cơ học của dòng chảy nham thạch.
  * **Bảo toàn 100% màu sắc:** Giữ nguyên vẹn sắc đỏ cam đượm lửa nguyên bản (Lựa chọn A).

---

### 3. Hành tinh tiền cảnh (Góc dưới bên phải)
* **Chuyển động Parallax 3D:**
  * Khối hành tinh tiền cảnh trượt góc nhẹ ($4-5\text{ px}$ theo quỹ đạo elip khép kín), tạo cảm giác camera hoặc phi thuyền đang lướt trong không gian có chiều sâu nhiều tầng.
* **Bề mặt:**
  * Các đốm dung nham ở góc dưới phải có dòng chảy vi mô êm dịu bên trong lòng rãnh.
  * Lớp mây trắng tầng tích trên khí quyển trôi chậm rãi theo độ cong bề mặt.

---

### 4. Ngôi sao sáng (Góc trên bên trái) & Ánh sáng viền (Atmospheric Rim Light)
* **Quầng sáng & Tia nhiễu xạ:**
  * Quầng hào quang mặt trời (*Solar Halo*) và các tia sáng (*Diffraction Spikes*) co giãn nhẹ nhàng $\pm 8\%$ theo nhịp thở chu kỳ $6.0\text{ giây}$.
* **Đồng bộ quang học:**
  * Dải sáng trắng xanh cực gắt ôm lấy đường cong khí quyển của hành tinh lớn bừng sáng đồng pha với nhịp thở của mặt trời, mô phỏng chân thực hiệu ứng bùng lóa quang học (*Lens bloom*).

---

### 5. Khí quyển tro bụi & Bầu trời sao (Background)
* **Khói bụi núi lửa:**
  * Lớp tro bụi và khói sulfur mờ xám ở bán cầu tây trôi dạt êm dịu theo luồng gió tầng cao của hành tinh.
* **Sao lấp lánh có chọn lọc:**
  * Hơn 80 ngôi sao nhỏ trong khoảng không gian sâu lấp lánh độc lập bằng các hàm điều hòa lệch pha nhau.

---

## III. THÔNG SỐ KỸ THUẬT & NGHIỆM THU

* **File xuất:** [`lava_planets_animated_loop.mp4`](file:///D:/3D-Shooter-master/GameBackgroundPictures/lava_planets_animated_loop.mp4) (đồng thời lưu tại thư mục `Downloads`).
* **Độ phân giải:** $2560 \times 1440\text{ px}$ (2K QHD 16:9).
* **Tốc độ khung hình:** $30\text{ fps}$ (Tổng cộng 360 frames).
* **Thời lượng:** Đúng $12.0\text{ giây}$.
* **Tiêu chí nghiệm thu:**
  1. Mặt trăng: Nhìn thấy rõ miệng núi lửa trượt ngang từ trái sang phải trên khối cầu 3D, nét đanh từng pixel, không bóng ma.
  2. Dung nham: Nhìn thấy rõ các dòng chảy magma cuộn trôi trong lòng rãnh nứt, không còn bị chớp nháy nhức mắt.
  3. Bờ đá: Đứng yên kiên cố.
  4. Vòng lặp: Bật Loop chạy liên tục $0$ frame giật, $0$ khựng.
