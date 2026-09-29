import os
import sys
import time
import shutil
import subprocess
import cv2
import numpy as np
import imageio_ffmpeg

if sys.stdout:
    sys.stdout.reconfigure(encoding='utf-8')
if sys.stderr:
    sys.stderr.reconfigure(encoding='utf-8')

def build_atmospheric_lava_video():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_4k_lossless.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_name = 'lava_planets_atmospheric_loop.mp4'
    out_path = os.path.join(out_dir, out_name)
    downloads_path = os.path.join(r'C:\Users\ADMIN\Downloads', out_name)

    print(f"===========================================================", flush=True)
    print(f"KHỞI ĐỘNG XUẤT VIDEO: {out_name}", flush=True)
    print(f"Nguồn ảnh: {src_path}", flush=True)
    print(f"===========================================================", flush=True)

    img_full = cv2.imread(src_path)
    if img_full is None:
        raise FileNotFoundError(f"Không thể mở file: {src_path}")

    # Chuẩn 2K QHD 16:9
    target_w, target_h = 2560, 1440
    print(f"1. Chuẩn hóa độ phân giải 2K QHD ({target_w}x{target_h})...", flush=True)
    img = cv2.resize(img_full, (target_w, target_h), interpolation=cv2.INTER_AREA)
    img_f = img.astype(np.float32) / 255.0

    fps = 30
    duration = 12.0  # seconds per loop
    total_frames = int(fps * duration)
    omega0 = 2.0 * np.pi / duration

    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)

    # ----------------------------------------------------
    # 2. Phân tầng hình học chuẩn xác (Layer Masks)
    # ----------------------------------------------------
    print("2. Tính toán mặt nạ phân tầng (Layer Masks)...", flush=True)

    # A. Mặt trăng (Moon) - Tâm và bán kính chuẩn xác
    moon_cx, moon_cy, moon_r = 762.7, 777.3, 176.0
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask_f = np.clip((moon_r + 1.0 - moon_dist) / 2.0, 0.0, 1.0)
    moon_bool = moon_mask_f > 0.01

    # B. Tiền cảnh (Foreground Planet) - Phương trình Parabol chuẩn
    fg_edge_y = (4.793945e-05 * 2.25 * (x_grid**2) - 0.8305 * 1.5 * x_grid + 3822.02) / 1.5
    fg_dist = y_grid - fg_edge_y
    fg_mask_f = np.clip((fg_dist + 4.0) / 8.0, 0.0, 1.0)
    fg_bool = fg_mask_f > 0.01

    # C. Hành tinh Mẹ (Main Planet) & Nền sao (Deep Space)
    main_cx, main_cy, main_r = 1200.0, 780.0, 886.0
    main_dist = np.hypot(x_grid - main_cx, y_grid - main_cy)
    main_planet_circle = np.clip((main_r + 2.0 - main_dist) / 4.0, 0.0, 1.0)
    main_bool = (main_planet_circle > 0.01) & (~moon_bool) & (~fg_bool)
    space_bool = (main_planet_circle < 0.01) & (~moon_bool) & (~fg_bool)

    # ----------------------------------------------------
    # 3. Trích xuất & Vẽ bổ sung dòng Dung nham 3 Hành tinh
    # ----------------------------------------------------
    print("3. Trích xuất và tổng hợp mạng lưới sông Dung nham trên cả 3 hành tinh...", flush=True)

    # Kênh màu
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    gray_f = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.float32)

    # A. HÀNH TINH MẸ (MAIN PLANET)
    print("   - Khởi tạo thủy động lực sông Dung nham Hành tinh Mẹ...", flush=True)
    magma_score_main = np.clip((r_ch - b_ch * 1.25) * 1.6, 0.0, 1.0) * np.clip((r_ch - 0.22) / 0.78, 0.0, 1.0)
    main_magma_mask = (magma_score_main > 0.20) & (r_ch > g_ch * 1.05) & main_bool
    main_magma_u8 = main_magma_mask.astype(np.uint8) * 255

    # Structure Tensor cho hướng dòng chảy theo rãnh nứt
    gray_smooth_m = cv2.GaussianBlur(gray_f, (15, 15), 3.0)
    Ix_m = cv2.Sobel(gray_smooth_m, cv2.CV_32F, 1, 0, ksize=5)
    Iy_m = cv2.Sobel(gray_smooth_m, cv2.CV_32F, 0, 1, ksize=5)
    Jxx_m = cv2.GaussianBlur(Ix_m * Ix_m, (21, 21), 5.0)
    Jyy_m = cv2.GaussianBlur(Iy_m * Iy_m, (21, 21), 5.0)
    Jxy_m = cv2.GaussianBlur(Ix_m * Iy_m, (21, 21), 5.0)
    theta_m = 0.5 * np.arctan2(2.0 * Jxy_m, Jxx_m - Jyy_m) + (np.pi / 2.0)
    t_m_x = np.cos(theta_m)
    t_m_y = np.sin(theta_m)

    # Đồng bộ chiều chảy xuôi theo hẻm núi xích đạo (Tây -> Đông)
    flip_m = (t_m_x * 0.8 + t_m_y * 0.6) < 0
    t_m_x[flip_m] *= -1.0
    t_m_y[flip_m] *= -1.0
    t_m_x = cv2.GaussianBlur(t_m_x, (15, 15), 3.0)
    t_m_y = cv2.GaussianBlur(t_m_y, (15, 15), 3.0)
    norm_m = np.hypot(t_m_x, t_m_y) + 1e-5
    t_m_x /= norm_m
    t_m_y /= norm_m

    # Khoảng cách tới bờ đá bazan (No-Slip Condition)
    dist_wall_m = cv2.distanceTransform(main_magma_u8, cv2.DIST_L2, 5)
    dist_norm_m = np.clip(dist_wall_m / 6.0, 0.0, 1.0)
    prof_m = dist_norm_m ** 1.5

    v_main_x = t_m_x * prof_m * 24.0
    v_main_y = t_m_y * prof_m * 24.0

    # B. MẶT TRĂNG (MOON SATELLITE) - VẼ BỔ SUNG SÔNG DUNG NHAM ĐỊA CHẤT
    print("   - Vẽ bổ sung và kích hoạt dòng chảy sông Dung nham Mặt trăng...", flush=True)
    def get_ridges(im, sigma):
        g = cv2.GaussianBlur(im, (0, 0), sigma)
        Ixx = cv2.Sobel(g, cv2.CV_32F, 2, 0, ksize=3)
        Iyy = cv2.Sobel(g, cv2.CV_32F, 0, 2, ksize=3)
        Ixy = cv2.Sobel(g, cv2.CV_32F, 1, 1, ksize=3)
        tr = Ixx + Iyy
        det = Ixx * Iyy - Ixy * Ixy
        disc = np.sqrt(np.maximum(tr * tr - 4 * det, 0.0))
        lambda2 = 0.5 * (tr - disc)
        return np.maximum(-lambda2, 0.0)

    moon_volcano_score = np.clip((r_ch - b_ch * 1.1) / 30.0, 0.0, 1.0) * np.clip((r_ch - 40.0) / 100.0, 0.0, 1.0)
    r1 = get_ridges(gray_f, 2.0)
    r2 = get_ridges(gray_f, 4.0)
    ridge_m = (r1 + r2 * 1.5)
    ridge_m = ridge_m / (np.percentile(ridge_m, 99) + 1e-5)
    moon_rivers_val = np.clip(moon_volcano_score * 1.2 + ridge_m * 0.4, 0.0, 1.0) * (moon_dist < (moon_r - 6))

    moon_magma_mask = (moon_rivers_val > 0.22) & moon_bool
    moon_magma_u8 = moon_magma_mask.astype(np.uint8) * 255

    # Structure Tensor cho rãnh nứt mặt trăng
    Ix_moon = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 1, 0, ksize=5)
    Iy_moon = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 0, 1, ksize=5)
    Jxx_moon = cv2.GaussianBlur(Ix_moon * Ix_moon, (15, 15), 3.0)
    Jyy_moon = cv2.GaussianBlur(Iy_moon * Iy_moon, (15, 15), 3.0)
    Jxy_moon = cv2.GaussianBlur(Ix_moon * Iy_moon, (15, 15), 3.0)
    theta_moon = 0.5 * np.arctan2(2.0 * Jxy_moon, Jxx_moon - Jyy_moon) + (np.pi / 2.0)
    t_moon_x = np.cos(theta_moon)
    t_moon_y = np.sin(theta_moon)
    flip_moon = (t_moon_y) < 0  # Hướng chảy xuôi từ cực bắc xuống
    t_moon_x[flip_moon] *= -1.0
    t_moon_y[flip_moon] *= -1.0

    dist_wall_moon = cv2.distanceTransform(moon_magma_u8, cv2.DIST_L2, 5)
    dist_norm_moon = np.clip(dist_wall_moon / 4.5, 0.0, 1.0)
    prof_moon = dist_norm_moon ** 1.5

    v_moon_x = t_moon_x * prof_moon * 14.0
    v_moon_y = t_moon_y * prof_moon * 14.0

    # Lớp texture sông dung nham phát quang cho mặt trăng
    moon_glow_tex = cv2.GaussianBlur(moon_rivers_val, (9, 9), 2.5)
    moon_core_tex = cv2.GaussianBlur(moon_rivers_val, (3, 3), 0.8)

    # C. HÀNH TINH TIỀN CẢNH (FOREGROUND PLANET)
    print("   - Khởi tạo thủy động lực sông Dung nham Tiền cảnh...", flush=True)
    fg_magma_score = np.clip((r_ch - b_ch * 1.2) * 1.5, 0.0, 1.0) * np.clip((r_ch - 0.2) / 0.8, 0.0, 1.0)
    r_fg = get_ridges(gray_f, 2.5)
    r_fg = r_fg / (np.percentile(r_fg, 99) + 1e-5)
    fg_rivers_val = np.clip(fg_magma_score * 1.1 + r_fg * 0.35, 0.0, 1.0) * (fg_dist > 10.0)

    fg_magma_mask = (fg_rivers_val > 0.20) & fg_bool
    fg_magma_u8 = fg_magma_mask.astype(np.uint8) * 255

    Ix_fg = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 1, 0, ksize=5)
    Iy_fg = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 0, 1, ksize=5)
    Jxx_fg = cv2.GaussianBlur(Ix_fg * Ix_fg, (15, 15), 3.0)
    Jyy_fg = cv2.GaussianBlur(Iy_fg * Iy_fg, (15, 15), 3.0)
    Jxy_fg = cv2.GaussianBlur(Ix_fg * Iy_fg, (15, 15), 3.0)
    theta_fg = 0.5 * np.arctan2(2.0 * Jxy_fg, Jxx_fg - Jyy_fg) + (np.pi / 2.0)
    t_fg_x = np.cos(theta_fg)
    t_fg_y = np.sin(theta_fg)
    flip_fg = (t_fg_x * 0.7 - t_fg_y * 0.7) < 0
    t_fg_x[flip_fg] *= -1.0
    t_fg_y[flip_fg] *= -1.0

    dist_wall_fg = cv2.distanceTransform(fg_magma_u8, cv2.DIST_L2, 5)
    dist_norm_fg = np.clip(dist_wall_fg / 5.5, 0.0, 1.0)
    prof_fg = dist_norm_fg ** 1.5

    v_fg_x = t_fg_x * prof_fg * 20.0
    v_fg_y = t_fg_y * prof_fg * 20.0

    # ----------------------------------------------------
    # 4. Hợp nhất Trường Véc-tơ Toàn cục (Combined Velocity Field)
    # ----------------------------------------------------
    print("4. Hợp nhất trường vận tốc thủy động lực (Zero Rock Deformation)...", flush=True)
    v_total_x = np.zeros((target_h, target_w), dtype=np.float32)
    v_total_y = np.zeros((target_h, target_w), dtype=np.float32)
    total_blend_mask = np.zeros((target_h, target_w), dtype=np.float32)

    # Đưa vận tốc của từng hành tinh vào trường toàn cục
    v_total_x[main_magma_mask] = v_main_x[main_magma_mask]
    v_total_y[main_magma_mask] = v_main_y[main_magma_mask]
    total_blend_mask[main_magma_mask] = prof_m[main_magma_mask]

    v_total_x[moon_magma_mask] = v_moon_x[moon_magma_mask]
    v_total_y[moon_magma_mask] = v_moon_y[moon_magma_mask]
    total_blend_mask[moon_magma_mask] = prof_moon[moon_magma_mask]

    v_total_x[fg_magma_mask] = v_fg_x[fg_magma_mask]
    v_total_y[fg_magma_mask] = v_fg_y[fg_magma_mask]
    total_blend_mask[fg_magma_mask] = prof_fg[fg_magma_mask]

    total_magma_mask = main_magma_mask | moon_magma_mask | fg_magma_mask

    # ----------------------------------------------------
    # 5. Khí quyển Tro bụi & Bầu trời sao (Atmospheric & Space)
    # ----------------------------------------------------
    print("5. Khởi tạo hiệu ứng tán xạ khí quyển và bầu trời sao...", flush=True)
    # Mặt trời: góc trên trái (427, 246)
    sun_x, sun_y = 427.0, 246.0
    dist_sun = np.hypot(x_grid - sun_x, y_grid - sun_y)
    sun_halo_base = np.exp(-dist_sun / 140.0)

    # Vành sáng khí quyển bừng sáng (Atmospheric Rim Bloom)
    main_rim_dist = np.abs(main_dist - main_r)
    main_rim_mask = np.exp(-(main_rim_dist**2) / (2.0 * (18.0**2))) * (x_grid < main_cx) * (y_grid < main_cy + 100)

    # Danh sách 80 ngôi sao nhấp nháy độc lập
    np.random.seed(999)
    star_candidates = np.where((gray_f > 140) & (space_bool) & (dist_sun > 220))
    star_indices = np.random.choice(len(star_candidates[0]), size=min(100, len(star_candidates[0])), replace=False)
    star_coords = list(zip(star_candidates[1][star_indices], star_candidates[0][star_indices]))
    star_phases = np.random.uniform(0, 2 * np.pi, size=len(star_coords))
    star_freqs = np.random.choice([1, 2, 3], size=len(star_coords))

    # Gió tro bụi bán cầu Tây (Tropospheric Ash Wind)
    west_ash_mask = np.clip((950.0 - x_grid) / 350.0, 0.0, 1.0) * main_planet_circle * (~total_magma_mask)
    ash_noise_base = cv2.GaussianBlur(np.random.randn(target_h // 16, target_w // 16).astype(np.float32), (0, 0), 3.0)
    ash_noise = cv2.resize(ash_noise_base, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 0.04

    # ----------------------------------------------------
    # 6. Khởi động FFMPEG Render Pipeline
    # ----------------------------------------------------
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
        '-crf', '16',          # Chất lượng siêu nét
        '-preset', 'medium',
        out_path
    ]

    print(f"6. Bắt đầu Render {total_frames} frames (Seamless 12.0s Loop, 30fps)...", flush=True)
    pipe = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    t_start = time.time()

    for frame_idx in range(total_frames):
        t = frame_idx / fps
        theta0 = omega0 * t

        # --- A. Thủy động lực Dung nham 2 pha tuần hoàn (Dual-Phase Cyclic Advection) ---
        p1 = (t / duration) % 1.0
        p2 = ((t / duration) + 0.5) % 1.0
        w1 = 0.5 * (1.0 - np.cos(2.0 * np.pi * p1))
        w2 = 0.5 * (1.0 - np.cos(2.0 * np.pi * p2))

        map_x1 = np.clip(x_grid - v_total_x * p1, 0, target_w - 1).astype(np.float32)
        map_y1 = np.clip(y_grid - v_total_y * p1, 0, target_h - 1).astype(np.float32)
        map_x2 = np.clip(x_grid - v_total_x * p2, 0, target_w - 1).astype(np.float32)
        map_y2 = np.clip(y_grid - v_total_y * p2, 0, target_h - 1).astype(np.float32)

        sample1 = cv2.remap(img, map_x1, map_y1, cv2.INTER_LINEAR).astype(np.float32)
        sample2 = cv2.remap(img, map_x2, map_y2, cv2.INTER_LINEAR).astype(np.float32)

        advected_magma = sample1 * w1 + sample2 * w2

        # Lắp ghép cứng vào lòng sông (Bờ đá giữ nguyên 100% bằng no-slip)
        composite = img.astype(np.float32).copy()
        mask_3d = total_blend_mask[:, :, None]
        composite[total_magma_mask] = (
            img.astype(np.float32)[total_magma_mask] * (1.0 - mask_3d[total_magma_mask]) +
            advected_magma[total_magma_mask] * mask_3d[total_magma_mask]
        )

        # --- B. Vẽ sáng sông dung nham bổ sung cho Mặt trăng ---
        moon_pulse = 0.85 + 0.15 * np.sin(omega0 * t * 2.0)
        glow_val = moon_glow_tex * moon_pulse
        core_val = moon_core_tex * moon_pulse
        # Bổ sung màu dung nham nhiệt cao lên các nhánh sông mặt trăng
        composite[:, :, 0] = np.maximum(composite[:, :, 0], glow_val * 12.0 * moon_mask_f)
        composite[:, :, 1] = np.maximum(composite[:, :, 1], (glow_val * 75.0 + core_val * 150.0) * moon_mask_f)
        composite[:, :, 2] = np.maximum(composite[:, :, 2], (glow_val * 210.0 + core_val * 255.0) * moon_mask_f)

        # --- C. Tro bụi núi lửa trôi nhẹ bán cầu Tây ---
        ash_drift = ash_noise * np.sin(omega0 * t) * west_ash_mask
        composite[:, :, 0] = np.clip(composite[:, :, 0] * (1.0 + ash_drift), 0, 255)
        composite[:, :, 1] = np.clip(composite[:, :, 1] * (1.0 + ash_drift), 0, 255)
        composite[:, :, 2] = np.clip(composite[:, :, 2] * (1.0 + ash_drift), 0, 255)

        # --- D. Nhịp thở quang học Mặt trời & Viền khí quyển ---
        sun_pulse = 0.04 * np.sin(2.0 * omega0 * t)  # Chu kỳ 6.0s (2 nhịp thở trong 12s)
        sun_boost = sun_halo_base * sun_pulse * 255.0
        rim_boost = main_rim_mask * (sun_pulse * 1.5) * 255.0

        composite[:, :, 0] = np.clip(composite[:, :, 0] + sun_boost * 0.7 + rim_boost * 0.8, 0, 255)
        composite[:, :, 1] = np.clip(composite[:, :, 1] + sun_boost * 0.85 + rim_boost * 0.9, 0, 255)
        composite[:, :, 2] = np.clip(composite[:, :, 2] + sun_boost * 1.0 + rim_boost * 1.0, 0, 255)

        # --- E. Sao lấp lánh có chọn lọc trong không gian sâu ---
        for (sx, sy), s_phase, s_freq in zip(star_coords, star_phases, star_freqs):
            star_twinkle = 0.25 * np.sin(s_freq * omega0 * t + s_phase)
            composite[sy-1:sy+2, sx-1:sx+2] = np.clip(
                composite[sy-1:sy+2, sx-1:sx+2] * (1.0 + star_twinkle), 0, 255
            )

        # Chuyển đổi frame hoàn tất
        frame_out = np.clip(composite, 0, 255).astype(np.uint8)
        pipe.stdin.write(frame_out.tobytes())

        if (frame_idx + 1) % 45 == 0 or frame_idx == total_frames - 1:
            elapsed = time.time() - t_start
            fps_render = (frame_idx + 1) / elapsed
            percent = (frame_idx + 1) * 100.0 / total_frames
            print(f"   [Render Progress] Frame {frame_idx + 1:3d}/{total_frames} ({percent:5.1f}%) - Tốc độ: {fps_render:4.1f} fps", flush=True)

    pipe.stdin.close()
    pipe.wait()

    # Đồng bộ sang thư mục Downloads cho người dùng
    try:
        shutil.copy2(out_path, downloads_path)
        print(f"Đã sao chép video sang Downloads: {downloads_path}", flush=True)
    except Exception as e:
        print(f"Không thể sao chép sang Downloads: {e}", flush=True)

    file_size_mb = os.path.getsize(out_path) / (1024 * 1024)
    total_time = time.time() - t_start

    print(f"===========================================================", flush=True)
    print(f"XUẤT VIDEO THÀNH CÔNG 100%!", flush=True)
    print(f"Tệp video: {out_path}", flush=True)
    print(f"Dung lượng: {file_size_mb:.2f} MB", flush=True)
    print(f"Thời gian render: {total_time:.1f} giây", flush=True)
    print(f"===========================================================", flush=True)

if __name__ == '__main__':
    build_atmospheric_lava_video()
