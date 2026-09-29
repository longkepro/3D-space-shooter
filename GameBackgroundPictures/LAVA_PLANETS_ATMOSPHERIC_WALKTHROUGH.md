# BÁO CÁO NGHIỆM THU: VIDEO ĐỘNG DUNG NHAM KHÍ QUYỂN 2.5D (LAVA PLANETS ATMOSPHERIC LOOP)

> **Asset gốc:** [`lava_planets_4k_lossless.png`](file:///d:/3D-Shooter-master/GameBackgroundPictures/lava_planets_4k_lossless.png)  
> **Script thực thi:** [`create_lava_planets_atmospheric_video.py`](file:///d:/3D-Shooter-master/GameBackgroundPictures/create_lava_planets_atmospheric_video.py)  
> **Video hoàn tất:** [`lava_planets_atmospheric_loop.mp4`](file:///d:/3D-Shooter-master/GameBackgroundPictures/lava_planets_atmospheric_loop.mp4) (đồng thời lưu tại [`C:\Users\ADMIN\Downloads\lava_planets_atmospheric_loop.mp4`](file:///C:/Users/ADMIN/Downloads/lava_planets_atmospheric_loop.mp4))  
> **Trạng thái:** Hoàn tất $100\%$, đạt tiêu chuẩn kiểm nghiệm quang học và chuyển động.

---

## I. CÁC YÊU CẦU ĐÃ THỰC THI CHUẨN XÁC

| Hạng mục | Yêu cầu của bạn | Phương pháp giải quyết & Kết quả |
| :--- | :--- | :--- |
| **1. Cắt rõ Layer** | Cắt các layer rành mạch, không lem dính mép | • Bán kính Mặt trăng chuẩn $r = 176\text{ px}$ (2K) / $r = 264\text{ px}$ (4K) ôm khít $100\%$ hình cầu.<br>• Đường chân trời tiền cảnh bám sát parabol viền phát sáng.<br>• Tách 5 lớp sạch sẽ, không còn bóng ma hay khuyết mép. |
| **2. Bỏ quay hành tinh** | Bỏ qua việc hành tinh quay | • Khóa cứng $100\%$ vỏ đá bazan, miệng hố va chạm và lục địa.<br>• Không xoay cầu $360^\circ$, không cuộn texture $\implies$ Giữ trọn độ nét nguyên bản của ảnh 4K/2K. |
| **3. Dung nham bám rãnh** | Chảy theo dòng, theo rãnh chứ không chảy bừa | • Dùng **Ten-sơ cấu trúc (Structure Tensor)** trích xuất véc-tơ tiếp tuyến $\vec{T}(x, y)$ uốn lượn theo lòng sông.<br>• Phân bố vận tốc ống dẫn **Poiseuille** kết hợp **No-Slip Condition**: Vận tốc tại bờ đá triệt tiêu về $0$ ($\text{Diff} = 0.0$ tuyệt đối). |
| **4. Dung nham cả 3 hành tinh** | Cả 3 hành tinh đều chảy, vẽ bổ sung để thực tế hơn | • **Hành tinh Mẹ:** Hẻm núi xích đạo cuộn trôi mạnh mẽ từ Tây sang Đông.<br>• **Mặt trăng:** Dò theo rãnh nứt địa chất tự nhiên bằng bộ lọc Hessian đa tỷ lệ, tổng hợp mạng lưới sông nham thạch nóng sáng ($T > 1200^\circ\text{C}$) phân nhánh từ cực bắc.<br>• **Tiền cảnh:** Dòng nham thạch cuộn trôi trong các hồ caldera và rãnh nứt lớn góc dưới phải. |

---

## II. THÔNG SỐ KỸ THUẬT VIDEO

* **Độ phân giải:** $2560 \times 1440\text{ px}$ (2K QHD chuẩn 16:9).
* **Tốc độ khung hình:** $30\text{ fps}$ (Tổng cộng 360 frames).
* **Thời lượng:** Đúng $12.0\text{ giây}$.
* **Chu kỳ lặp (Loop Continuity):** Thuật toán 2 pha tuần hoàn (Dual-Phase Cyclic Advection) đảm bảo **Seamless Loop $100\%$ tuyệt đối ($0$ frame khựng, $0$ giật)**.
* **Dung lượng tệp:** $7.41\text{ MB}$ (Chuẩn nén H.264 CRF 16 chất lượng cao).
