# BỘ QUY TẮC PHÁT TRIỂN DỰ ÁN (MANDATORY PROJECT RULES)
> **HIỆU LỰC TỐI CAO:** Bộ quy tắc này là hiến pháp phát triển của dự án. AI trợ lý bắt buộc phải tuân thủ 100% trong mọi tình huống, không có ngoại lệ.

---

## ĐIỀU 1: QUY TRÌNH PHÊ DUYỆT VÀ QUẢN LÝ THAY ĐỔI (WORKFLOW & APPROVAL PROTOCOL)
1. **KHÔNG TỰ Ý SỬA CODE KHI CHƯA ĐƯỢC DUYỆT:**
   - Khi người dùng đưa ra câu hỏi, yêu cầu kiểm tra, chẩn đoán lỗi ("check lại", "tại sao", "giải thích", "cho tôi biết", "xem lại"): AI **CHỈ ĐƯỢC PHÉP** đọc file, phân tích nguyên nhân gốc rễ (Root Cause Analysis), mô phỏng toán học và trình bày giải pháp.
   - **TUYỆT ĐỐI CẤM** tự tiện chỉnh sửa file mã nguồn, tạo commit hoặc thay đổi asset khi người dùng chưa ra lệnh trực tiếp: *"thực hiện"*, *"triển khai"*, *"tiến hành"*, *"đồng ý"*.
2. **QUY TRÌNH 4 BƯỚC BẮT BUỘC CHO MỌI THAY ĐỔI:**
   - **Bước 1 (Khảo sát):** Đọc code thực tế, chạy script mô phỏng, tìm chính xác nguyên nhân gốc rễ, không phỏng đoán.
   - **Bước 2 (Lập kế hoạch):** Trình bày phương án chi tiết kèm phân tích ưu/nhược, tác động tới kiến trúc, và kế hoạch nghiệm thu.
   - **Bước 3 (Chờ phê duyệt):** Chờ người dùng xác nhận và lựa chọn phương án.
   - **Bước 4 (Thực thi & Nghiệm thu):** Thực hiện đúng phạm vi được duyệt, biên dịch kiểm tra `0 Errors`, cập nhật tài liệu walkthrough.
3. **MỆNH LỆNH HOÀN NGUYÊN (UNDO) LẬP TỨC:**
   - Khi người dùng ra lệnh "undo", AI phải lập tức hoàn nguyên mã nguồn và asset về trạng thái trước đó một cách an toàn, chính xác và không tranh luận.

---

## ĐIỀU 2: NGUYÊN TẮC CHỐNG TỰ SUY DIỄN (ZERO GUESSWORK PRINCIPLE)
1. **CẤM TỰ Ý SÁNG TÁC THÔNG SỐ VÀ CƠ CHẾ:**
   - AI **KHÔNG ĐƯỢC TỰ Ý** quyết định: thuật toán mới, công thức, parameter, state transition, animation/input priority, timing, hoặc tự ý thêm các cơ chế ngoài spec (như tự thêm micro-aim alignment khi chưa có sự chấp thuận).
   - **KHÔNG ĐƯỢC PHÉP** thay thế bằng: *"ước lượng"*, *"cảm giác"*, *"giá trị hợp lý"*, *"industry standard"*, *"Unity default"*, *"Lerp cho mượt"*.
2. **NGUỒN CHÂN LÝ DUY NHẤT (SOURCE OF TRUTH):**
   - Mọi cơ chế bay, ngắm bắn, điều phối quái phải bám sát 100% tài liệu kỹ thuật của dự án:
     - `STARFOX64_AIMING_PLAN.md` (Hệ thống ngắm bắn 4 giai đoạn).
     - `STARFOX64_ENEMY_SPAWN_PLAN.md` (Kế hoạch spawn quái & thiên thạch).
     - Skill `starfox-64-mechanics` (Chuẩn N64 US Rev 1.1).

---

