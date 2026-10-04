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

def build_cinematic_designer_video():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\stage1.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_name = 'stage1_cinematic_designer.mp4'
    out_path = os.path.join(out_dir, out_name)
    downloads_path = os.path.join(r'C:\Users\ADMIN\Downloads', out_name)

    print("===========================================================", flush=True)
    print(f"KHỞI ĐỘNG XUẤT VIDEO ĐIỆN ẢNH CINEMAGRAPH: {out_name}", flush=True)
    print("PHƯƠNG PHÁP: TIÊU CHUẨN DESIGNER (HOLLYWOOD CINEMATIC REALISM)", flush=True)
    print(f"Nguồn ảnh: {src_path}", flush=True)
    print("===========================================================", flush=True)

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
    # 2. Phân tầng Layer & Tạo Clean Plates cho Parallax
    # ----------------------------------------------------
    print("2. Tính toán mặt nạ phân tầng (Layer Masks) & Inpainting Clean Plates...", flush=True)

    # A. Đường chân trời khí quyển Tiền cảnh (Foreground Planet)
    # y = 3.571329e-05 * x^2 + 0.1618 * x + 809.56
    fg_edge_y = 3.571329e-05 * (x_grid**2) + 0.1618 * x_grid + 809.56
    fg_dist = y_grid - fg_edge_y
    fg_mask_f = np.clip((fg_dist + 5.0) / 10.0, 0.0, 1.0)
    fg_bool = fg_mask_f > 0.01

    # B. Hành tinh Vệ tinh Băng/Kim loại (Midground Icy Planet)
    moon_cx, moon_cy, moon_r = 352.3, 1317.3, 916.7
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask_f = np.clip((moon_r + 2.0 - moon_dist) / 4.0, 0.0, 1.0) * (1.0 - fg_mask_f)
    moon_bool = (moon_mask_f > 0.01) & (~fg_bool)

    # C. Không gian sâu (Deep Space)
    space_mask_f = np.clip((1.0 - moon_mask_f) * (1.0 - fg_mask_f), 0.0, 1.0)
    space_bool = space_mask_f > 0.05

    # D. Inpainting Clean Plates: Xóa viền sau vệ tinh và tiền cảnh để khi Parallax trôi không bị rách nền
    inpaint_moon_strip = (moon_dist > moon_r - 2) & (moon_dist < moon_r + 16) & (fg_dist < -10)
    img_behind_moon = cv2.inpaint(img, (inpaint_moon_strip.astype(np.uint8))*255, 7, cv2.INPAINT_TELEA)

    inpaint_fg_strip = (fg_dist > -20) & (fg_dist < 8)
    img_behind_fg = cv2.inpaint(img_behind_moon, (inpaint_fg_strip.astype(np.uint8))*255, 9, cv2.INPAINT_TELEA)

    # ----------------------------------------------------
    # 3. Chuyển động Mây trôi Tiền cảnh (Puppet Pin / Cloud Drift)
    # ----------------------------------------------------
    print("3. Khởi tạo trường trôi mây tầng đối lưu tiền cảnh (Puppet-Pin Motion)...", flush=True)
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    cloud_score = np.clip((r_ch * 0.3 + g_ch * 0.35 + b_ch * 0.35 - 0.25) / 0.75, 0.0, 1.0) * fg_mask_f
    cloud_mask = (cloud_score > 0.22) & (fg_dist > 25.0)

    # Tiếp tuyến đường cong chân trời
    slope = 2.0 * 3.571329e-05 * x_grid + 0.1618
    norm_slope = np.sqrt(1.0 + slope**2)
    t_cloud_x = 1.0 / norm_slope
    t_cloud_y = slope / norm_slope

    # Vận tốc mây trôi tự nhiên cực êm (12 px/cycle ~ 1.0 px/s)
    v_cloud_x = t_cloud_x * 12.0 * (fg_mask_f ** 1.3)
    v_cloud_y = t_cloud_y * 12.0 * (fg_mask_f ** 1.3)
    cloud_blend_mask = (cv2.GaussianBlur(cloud_mask.astype(np.float32), (15, 15), 5.0) * (fg_mask_f ** 2.0))[:, :, None]

    # ----------------------------------------------------
    # 4. Chuyển động Tự quay Vi mô Vệ tinh (Sub-degree Micro-Rotation)
    # ----------------------------------------------------
    print("4. Cấu hình dịch chuyển vi mô bề mặt hành tinh vệ tinh (Micro-Drift 0.3 px/s)...", flush=True)
    # Bề mặt mặt trăng chỉ dịch chuyển vi mô 3.5 px theo chiều ngang (không quay tròn bi-a)
    moon_amp_drift = 3.5
    moon_surface_mask = cv2.GaussianBlur(moon_mask_f, (7, 7), 2.0)[:, :, None]

    # ----------------------------------------------------
    # 5. Đồng bộ Quang học Vật lý (Physical Lighting Sync)
    # ----------------------------------------------------
    print("5. Thiết lập hệ thống đồng bộ ánh sáng quang học toàn cục (Global Light Sync)...", flush=True)
    # Ngôi sao xanh khổng lồ (2022, 401)
    star_cx, star_cy = 2022.0, 401.0
    dist_star = np.hypot(x_grid - star_cx, y_grid - star_cy)
    star_halo_base = np.exp(-dist_star / 150.0)

    # Dải tia sáng Anamorphic Flare ngang
    dy_star = np.abs(y_grid - star_cy)
    anamorphic_beam = np.exp(-(dy_star**2) / (2.0 * (12.0**2))) * np.exp(-np.abs(x_grid - star_cx) / 1150.0)

    # Viền sáng Rim Light của Hành tinh vệ tinh (Bán cầu hướng về phía ngôi sao)
    norm_moon_x = (x_grid - moon_cx) / (moon_dist + 1e-5)
    norm_moon_y = (y_grid - moon_cy) / (moon_dist + 1e-5)
    dir_to_star_x = (star_cx - moon_cx) / np.hypot(star_cx - moon_cx, star_cy - moon_cy)
    dir_to_star_y = (star_cy - moon_cy) / np.hypot(star_cx - moon_cx, star_cy - moon_cy)
    cos_facing_star = np.clip(norm_moon_x * dir_to_star_x + norm_moon_y * dir_to_star_y, 0.0, 1.0)
    moon_rim_mask = np.exp(-((moon_dist - moon_r)**2) / (2.0 * (16.0**2))) * (cos_facing_star ** 1.5) * moon_mask_f

    # Viền sáng chân trời khí quyển Hành tinh lớn
    horizon_rim_mask = np.exp(-(fg_dist**2) / (2.0 * (15.0**2)))

    # ----------------------------------------------------
    # 6. Tinh vân khói thuốc, Sao lấp lánh & Sao băng duy nhất
    # ----------------------------------------------------
    print("6. Khởi tạo tinh vân khói thuốc, sao lấp lánh và sao băng đơn lẻ...", flush=True)
    # Tinh vân bụi khí xanh trôi êm
    nebula_mask = (img_f[:, :, 0] > 0.25) & (img_f[:, :, 2] < 0.2) & space_bool
    nebula_mask_f = cv2.GaussianBlur(nebula_mask.astype(np.float32), (31, 31), 10.0)[:, :, None]

    # 80 ngôi sao lấp lánh có chọn lọc bằng Opacity mượt mà
    np.random.seed(777)
    gray_f = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.float32)
    star_cands = np.where((gray_f > 130) & (space_bool) & (dist_star > 260) & (y_grid < 1000))
    star_sel = np.random.choice(len(star_cands[0]), size=min(80, len(star_cands[0])), replace=False)
    star_coords = list(zip(star_cands[1][star_sel], star_cands[0][star_sel]))
    star_phases = np.random.uniform(0, 2 * np.pi, size=len(star_coords))
    star_speeds = np.random.uniform(0.8, 1.8, size=len(star_coords))

    # Sao băng duy nhất ở giây thứ 6:
    meteor_len = 130.0
    meteor_dx, meteor_dy = 0.82, 0.57

    # ----------------------------------------------------
    # 7. Cinematic Post-Processing (Vignette & Film Grain)
    # ----------------------------------------------------
    print("7. Tạo hiệu ứng Film Grain & Optical Vignette chuẩn điện ảnh...", flush=True)
    # Vignette
    rad_norm = np.hypot((x_grid - target_w/2.0) / (target_w/2.0), (y_grid - target_h/2.0) / (target_h/2.0))
    vignette = np.clip(1.0 - 0.20 * (rad_norm ** 2.2), 0.78, 1.0)[:, :, None]

    # Film grain nhẹ nhàng (1.2% amplitude)
    np.random.seed(1234)
    grain_table = [
        (np.random.randn(target_h, target_w, 1).astype(np.float32) * 2.8) for _ in range(15)
    ]

    # ----------------------------------------------------
    # 8. FFMPEG Render Pipeline
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
        '-crf', '16',          # Chất lượng hình ảnh cực cao
        '-preset', 'medium',
        out_path
    ]

    print(f"8. Bắt đầu Render {total_frames} frames (Seamless 12.0s Loop, 30fps)...", flush=True)
    pipe = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    t_start = time.time()

    # Biên độ Parallax phi thuyền
    cam_amp = 5.0  # px

    for frame_idx in range(total_frames):
        t = frame_idx / fps
        theta0 = omega0 * t

        # A. Parallax phi thuyền lướt qua (0.5 - 1.0 px/s)
        # Hàm trôi điều hòa khép kín
        shift_cam = cam_amp * np.sin(theta0)
        shift_fg = shift_cam * 1.0
        shift_mid = shift_cam * 0.45
        shift_bg = shift_cam * 0.08

        # B. Dịch chuyển vi mô của Mặt trăng (0.3 px/s)
        shift_moon_surf = moon_amp_drift * np.sin(theta0)

        # C. Nền không gian sâu (Stars + Deep Space)
        map_bg_x = np.clip(x_grid - shift_bg, 0, target_w - 1).astype(np.float32)
        frame_bg = cv2.remap(img_behind_fg, map_bg_x, y_grid, cv2.INTER_LINEAR).astype(np.float32)

        # D. Mặt trăng Trung cảnh (Midground Icy Planet)
        # Tịnh tiến do Parallax + Tự trôi bề mặt vi mô
        map_moon_x = np.clip(x_grid - (shift_mid + shift_moon_surf), 0, target_w - 1).astype(np.float32)
        frame_moon = cv2.remap(img_behind_fg, map_moon_x, y_grid, cv2.INTER_LINEAR).astype(np.float32)

        # Ghép mặt trăng lên nền
        moon_mask_shifted = cv2.remap(moon_mask_f, np.clip(x_grid - shift_mid, 0, target_w - 1).astype(np.float32), y_grid, cv2.INTER_LINEAR)[:, :, None]
        composite = frame_bg * (1.0 - moon_mask_shifted) + frame_moon * moon_mask_shifted

        # E. Mây trôi & Tiền cảnh Hành tinh lớn (Foreground Planet)
        p1 = (t / duration) % 1.0
        p2 = ((t / duration) + 0.5) % 1.0
        w1 = 0.5 * (1.0 - np.cos(2.0 * np.pi * p1))
        w2 = 0.5 * (1.0 - np.cos(2.0 * np.pi * p2))

        m1_x = np.clip(x_grid - shift_fg - v_cloud_x * p1, 0, target_w - 1).astype(np.float32)
        m1_y = np.clip(y_grid - v_cloud_y * p1, 0, target_h - 1).astype(np.float32)
        m2_x = np.clip(x_grid - shift_fg - v_cloud_x * p2, 0, target_w - 1).astype(np.float32)
        m2_y = np.clip(y_grid - v_cloud_y * p2, 0, target_h - 1).astype(np.float32)

        s_cloud1 = cv2.remap(img, m1_x, m1_y, cv2.INTER_LINEAR).astype(np.float32)
        s_cloud2 = cv2.remap(img, m2_x, m2_y, cv2.INTER_LINEAR).astype(np.float32)
        cloud_advected = s_cloud1 * w1 + s_cloud2 * w2

        # Bề mặt tiền cảnh tịnh tiến theo Parallax
        map_fg_x = np.clip(x_grid - shift_fg, 0, target_w - 1).astype(np.float32)
        frame_fg_surface = cv2.remap(img, map_fg_x, y_grid, cv2.INTER_LINEAR).astype(np.float32)

        # Hòa trộn mây trôi vào bề mặt tiền cảnh
        fg_composite = frame_fg_surface + (cloud_advected - frame_fg_surface) * cloud_blend_mask

        # Ghép tiền cảnh lên toàn cảnh
        fg_mask_shifted = cv2.remap(fg_mask_f, map_fg_x, y_grid, cv2.INTER_LINEAR)[:, :, None]
        composite = composite * (1.0 - fg_mask_shifted) + fg_composite * fg_mask_shifted

        # --- F. Đồng bộ Ánh sáng Quang học Toàn cục (Global Light Pulse) ---
        # Nhịp thở chậm 4.0s (3 nhịp thở trong 12.0s loop)
        omega_star = 3.0 * omega0
        star_pulse = 0.028 * np.sin(omega_star * t)  # Co giãn 2.8%

        # 1. Quầng sáng ngôi sao nở nhẹ
        star_halo_boost = star_halo_base * star_pulse * 255.0
        # 2. Dải tia Anamorphic co giãn chậm
        beam_pulse = 0.05 * np.sin(omega_star * t + 0.15)
        beam_boost = anamorphic_beam * beam_pulse * 255.0
        # 3. Viền sáng Rim Light của Mặt trăng bừng sáng đồng pha
        moon_rim_boost = moon_rim_mask * (star_pulse * 2.2) * 255.0
        # 4. Viền khí quyển chân trời bừng sáng đồng pha
        horizon_rim_boost = horizon_rim_mask * (star_pulse * 1.8) * 255.0

        # Áp dụng ánh sáng đồng bộ (Tone màu trắng xanh tự nhiên)
        light_sum = (star_halo_boost + beam_boost + moon_rim_boost + horizon_rim_boost)
        composite[:, :, 0] += light_sum * 1.0   # Blue
        composite[:, :, 1] += light_sum * 0.92  # Green
        composite[:, :, 2] += light_sum * 0.82  # Red

        # --- G. Sao lấp lánh Opacity mượt mà ---
        for (sx, sy), s_phase, s_speed in zip(star_coords, star_phases, star_speeds):
            star_twinkle = 0.22 * np.sin(s_speed * omega0 * t + s_phase)
            cur_sx = int(sx + shift_bg)
            if 1 <= cur_sx < target_w - 1:
                composite[sy-1:sy+2, cur_sx-1:cur_sx+2] += star_twinkle * 28.0

        # --- H. Sao băng duy nhất lướt qua ở giây thứ 6 ---
        if 5.6 <= t <= 6.4:
            m_prog = (t - 5.6) / 0.8
            m_hx = 1400.0 + m_prog * 900.0
            m_hy = 140.0 + m_prog * 560.0
            # Bounding box cục bộ siêu tốc
            bx1, bx2 = max(0, int(m_hx - 150)), min(target_w, int(m_hx + 30))
            by1, by2 = max(0, int(m_hy - 100)), min(target_h, int(m_hy + 30))
            if bx2 > bx1 and by2 > by1:
                sub_y, sub_x = y_grid[by1:by2, bx1:bx2], x_grid[by1:by2, bx1:bx2]
                d_sq = ((sub_x - m_hx) * meteor_dy - (sub_y - m_hy) * meteor_dx)**2
                along = (sub_x - m_hx) * meteor_dx + (sub_y - m_hy) * meteor_dy
                tail = (along < 0) & (along > -meteor_len) & (d_sq < 3.2)
                tail_f = np.clip((1.0 + along / meteor_len) * (1.0 - d_sq / 3.2), 0.0, 1.0) * space_mask_f[by1:by2, bx1:bx2]
                composite[by1:by2, bx1:bx2, 0] += tail_f * 255.0
                composite[by1:by2, bx1:bx2, 1] += tail_f * 240.0
                composite[by1:by2, bx1:bx2, 2] += tail_f * 210.0

        # --- I. Color Grading: Optical Vignette & Film Grain ---
        composite = composite * vignette
        # Thêm film grain ngẫu nhiên theo chu kỳ
        grain = grain_table[frame_idx % len(grain_table)]
        composite += grain

        # Chuyển đổi khung hình ra FFmpeg
        frame_out = np.clip(composite, 0, 255).astype(np.uint8)
        pipe.stdin.write(frame_out.tobytes())

        if (frame_idx + 1) % 45 == 0 or frame_idx == total_frames - 1:
            elapsed = time.time() - t_start
            fps_render = (frame_idx + 1) / elapsed
            percent = (frame_idx + 1) * 100.0 / total_frames
            print(f"   [Render Progress] Frame {frame_idx + 1:3d}/{total_frames} ({percent:5.1f}%) - Tốc độ: {fps_render:4.1f} fps", flush=True)

    pipe.stdin.close()
    pipe.wait()

    # Đồng bộ sang Downloads
    try:
        shutil.copy2(out_path, downloads_path)
        print(f"Đã sao chép video sang Downloads: {downloads_path}", flush=True)
    except Exception as e:
        print(f"Không thể sao chép sang Downloads: {e}", flush=True)

    file_size_mb = os.path.getsize(out_path) / (1024 * 1024)
    total_time = time.time() - t_start

    print("===========================================================", flush=True)
    print("XUẤT VIDEO THÀNH CÔNG 100%!", flush=True)
    print(f"Tệp video: {out_path}", flush=True)
    print(f"Dung lượng: {file_size_mb:.2f} MB", flush=True)
    print(f"Thời gian render: {total_time:.1f} giây", flush=True)
    print("===========================================================", flush=True)

if __name__ == '__main__':
    build_cinematic_designer_video()
