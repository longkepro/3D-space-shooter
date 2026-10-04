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

def render_stage1_simple_animated_demo():
    src_path = r'D:\3D-Shooter-master\Assets\Textures\Skybox\stage1.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_mp4 = os.path.join(out_dir, 'stage1_cosmic_animated.mp4')
    brain_mp4 = r'C:\Users\ADMIN\.gemini\antigravity\brain\bdaefadd-efec-4e27-a837-c761193a9812\stage1_cosmic_animated.mp4'

    print("===========================================================", flush=True)
    print("XUẤT VIDEO STAGE 1: KHÔNG GIAN SÂU & TRÁI ĐẤT (TỐI GIẢN, TRANG NHÃ)", flush=True)
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

    # 1. Trích xuất mặt nạ Ngôi sao mẹ / Mặt trời (Star/Sun mask)
    star_cx, star_cy = 1539.0, 298.0
    dist_star = np.hypot(x_grid - star_cx, y_grid - star_cy)
    star_halo = np.exp(-dist_star / 90.0)
    star_core = np.clip((gray - 0.45) / 0.55, 0.0, 1.0) * (dist_star < 400.0)
    star_mask = cv2.GaussianBlur(np.maximum(star_halo * 0.7, star_core), (15, 15), 4.0)

    # 2. Vành khí quyển chân trời Trái Đất (Earth Atmosphere Rayleigh Glow)
    # Tọa độ parabol chuẩn xác cho chân trời Trái Đất (1080p)
    a_pix = 0.00022419118
    b_pix = -0.41610607
    c_pix = 857.98947
    earth_y = a_pix * (x_grid ** 2) + b_pix * x_grid + c_pix
    dist_earth_horizon = np.abs(y_grid - earth_y)
    atmo_mask = np.exp(- (dist_earth_horizon ** 2) / (2.0 * (14.0 ** 2))) * (b_base > 0.15)

    # 3. Dải mây tầng đối lưu (Cloud mask)
    cloud_raw = np.clip((gray - 0.28) / 0.5, 0.0, 1.0) * (y_grid > earth_y - 10)
    cloud_mask = cv2.GaussianBlur(cloud_raw, (9, 9), 2.5)

    # 4. Viền sáng Vệ tinh băng (Midground Icy Moon Rim)
    scale = 1920.0 / 2560.0
    mcx, mcy, mr = 352.3 * scale, 1317.3 * scale, 916.7 * scale
    dist_m = np.hypot(x_grid - mcx, y_grid - mcy)
    dir_x = (star_cx - mcx) / np.hypot(star_cx - mcx, star_cy - mcy)
    dir_y = (star_cy - mcy) / np.hypot(star_cx - mcx, star_cy - mcy)
    norm_mx = (x_grid - mcx) / (dist_m + 1e-5)
    norm_my = (y_grid - mcy) / (dist_m + 1e-5)
    cos_facing = np.clip(norm_mx * dir_x + norm_my * dir_y, 0.0, 1.0)
    moon_rim = np.exp(- ((dist_m - mr) ** 2) / (2.0 * (14.0 ** 2))) * (cos_facing ** 2.5) * (y_grid < earth_y)

    # 5. Sao vũ trụ sâu: Tách biệt hoàn toàn khỏi Trái Đất (Deep Space Stars)
    # Không để lọt bất kỳ pixel khí quyển Trái Đất nào vào mask sao
    space_mask = (y_grid < earth_y - 25.0) & (dist_m > mr + 15.0)
    stars_raw = (gray > 0.45) & (dist_star > 180) & space_mask

    # Phân lập từng ngôi sao điểm (Connected Components Area <= 45 px)
    num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats((stars_raw > 0).astype(np.uint8))
    valid_star_mask = np.zeros_like(stars_raw, dtype=bool)
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

        # A. KHỐI NỀN TĨNH GỐC 100% (Solid celestial bodies - Không méo, không vẽ thêm vật thể lạ)
        r_out = r_base.copy()
        g_out = g_base.copy()
        b_out = b_base.copy()

        # B. NGÔI SAO: NHỊP PHÁT SÁNG TỰ NHIÊN (Luminance Breathing - Bội số nguyên k = 1.0)
        # Chỉ tăng giảm độ sáng vi tế của chính ngôi sao và dải flare sẵn có
        star_pulse = np.sin(theta * 1.0) * 0.12
        r_out += r_base * star_mask * star_pulse
        g_out += g_base * star_mask * star_pulse * 0.95
        b_out += b_base * star_mask * star_pulse * 1.05

        # C. KHÍ QUYỂN TRÁI ĐẤT: VÀNH TÁN XẠ BẬP BÙNG NHẸ (Rayleigh Shimmer - Bội số nguyên k = 2.0 thuần biên độ)
        atmo_pulse = np.sin(theta * 2.0) * 0.08
        b_out += atmo_mask * atmo_pulse * 0.25
        g_out += atmo_mask * atmo_pulse * 0.15
        r_out += atmo_mask * atmo_pulse * 0.08

        # D. DẢI MÂY KHÍ QUYỂN: NHỊP PHẢN QUANG DỊU NHẸ (Cloud Breathing - Bội số nguyên k = 1.0 thuần biên độ)
        cloud_pulse = np.sin(theta * 1.0) * 0.06
        r_out += cloud_mask * cloud_pulse * 0.18
        g_out += cloud_mask * cloud_pulse * 0.20
        b_out += cloud_mask * cloud_pulse * 0.22

        # E. VIỀN SÁNG VỆ TINH BĂNG (Moon Rim Breathing - Đồng pha với Ngôi sao k = 1.0)
        moon_pulse = np.sin(theta * 1.0) * 0.10
        r_out += moon_rim * moon_pulse * 0.20
        g_out += moon_rim * moon_pulse * 0.22
        b_out += moon_rim * moon_pulse * 0.25

        # F. SAO VŨ TRỤ SÂU: LẤP LÁNH TỰ NHIÊN ĐỘC LẬP TỪNG NGÔI SAO (Bội số nguyên k = 3.0)
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
        print(f"HOÀN TẤT XUẤT VIDEO STAGE 1 THÀNH CÔNG: {out_mp4}", flush=True)

if __name__ == '__main__':
    render_stage1_simple_animated_demo()
