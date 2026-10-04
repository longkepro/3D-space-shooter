import os
import sys
import subprocess
import cv2
import numpy as np
import imageio_ffmpeg

if sys.stdout:
    sys.stdout.reconfigure(encoding='utf-8')
if sys.stderr:
    sys.stderr.reconfigure(encoding='utf-8')

def render_stage3_citadel_animated_demo():
    src_path = r'D:\3D-Shooter-master\Assets\Textures\Skybox\stage3.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_mp4 = os.path.join(out_dir, 'stage3_citadel_animated.mp4')
    brain_mp4 = r'C:\Users\ADMIN\.gemini\antigravity\brain\bdaefadd-efec-4e27-a837-c761193a9812\stage3_citadel_animated.mp4'

    print("===========================================================", flush=True)
    print("XUẤT VIDEO STAGE 3: SIÊU PHÁO ĐÀI QUỸ ĐẠO ALIEN CITADEL (SECTOR 3 - CLIMAX)", flush=True)
    print(f"Nguồn ảnh gốc: {src_path}", flush=True)
    print("===========================================================", flush=True)

    img = cv2.imread(src_path)
    if img is None:
        raise FileNotFoundError(f"Không thể mở file: {src_path}")

    target_w, target_h = 1920, 1080
    img_1080 = cv2.resize(img, (target_w, target_h), interpolation=cv2.INTER_LANCZOS4)
    img_f = img_1080.astype(np.float32) / 255.0

    b_base = img_f[:, :, 0]
    g_base = img_f[:, :, 1]
    r_base = img_f[:, :, 2]
    gray = cv2.cvtColor(img_1080, cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0

    fps = 30
    duration = 6.0  # seconds (Seamless Loop 100%)
    total_frames = int(fps * duration)
    omega = 2.0 * np.pi / duration

    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)

    # 1. Mặt nạ Ngôi sao mẹ (Host Radiant Star tại x=392, y=235)
    star_cx, star_cy = 392.0, 235.0
    dist_star = np.hypot(x_grid - star_cx, y_grid - star_cy)
    star_halo = np.exp(-dist_star / 110.0)
    star_core = np.clip((gray - 0.45) / 0.55, 0.0, 1.0) * (dist_star < 320.0)
    star_mask = cv2.GaussianBlur(np.maximum(star_halo * 0.70, star_core), (21, 21), 6.0)

    # 2. Mặt nạ Mạch năng lượng Plasma & Lõi pháo đài (Citadel Plasma Rails & Power Core)
    # Vùng pháo đài (Citadel structural zone trên mạn phải và đáy)
    poly_pts = np.array([
        [1500, 0], [1920, 0], [1920, 1080], [520, 1080],
        [520, 840], [740, 680], [820, 500], [1050, 420],
        [1350, 200], [1480, 0]
    ], dtype=np.int32)
    citadel_hull_mask = np.zeros((target_h, target_w), dtype=np.uint8)
    cv2.fillPoly(citadel_hull_mask, [poly_pts], 1)

    # Lọc các dải mạch plasma xanh ngọc sáng rực trên thân pháo đài
    plasma_raw = (citadel_hull_mask > 0) & (g_base > 0.40) & (g_base > r_base + 0.14)
    plasma_mask = cv2.GaussianBlur(plasma_raw.astype(np.float32), (7, 7), 2.0)

    # 3. Mặt nạ Lõi năng lượng cực đại (Core-01 Sanctum Reactor tại x=1691, y=397)
    dist_core = np.hypot(x_grid - 1691.0, y_grid - 397.0)
    core_glow = np.exp(-(dist_core ** 2) / (2.0 * (45.0 ** 2))) * (g_base > 0.5)
    core_mask = cv2.GaussianBlur(core_glow.astype(np.float32), (15, 15), 4.0)

    # 4. Mặt nạ Đèn tín hiệu cảnh báo hàng hải (Beacon Warning Strobes)
    beacon_raw = (r_base > 0.48) & (b_base < 0.35) & (r_base > g_base + 0.10)
    beacon_mask = cv2.GaussianBlur(beacon_raw.astype(np.float32), (3, 3), 0.8)

    # 5. Mặt nạ Động cơ tiêm kích hộ vệ (Fleet Afterburners tại x=872, y=1012)
    dist_fleet = np.hypot(x_grid - 872.0, y_grid - 1012.0)
    fleet_raw = (dist_fleet < 120.0) & (r_base > 0.38) & (g_base > 0.28) & (b_base < 0.32)
    fleet_mask = cv2.GaussianBlur(fleet_raw.astype(np.float32), (3, 3), 0.8)

    # 6. Mặt nạ Màn sương Tinh vân ngọc lục bảo (Emerald Nebula Glow)
    nebula_raw = (citadel_hull_mask == 0) & (dist_star > 110.0) & (g_base > 0.22) & (g_base > r_base + 0.05) & (gray < 0.70)
    nebula_mask = cv2.GaussianBlur(nebula_raw.astype(np.float32), (41, 41), 15.0)

    # 7. Sao vũ trụ sâu: Phân lập điểm sao độc lập (Area <= 45 px)
    space_zone = (dist_star > 160.0) & (~plasma_raw) & (citadel_hull_mask == 0) & (gray > 0.42)
    num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(space_zone.astype(np.uint8))
    valid_star_mask = np.zeros_like(space_zone, dtype=bool)
    star_phase = np.zeros((target_h, target_w), dtype=np.float32)
    np.random.seed(42)
    phases = np.random.uniform(0, 2.0 * np.pi, size=num_labels)
    for i in range(1, num_labels):
        if stats[i, cv2.CC_STAT_AREA] <= 45:
            m_i = (labels == i)
            valid_star_mask[m_i] = True
            star_phase[m_i] = phases[i]
    stars_mask = cv2.GaussianBlur(valid_star_mask.astype(np.float32), (3, 3), 0.8)

    ffmpeg_exe = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [
        ffmpeg_exe,
        '-y',
        '-f', 'rawvideo',
        '-vcodec', 'rawvideo',
        '-s', f'{target_w}x{target_h}',
        '-pix_fmt', 'bgr24',
        '-r', str(fps),
        '-i', '-',
        '-c:v', 'libx264',
        '-pix_fmt', 'yuv420p',
        '-preset', 'fast',
        '-crf', '17',
        out_mp4
    ]

    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    print(f"Bắt đầu render {total_frames} frames ({duration}s @ {fps}fps)...", flush=True)

    for frame_idx in range(total_frames):
        t = frame_idx / fps
        theta = omega * t

        # A. KHỐI NỀN TĨNH GỐC 100% (Solid Citadel Hull & Celestial Bodies - Giữ nguyên kết cấu vững chãi)
        r_out = r_base.copy()
        g_out = g_base.copy()
        b_out = b_base.copy()

        # B. NGÔI SAO MẸ: NHỊP PHÁT SÁNG TỰ NHIÊN (Star Breathing - Bội số nguyên k = 1.0)
        star_pulse = np.sin(theta * 1.0) * 0.10
        r_out += r_base * star_mask * star_pulse
        g_out += g_base * star_mask * star_pulse * 1.02
        b_out += b_base * star_mask * star_pulse * 1.05

        # C. MẠCH NĂNG LƯỢNG PLASMA PHÁO ĐÀI (Plasma Conduits Breathing - Bội số nguyên k = 1.0)
        plasma_pulse = np.sin(theta * 1.0) * 0.12
        g_out += plasma_mask * plasma_pulse * 0.28
        b_out += plasma_mask * plasma_pulse * 0.22
        r_out += plasma_mask * plasma_pulse * 0.08

        # D. LÕI NĂNG LƯỢNG TRUNG TÂM CORE-01 (Core Quantum Resonance - Đồng pha k = 1.0)
        core_pulse = np.sin(theta * 1.0) * 0.15
        g_out += core_mask * core_pulse * 0.35
        b_out += core_mask * core_pulse * 0.25
        r_out += core_mask * core_pulse * 0.10

        # E. MÀN SƯƠNG TINH VÂN NGỌC LỤC BẢO (Nebula Ambient Shimmer - Bội số nguyên k = 1.0)
        nebula_pulse = np.sin(theta * 1.0) * 0.06
        g_out += nebula_mask * nebula_pulse * 0.15
        b_out += nebula_mask * nebula_pulse * 0.12
        r_out += nebula_mask * nebula_pulse * 0.04

        # F. HỆ THỐNG ĐÈN TÍN HIỆU CẢNH BÁO (Beacon Warning Strobes - Bội số nguyên k = 2.0, chu kỳ 3.0s)
        # Hàm chớp xung vi mô mượt mà: (sin^6)
        beacon_strobe = (np.sin(theta * 2.0) ** 6) * 0.35
        r_out += beacon_mask * beacon_strobe * 0.40
        g_out += beacon_mask * beacon_strobe * 0.20

        # G. ĐỘNG CƠ PHI THUYỀN TIÊM KÍCH HỘ VỆ (Fleet Afterburners Shimmer - Bội số nguyên k = 2.0)
        fleet_pulse = (np.sin(theta * 2.0) ** 4) * 0.25
        r_out += fleet_mask * fleet_pulse * 0.30
        g_out += fleet_mask * fleet_pulse * 0.18

        # H. SAO VŨ TRỤ SÂU: LẤP LÁNH TỰ NHIÊN ĐỘC LẬP TỪNG NGÔI SAO (Bội số nguyên k = 3.0)
        star_twinkle = np.sin(theta * 3.0 + star_phase) * 0.20
        r_out += stars_mask * star_twinkle * 0.25
        g_out += stars_mask * star_twinkle * 0.25
        b_out += stars_mask * star_twinkle * 0.30

        # Clip chuẩn [0, 1]
        frame_rgb = np.clip(np.dstack([b_out, g_out, r_out]), 0.0, 1.0)
        frame_u8 = (frame_rgb * 255.0).astype(np.uint8)

        proc.stdin.write(frame_u8.tobytes())

        if frame_idx % 30 == 0:
            print(f"Rendered {frame_idx}/{total_frames} frames ({(frame_idx/total_frames)*100:.1f}%)...", flush=True)

    proc.stdin.close()
    proc.wait()

    if os.path.exists(out_mp4):
        import shutil
        shutil.copyfile(out_mp4, brain_mp4)
        print(f"HOÀN TẤT XUẤT VIDEO STAGE 3 THÀNH CÔNG: {out_mp4}", flush=True)

if __name__ == '__main__':
    render_stage3_citadel_animated_demo()
