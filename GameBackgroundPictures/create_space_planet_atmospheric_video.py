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

def build_space_planet_atmospheric_video():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\space_planet_4k_nowatermark.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_name = 'space_planet_atmospheric_loop.mp4'
    out_path = os.path.join(out_dir, out_name)
    downloads_path = os.path.join(r'C:\Users\ADMIN\Downloads', out_name)

    print("===========================================================", flush=True)
    print(f"KHỞI ĐỘNG XUẤT VIDEO: {out_name}", flush=True)
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
    # 2. Phân tầng hình học chuẩn xác (Layer Masks)
    # ----------------------------------------------------
    print("2. Tính toán mặt nạ phân tầng (Layer Masks)...", flush=True)

    # A. Đường chân trời khí quyển Tiền cảnh (Foreground Atmospheric Horizon)
    # y = 3.571329e-05 * x^2 + 0.1618 * x + 809.56
    fg_edge_y = 3.571329e-05 * (x_grid**2) + 0.1618 * x_grid + 809.56
    fg_dist = y_grid - fg_edge_y
    fg_mask_f = np.clip((fg_dist + 5.0) / 10.0, 0.0, 1.0)
    fg_bool = fg_mask_f > 0.01

    # B. Hành tinh Băng / Kim loại (Giant Icy Planet on Left)
    moon_cx, moon_cy, moon_r = 352.3, 1317.3, 916.7
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask_f = np.clip((moon_r + 2.0 - moon_dist) / 4.0, 0.0, 1.0) * (1.0 - fg_mask_f)
    moon_bool = (moon_mask_f > 0.01) & (~fg_bool)

    # C. Không gian sâu (Deep Space)
    space_mask_f = np.clip((1.0 - moon_mask_f) * (1.0 - fg_mask_f), 0.0, 1.0)
    space_bool = space_mask_f > 0.05

    # ----------------------------------------------------
    # 3. Dòng Mây tầng đối lưu Tiền cảnh (Tropospheric Cloud Advection)
    # ----------------------------------------------------
    print("3. Khởi tạo thủy động lực mây tầng đối lưu tiền cảnh...", flush=True)
    # Bóc tách lớp mây trắng trên bề mặt hành tinh tiền cảnh
    # Mây có độ phản xạ trắng cao (R, G, B đều lớn)
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    cloud_score = np.clip((r_ch * 0.3 + g_ch * 0.35 + b_ch * 0.35 - 0.25) / 0.75, 0.0, 1.0) * fg_mask_f
    cloud_mask = (cloud_score > 0.25) & (fg_dist > 25.0)  # Nằm sâu dưới lớp khí quyển viền

    # Véc-tơ tiếp tuyến trôi mây theo độ cong hành tinh
    # Đạo hàm của đường cong: dy/dx = 2 * 3.57e-5 * x + 0.1618
    slope = 2.0 * 3.571329e-05 * x_grid + 0.1618
    norm_slope = np.sqrt(1.0 + slope**2)
    t_cloud_x = 1.0 / norm_slope
    t_cloud_y = slope / norm_slope

    # Vận tốc mây trôi tự nhiên (chảy chậm rãi, êm dịu: 18 px/cycle)
    v_cloud_x = t_cloud_x * 18.0 * (fg_mask_f ** 1.2)
    v_cloud_y = t_cloud_y * 18.0 * (fg_mask_f ** 1.2)
    cloud_blend_mask = (cv2.GaussianBlur(cloud_mask.astype(np.float32), (15, 15), 5.0) * (fg_mask_f ** 2.0))[:, :, None]

    # ----------------------------------------------------
    # 4. Hẻm núi địa chất Hành tinh Băng & Năng lượng Cryo (Cryo-Rift Energy)
    # ----------------------------------------------------
    print("4. Dò cấu trúc rãnh nứt địa chất & năng lượng hành tinh băng...", flush=True)
    gray_f = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.float32)

    # Hessian ridge filter tìm rãnh nứt lớn và hẻm núi thẳng đứng
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

    ridge_cryo1 = get_ridges(gray_f, 2.5)
    ridge_cryo2 = get_ridges(gray_f, 5.0)
    ridge_cryo = (ridge_cryo1 + ridge_cryo2 * 1.6)
    ridge_cryo = ridge_cryo / (np.percentile(ridge_cryo, 99.2) + 1e-5)

    # Lọc rãnh nứt nằm trên hành tinh băng
    cryo_rift_mask = (ridge_cryo > 0.38) & moon_bool
    cryo_rift_u8 = cryo_rift_mask.astype(np.uint8) * 255

    # Structure Tensor cho rãnh nứt hành tinh băng
    Ix_cryo = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 1, 0, ksize=5)
    Iy_cryo = cv2.Sobel(cv2.GaussianBlur(gray_f, (11, 11), 2.0), cv2.CV_32F, 0, 1, ksize=5)
    Jxx_c = cv2.GaussianBlur(Ix_cryo * Ix_cryo, (15, 15), 3.0)
    Jyy_c = cv2.GaussianBlur(Iy_cryo * Iy_cryo, (15, 15), 3.0)
    Jxy_c = cv2.GaussianBlur(Ix_cryo * Iy_cryo, (15, 15), 3.0)
    theta_cryo = 0.5 * np.arctan2(2.0 * Jxy_c, Jxx_c - Jyy_c) + (np.pi / 2.0)
    t_cryo_x = np.cos(theta_cryo)
    t_cryo_y = np.sin(theta_cryo)
    # Hướng năng lượng chảy xuôi xuống phía nam
    flip_c = t_cryo_y < 0
    t_cryo_x[flip_c] *= -1.0
    t_cryo_y[flip_c] *= -1.0

    dist_wall_cryo = cv2.distanceTransform(cryo_rift_u8, cv2.DIST_L2, 5)
    dist_norm_cryo = np.clip(dist_wall_cryo / 5.0, 0.0, 1.0)
    prof_cryo = (dist_norm_cryo ** 1.5) * moon_mask_f

    v_cryo_x = t_cryo_x * prof_cryo * 16.0
    v_cryo_y = t_cryo_y * prof_cryo * 16.0
    cryo_blend_mask = prof_cryo[:, :, None]

    # Texture năng lượng điện quang / Cryo core phát sáng
    cryo_glow_tex = cv2.GaussianBlur(np.clip(ridge_cryo * moon_mask_f, 0.0, 1.0), (11, 11), 3.0)
    cryo_core_tex = cv2.GaussianBlur(np.clip(ridge_cryo * moon_mask_f, 0.0, 1.0), (3, 3), 0.8)

    # ----------------------------------------------------
    # 5. Hợp nhất Trường Vận tốc 2 Pha Toàn cục
    # ----------------------------------------------------
    print("5. Hợp nhất trường vận tốc thủy động lực (Zero Rock Deformation)...", flush=True)
    v_total_x = v_cloud_x * (fg_mask_f) + v_cryo_x * (moon_mask_f)
    v_total_y = v_cloud_y * (fg_mask_f) + v_cryo_y * (moon_mask_f)

    # ----------------------------------------------------
    # 6. Ngôi sao Xanh, Vành sáng khí quyển & Vũ trụ sâu
    # ----------------------------------------------------
    print("6. Khởi tạo hiệu ứng tán xạ quang học và vũ trụ sâu...", flush=True)
    # Ngôi sao xanh khổng lồ (2022, 401)
    star_cx, star_cy = 2022.0, 401.0
    dist_star = np.hypot(x_grid - star_cx, y_grid - star_cy)
    star_halo_base = np.exp(-dist_star / 160.0)

    # Dải tia sáng Anamorphic Flare ngang
    dy_star = np.abs(y_grid - star_cy)
    anamorphic_beam = np.exp(-(dy_star**2) / (2.0 * (14.0**2))) * np.exp(-np.abs(x_grid - star_cx) / 1200.0)

    # Vành chân trời khí quyển phát quang (Rayleigh Blue Rim)
    horizon_dist = np.abs(fg_dist)
    horizon_rim_mask = np.exp(-(horizon_dist**2) / (2.0 * (15.0**2)))

    # Vệ tinh / Phi thuyền vũ trụ ở (910, 365)
    probe_x, probe_y = 910.0, 365.0
    dist_probe = np.hypot(x_grid - probe_x, y_grid - probe_y)
    probe_halo = np.exp(-dist_probe / 18.0)

    # 90 ngôi sao lấp lánh trong không gian sâu
    np.random.seed(888)
    star_cands = np.where((gray_f > 120) & (space_bool) & (dist_star > 260) & (dist_probe > 60))
    star_sel = np.random.choice(len(star_cands[0]), size=min(90, len(star_cands[0])), replace=False)
    star_coords = list(zip(star_cands[1][star_sel], star_cands[0][star_sel]))
    star_phases = np.random.uniform(0, 2 * np.pi, size=len(star_coords))
    star_freqs = np.random.choice([1, 2, 3], size=len(star_coords))

    # Sao băng vi mô (Passing Meteor Streak)
    meteor_len = 140.0
    meteor_dir_x, meteor_dir_y = 0.85, 0.52

    # ----------------------------------------------------
    # 7. Khởi động FFMPEG Render Pipeline (Tối ưu Vectorized)
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
        '-crf', '16',          # Độ nét tuyệt đối
        '-preset', 'medium',
        out_path
    ]

    print(f"7. Bắt đầu Render {total_frames} frames (Seamless 12.0s Loop, 30fps)...", flush=True)
    pipe = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    t_start = time.time()
    base_f = img.astype(np.float32)

    for frame_idx in range(total_frames):
        t = frame_idx / fps
        theta0 = omega0 * t

        # --- A. Thủy động lực 2 pha tuần hoàn (Dual-Phase Cyclic Advection) ---
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

        advected = sample1 * w1 + sample2 * w2

        # Ghép mây tầng đối lưu & hẻm núi (Vectorized Multiply-Add: Siêu nhanh, Zero Rock Leakage)
        composite = base_f + (advected - base_f) * (cloud_blend_mask * 0.75 + cryo_blend_mask * 0.85)

        # --- B. Phát quang Năng lượng Khe nứt Băng (Electric Cyan Plasma Pulse) ---
        cryo_pulse = 0.85 + 0.15 * np.sin(omega0 * t * 2.0)
        c_glow = cryo_glow_tex * cryo_pulse
        c_core = cryo_core_tex * cryo_pulse
        # Sắc màu năng lượng điện quang Cyan: B=255, G=220, R=110
        composite[:, :, 0] = np.maximum(composite[:, :, 0], (c_glow * 180.0 + c_core * 255.0) * moon_mask_f)
        composite[:, :, 1] = np.maximum(composite[:, :, 1], (c_glow * 140.0 + c_core * 220.0) * moon_mask_f)
        composite[:, :, 2] = np.maximum(composite[:, :, 2], (c_glow * 60.0 + c_core * 120.0) * moon_mask_f)

        # --- C. Nhịp thở Ngôi sao Xanh & Dải Anamorphic Flare ---
        star_pulse = 0.045 * np.sin(2.0 * omega0 * t)  # Chu kỳ 6.0s (2 nhịp thở trong 12s)
        star_boost = star_halo_base * star_pulse * 255.0

        # Anamorphic shimmer
        beam_shimmer = (0.08 * np.sin(4.0 * omega0 * t) + 0.04 * np.sin(7.0 * omega0 * t))
        beam_boost = anamorphic_beam * (0.12 + beam_shimmer) * 255.0

        # Vành chân trời phát quang bừng sáng theo nhịp
        rim_boost = horizon_rim_mask * (star_pulse * 1.6) * 255.0

        composite[:, :, 0] = np.clip(composite[:, :, 0] + star_boost * 1.0 + beam_boost * 1.0 + rim_boost * 1.0, 0, 255)
        composite[:, :, 1] = np.clip(composite[:, :, 1] + star_boost * 0.9 + beam_boost * 0.85 + rim_boost * 0.85, 0, 255)
        composite[:, :, 2] = np.clip(composite[:, :, 2] + star_boost * 0.75 + beam_boost * 0.65 + rim_boost * 0.6, 0, 255)

        # --- D. Tín hiệu Beacon Vệ tinh / Phi thuyền vũ trụ (Space Beacon Pulse) ---
        beacon_pulse = (np.sin(omega0 * t * 4.0) ** 6)  # Chớp sáng sắc nét mỗi 3.0s
        probe_boost = probe_halo * beacon_pulse * 220.0
        composite[:, :, 0] = np.clip(composite[:, :, 0] + probe_boost * 1.0, 0, 255)
        composite[:, :, 1] = np.clip(composite[:, :, 1] + probe_boost * 0.95, 0, 255)
        composite[:, :, 2] = np.clip(composite[:, :, 2] + probe_boost * 0.8, 0, 255)

        # --- E. Sao băng lướt qua khoảng không (Passing Relativistic Meteor) ---
        meteor_cycle = 6.0  # 2 lần trong 12s
        m_t = (t % meteor_cycle) / meteor_cycle
        if m_t < 0.25:  # Chỉ xuất hiện trong 1.5s đầu của chu kỳ
            m_prog = m_t / 0.25
            m_head_x = 1300.0 + m_prog * 850.0
            m_head_y = 120.0 + m_prog * 520.0
            # Vệt đuôi
            m_dist_sq = ((x_grid - m_head_x) * meteor_dir_y - (y_grid - m_head_y) * meteor_dir_x)**2
            m_along = (x_grid - m_head_x) * meteor_dir_x + (y_grid - m_head_y) * meteor_dir_y
            m_tail = (m_along < 0) & (m_along > -meteor_len) & (m_dist_sq < 3.5)
            m_tail_f = np.clip((1.0 + m_along / meteor_len) * (1.0 - m_dist_sq / 3.5), 0.0, 1.0) * space_mask_f
            composite[:, :, 0] = np.clip(composite[:, :, 0] + m_tail_f * 255.0, 0, 255)
            composite[:, :, 1] = np.clip(composite[:, :, 1] + m_tail_f * 240.0, 0, 255)
            composite[:, :, 2] = np.clip(composite[:, :, 2] + m_tail_f * 200.0, 0, 255)

        # --- F. Sao lấp lánh trong không gian sâu ---
        for (sx, sy), s_phase, s_freq in zip(star_coords, star_phases, star_freqs):
            star_twinkle = 0.28 * np.sin(s_freq * omega0 * t + s_phase)
            composite[sy-1:sy+2, sx-1:sx+2] = np.clip(
                composite[sy-1:sy+2, sx-1:sx+2] * (1.0 + star_twinkle), 0, 255
            )

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
    build_space_planet_atmospheric_video()