## ĐIỀU 3: QUY TẮC AN TOÀN 3 TẦNG BẢO LƯU (3-LAYER UNDO PROTOCOL)
Mọi can thiệp vào mã nguồn hoặc Prefab phải tuân thủ nghiêm ngặt 3 tầng an toàn:
1. **Tầng 1 (Repository Level):** Luôn tạo nhánh Git checkpoint (`backup/<feature-name>`) trước khi sửa đổi cấu trúc.
2. **Tầng 2 (Asset Level):** **KHÔNG BAO GIỜ** ghi đè trực tiếp Prefab gốc của dự án (`Player Ship (1).prefab`). Phải tạo Prefab độc lập mới (`VirtualRail_PlayerRig.prefab`) để Prefab gốc luôn nguyên vẹn 100%.
3. **Tầng 3 (Editor Tool Level):** Bắt buộc cung cấp cặp MenuItem 1-click đối xứng trong `Tools > Virtual Rail > ...`:
   - `[X] Action (ON)`: Kích hoạt tính năng mới.
   - `[X+1] Undo Action (OFF - Legacy)`: Hoàn nguyên 100% về cơ chế cũ ngay trong Unity Editor mà không cần sửa code.

---

## ĐIỀU 4: TIÊU CHUẨN KIẾN TRÚC & HIỆU NĂNG UNITY (PERFORMANCE & ARCHITECTURE)
1. **0 GC ALLOCATION TRONG GAME LOOP:**
   - **CẤM** gọi `new`, `Instantiate`, `Destroy`, LINQ, Boxing/Unboxing, ghép chuỗi string trong các hàm vòng lặp (`Update`, `LateUpdate`, `FixedUpdate`).
   - Sử dụng Struct, mảng tĩnh / cache component trong `Awake()`/`Start()`, và Object Pool cho toàn bộ đạn, vfx, quái vật, thiên thạch.
2. **KIẾN TRÚC LUỒNG DỮ LIỆU 1 CHIỀU (UNIDIRECTIONAL DATA FLOW):**
   ```text
   Input Reader -> Gameplay State -> Flight & Weapon Systems -> Presentation (Visual/Camera/UI)
   ```
   - Presentation (mesh đồ họa, Camera, UI) chỉ đọc dữ liệu từ Gameplay State để hiển thị, **TUYỆT ĐỐI KHÔNG** can thiệp ngược lại Gameplay Logic.
   - Không sử dụng Unity `Rigidbody` physics (`AddForce`, `Torque`, `velocity`) để mô phỏng bay Arwing. Trạng thái bay là máy trạng thái tất định (Deterministic State Machine).
3. **ĐỒNG BỘ CAMERA & TỌA ĐỘ CHIẾU UI (LATEUPDATE SYNC):**
   - Mọi tính toán tọa độ màn hình cho Crosshair, Marker, Reticle dựa trên góc nhìn Camera phải được thực thi trong `LateUpdate()` sau khi CameraRig đã định vị xong trong frame để triệt tiêu hoàn toàn độ trễ 1 frame (1-frame jitter).

---

## ĐIỀU 5: NGHIỆM THU BIÊN DỊCH & BÁO CÁO (VERIFICATION & REPORTING)
1. **BIÊN DỊCH BẮT BUỘC 0 ERRORS:**
   - Trước khi báo cáo hoàn tất bất kỳ task lập trình nào, AI phải tự chạy lệnh MSBuild trên terminal PowerShell:
     - `Assembly-CSharp.csproj`
     - `Assembly-CSharp-Editor.csproj`
   - Cả 2 project phải đạt **0 Errors** (Exit code 0).
2. **ĐỊNH DẠNG PHẢN HỒI:**
   - Sử dụng tiếng Việt rõ ràng, súc tích, mạch lạc.
   - Trình bày dạng GitHub-style markdown.
   - Mọi file và symbol được nhắc đến phải có link markdown dạng `[tên_file](file:///đường_dẫn_tuyệt_đối)`.
