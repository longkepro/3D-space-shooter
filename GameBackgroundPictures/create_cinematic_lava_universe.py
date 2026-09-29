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

def create_cinematic_lava_universe():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_4k_lossless.png'
    out_video_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_video_name = 'lava_planets_cinematic_2_5d.mp4'
    out_video_path = os.path.join(out_video_dir, out_video_name)
    downloads_path = os.path.join(r'C:\Users\ADMIN\Downloads', out_video_name)

    print(f"=== KHỞI ĐỘNG HỆ THỐNG CINEMATIC 2.5D: {out_video_name} ===")
    print(f"Nguồn ảnh: {src_path}")
    
    img_full = cv2.imread(src_path)
    if img_full is None:
        raise FileNotFoundError(f"Không thể đọc file: {src_path}")

    target_w, target_h = 2560, 1440
    print(f"Resize về chuẩn 2K QHD: {target_w}x{target_h}...")
    img = cv2.resize(img_full, (target_w, target_h), interpolation=cv2.INTER_AREA)
    img_f = img.astype(np.float32) / 255.0

    fps = 30
    duration = 16.0  # seconds per loop
    total_frames = int(fps * duration)
    omega0 = 2.0 * np.pi / duration
    flow_cycle = 8.0  # exactly 2 cycles of magma convection per 16s
    omega_flow = 2.0 * np.pi / flow_cycle

    # ----------------------------------------------------
    # 1. Tọa độ ma trận & Địa danh thiên thể
    # ----------------------------------------------------
    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)

    sun_x, sun_y = 427.0, 246.0
    moon_cx, moon_cy = 704.0, 760.0
    moon_r = 102.0
    main_cx, main_cy = 1180.0, 780.0
    main_r = 870.0

    print("1. Tạo các mặt nạ phân tầng (Layer Masks)...")
    # Foreground Planet Mask
    fg_edge_y = 0.000199 * (x_grid**2) - 1.7023 * x_grid + 3620.39
    fg_dist = y_grid - fg_edge_y
    fg_mask = np.clip((fg_dist + 5.0) / 10.0, 0.0, 1.0)

    # Moon Mask
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask = np.clip((moon_r + 2.0 - moon_dist) / 3.0, 0.0, 1.0)

    # Main Planet Mask
    main_dist = np.hypot(x_grid - main_cx, y_grid - main_cy)
    main_planet_circle = np.clip((main_r + 3.0 - main_dist) / 6.0, 0.0, 1.0)
    main_planet_mask = main_planet_circle * (1.0 - fg_mask)

    # Deep Space Mask
    space_mask = np.clip((1.0 - main_planet_circle) * (1.0 - fg_mask), 0.0, 1.0)

    # ----------------------------------------------------
    # 2. Inpainting (Xử lý lỗ hổng che khuất để Parallax mượt mà)
    # ----------------------------------------------------
    print("2. Inpainting khôi phục bề mặt bị che khuất (Clean Plates)...")
    # Inpaint sau mặt trăng: để khi mặt trăng trôi theo thị sai không bị lộ vệt đen
    moon_inpaint_mask = np.zeros((target_h, target_w), dtype=np.uint8)
    cv2.circle(moon_inpaint_mask, (int(moon_cx), int(moon_cy)), int(moon_r + 14), 255, -1)
    img_behind_moon = cv2.inpaint(img, moon_inpaint_mask, 9, cv2.INPAINT_TELEA).astype(np.float32) / 255.0

    # Inpaint sau tiền cảnh: cho phép tiền cảnh trôi dạt độc lập
    fg_inpaint_mask = np.zeros((target_h, target_w), dtype=np.uint8)
    fg_strip = (fg_dist > -25.0) & (fg_dist < 40.0)
    fg_inpaint_mask[fg_strip] = 255
    img_behind_fg = cv2.inpaint((img_behind_moon * 255.0).astype(np.uint8), fg_inpaint_mask, 9, cv2.INPAINT_TELEA).astype(np.float32) / 255.0

    # ----------------------------------------------------
    # 3. Phân tách Magma & Mô phỏng Đối lưu Nhiệt Động Học
    # ----------------------------------------------------
    print("3. Phân tách Magma và sinh trường đối lưu nhiệt & vảy đá nguội...")
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    magma_score = np.clip((r_ch - b_ch * 1.3) * 1.6, 0.0, 1.0) * np.clip((r_ch - 0.25) / 0.75, 0.0, 1.0)
    magma_mask = (magma_score > 0.20) & (r_ch > g_ch * 1.08)
    magma_mask_f = cv2.GaussianBlur(magma_mask.astype(np.float32), (9, 9), 0)

    # Trường vận tốc véc-tơ Magma
    np.random.seed(42)
    psi_magma = cv2.GaussianBlur(np.random.randn(target_h // 16, target_w // 16).astype(np.float32), (0, 0), 4)
    psi_magma = cv2.resize(psi_magma, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 85.0
    v_magma_y = (cv2.Sobel(psi_magma, cv2.CV_32F, 0, 1, ksize=5) + 38.0) * magma_mask_f
    v_magma_x = (-cv2.Sobel(psi_magma, cv2.CV_32F, 1, 0, ksize=5) + 16.0) * magma_mask_f

    magma_spd = np.hypot(v_magma_x, v_magma_y) + 1e-5
    v_magma_x = v_magma_x * (34.0 / np.max(magma_spd)) * magma_mask_f
    v_magma_y = v_magma_y * (34.0 / np.max(magma_spd)) * magma_mask_f

    # Vảy đá nguội (Basalt Cooling Flakes) trôi theo dòng magma
    np.random.seed(777)
    noise_flakes = cv2.GaussianBlur(np.random.randn(target_h // 6, target_w // 6).astype(np.float32), (0, 0), 1.5)
    noise_flakes = cv2.resize(noise_flakes, (target_w, target_h), interpolation=cv2.INTER_CUBIC)
    # Vùng nhiệt độ cực cao (magma_score > 0.5) làm vảy tan chảy
    melt_factor = np.clip(1.0 - magma_score * 1.4, 0.1, 1.0)
    flake_texture = np.clip(1.0 + (noise_flakes * 0.45 * melt_factor), 0.45, 1.45)

    # Tropospheric Cloud Flow (Bán cầu Tây của hành tinh dung nham)
    psi_clouds = cv2.GaussianBlur(np.random.randn(target_h // 32, target_w // 32).astype(np.float32), (0, 0), 4)
    psi_clouds = cv2.resize(psi_clouds, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 110.0
    v_cloud_y = cv2.Sobel(psi_clouds, cv2.CV_32F, 0, 1, ksize=5) * 0.35
    v_cloud_x = (-cv2.Sobel(psi_clouds, cv2.CV_32F, 1, 0, ksize=5) + 18.0) * 0.35
    west_mask = np.clip((1200.0 - x_grid) / 400.0, 0.0, 1.0) * main_planet_mask * (1.0 - magma_mask_f)
    v_cloud_x *= west_mask
    v_cloud_y *= west_mask

    flow_x_main = v_magma_x + v_cloud_x
    flow_y_main = v_magma_y + v_cloud_y

    # ----------------------------------------------------
    # 4. Cột Phun Trào Khí & Bụi Tro Vào Chân Không (Vacuum Plumes)
    # ----------------------------------------------------
    print("4. Cấu trúc các cột phun trào tro bụi (Vacuum Plumes)...")
    # 3 miệng núi lửa trên rìa hành tinh
    vents = [
        {"pos": (760.0, 220.0), "dir": (-0.6, -0.8), "scale": 110.0, "intensity": 0.35},
        {"pos": (580.0, 460.0), "dir": (-0.85, -0.5), "scale": 90.0, "intensity": 0.28},
        {"pos": (1080.0, 125.0), "dir": (-0.2, -0.98), "scale": 130.0, "intensity": 0.32},
    ]

    plume_bases = []
    for vent in vents:
        vx, vy = vent["pos"]
        dx, dy = vent["dir"]
        norm = np.hypot(dx, dy)
        dx, dy = dx / norm, dy / norm
        
        # Chiếu tọa độ theo hướng vòi phun
        rx = x_grid - vx
        ry = y_grid - vy
        d_par = rx * dx + ry * dy
        d_perp = np.abs(rx * (-dy) + ry * dx)

        cone_mask = (d_par > 0.0) & (d_par < vent["scale"] * 3.0)
        plume_shape = np.exp(-d_par / vent["scale"]) * np.exp(-(d_perp**2) / (2.0 * (0.35 * d_par + 6.0)**2)) * cone_mask
        plume_bases.append({
            "shape": plume_shape.astype(np.float32),
            "d_par": d_par,
            "intensity": vent["intensity"]
        })

    # ----------------------------------------------------
    # 5. Ngôi Sao, Tai Lửa & Tia Sáng Quét (Prominences & God Rays)
    # ----------------------------------------------------
    print("5. Thiết lập hào quang ngôi sao & chùm tia sáng (God Rays)...")
    r_sun = np.hypot(x_grid - sun_x, y_grid - sun_y)
    theta_sun = np.arctan2(y_grid - sun_y, x_grid - sun_x)

    # Nhật hoa nền tĩnh
    sun_corona_radial = np.exp(-r_sun / 140.0)

    # Chùm tia God Rays phân kỳ
    ray_pattern = (
        np.sin(7.0 * theta_sun) * 0.40 +
        np.sin(13.0 * theta_sun + 0.8) * 0.30 +
        np.cos(21.0 * theta_sun - 1.2) * 0.25
    )
    ray_pattern = np.clip((ray_pattern + 0.9) / 1.8, 0.0, 1.0)
    god_rays_base = ray_pattern * sun_corona_radial * (1.0 - main_planet_circle * 0.8)

    # ----------------------------------------------------
    # 6. Mặt Trăng Tiền Cảnh: Vector 3D & Decoupled Light Field
    # ----------------------------------------------------
    print("6. Chiếu cầu 3D & tách ánh sáng mặt trăng tiền cảnh...")
    r_int = int(moon_r) + 8
    y_box, x_box = np.indices((2 * r_int, 2 * r_int), dtype=np.float32)
    dx_box = (x_box - r_int) / moon_r
    dy_box = (y_box - r_int) / moon_r
    r_sq_box = dx_box**2 + dy_box**2
    moon_inside = r_sq_box <= 1.0
    z_box = np.sqrt(np.maximum(0.0, 1.0 - r_sq_box))

    lx, ly = sun_x - moon_cx, sun_y - moon_cy
    l_mag = np.hypot(lx, ly)
    lx, ly = lx / l_mag, ly / l_mag
    lz = 0.32
    l_norm = np.sqrt(lx**2 + ly**2 + lz**2)
    lx, ly, lz = lx / l_norm, ly / l_norm, lz / l_norm

    ndotl = dx_box * lx + dy_box * ly + z_box * lz
    moon_light_base = np.clip(0.35 + 0.65 * np.clip(ndotl, 0, 1) + 0.25 * np.clip(-dx_box*0.8 - dy_box*0.6, 0, 1)**3, 0.15, 1.3)[:, :, None]

    m_x0, m_y0 = int(moon_cx - r_int), int(moon_cy - r_int)
    m_x1, m_y1 = m_x0 + 2 * r_int, m_y0 + 2 * r_int
    moon_crop_orig = img_f[m_y0:m_y1, m_x0:m_x1]
    moon_albedo = np.clip(moon_crop_orig / moon_light_base, 0.0, 1.0)
    moon_albedo[~moon_inside] = 0

    # ----------------------------------------------------
    # 7. Hệ Thống Hạt Tàn Tro Bay Sát Thấu Kính (Foreground Bokeh Embers)
    # ----------------------------------------------------
    print("7. Khởi tạo hệ thống tàn tro bay lơ lửng sát camera (DoF Bokeh)...")
    np.random.seed(333)
    num_embers = 45
    ember_x0 = np.random.uniform(50, target_w - 50, num_embers)
    ember_y0 = np.random.uniform(50, target_h - 50, num_embers)
    ember_z = np.random.uniform(0.12, 0.85, num_embers)  # z càng nhỏ càng gần camera
    ember_phase_x = np.random.uniform(0, 2 * np.pi, num_embers)
    ember_phase_y = np.random.uniform(0, 2 * np.pi, num_embers)
    ember_amp_x = np.random.uniform(25, 75, num_embers)
    ember_amp_y = np.random.uniform(15, 45, num_embers)
    ember_size = (1.0 - ember_z) * 9.0 + 2.0  # bán kính blur
    ember_alpha = np.random.uniform(0.35, 0.75, num_embers) * (1.0 - ember_z * 0.4)

    # ----------------------------------------------------
    # 8. Sao Vũ Trụ Sâu (Scintillating Starfield)
    # ----------------------------------------------------
    print("8. Phát hiện sao nền vũ trụ...")
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    kernel_tophat = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    tophat = cv2.morphologyEx(gray, cv2.MORPH_TOPHAT, kernel_tophat)
    star_mask = (tophat > 25) & (space_mask > 0.85) & (r_sun > 160.0)

    num_stars, _, stats, centroids = cv2.connectedComponentsWithStats(star_mask.astype(np.uint8))
    valid_stars = []
    for i in range(1, num_stars):
        if 1 <= stats[i, cv2.CC_STAT_AREA] <= 18:
            valid_stars.append((int(centroids[i][0]), int(centroids[i][1])))

    np.random.seed(555)
    selected_stars = [valid_stars[i] for i in np.random.choice(len(valid_stars), min(90, len(valid_stars)), replace=False)]
    star_harmonics = np.random.choice([1, 2, 3], len(selected_stars))
    star_phases = np.random.uniform(0, 2 * np.pi, len(selected_stars))

    # ----------------------------------------------------
    # 9. Khởi Tạo Bộ Ghi Video Bằng FFmpeg (NVENC nếu có, fallback libx264)
    # ----------------------------------------------------
    ffmpeg_exe = imageio_ffmpeg.get_ffmpeg_exe()
    
    # Thử nghiệm NVENC hardware acceleration
    test_nvenc = subprocess.run(
        [ffmpeg_exe, '-f', 'lavfi', '-i', 'nullsrc=s=64x64:d=0.1', '-c:v', 'h264_nvenc', '-f', 'null', '-'],
        capture_output=True
    )
    use_nvenc = (test_nvenc.returncode == 0)
    print(f"Hỗ trợ tăng tốc phần cứng NVENC: {use_nvenc}")

    if use_nvenc:
        cmd_out = [
            ffmpeg_exe, '-y',
            '-f', 'rawvideo',
            '-vcodec', 'rawvideo',
            '-s', f'{target_w}x{target_h}',
            '-pix_fmt', 'bgr24',
            '-r', str(fps),
            '-i', '-',
            '-c:v', 'h264_nvenc',
            '-preset', 'p6',
            '-cq', '15',
            '-b:v', '28M',
            '-pix_fmt', 'yuv420p',
            out_video_path
        ]
    else:
        cmd_out = [
            ffmpeg_exe, '-y',
            '-f', 'rawvideo',
            '-vcodec', 'rawvideo',
            '-s', f'{target_w}x{target_h}',
            '-pix_fmt', 'bgr24',
            '-r', str(fps),
            '-i', '-',
            '-c:v', 'libx264',
            '-preset', 'medium',
            '-crf', '15',
            '-pix_fmt', 'yuv420p',
            out_video_path
        ]

    proc = subprocess.Popen(cmd_out, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    # ----------------------------------------------------
    # 10. VÒNG LẶP RENDER CHÍNH (MAIN RENDER LOOP)
    # ----------------------------------------------------
    print(f"\nBắt đầu render {total_frames} khung hình (16.0s @ {fps}fps)...")
    start_time = time.time()

    # Biên độ camera bồng bềnh (6-DOF Micro Lissajous Drift)
    cam_amp_x = 7.5
    cam_amp_y = 5.0

    for f_idx in range(total_frames):
        t = f_idx / fps

        # A. Tọa độ Camera trôi dạt khép kín (Zero-G Lissajous Drift)
        cam_x = cam_amp_x * np.sin(omega0 * t)
        cam_y = cam_amp_y * np.sin(2.0 * omega0 * t)

        # B. Dòng chảy 2 pha Magma (Thermal 2-Phase Advection)
        u = t / flow_cycle
        p1 = u % 1.0
        p2 = (u + 0.5) % 1.0
        w1 = 1.0 - 2.0 * abs(p1 - 0.5)
        w2 = 1.0 - w1

        # Parallax cho Hành tinh Dung nham (k_parallax = -0.35)
        px_main = -0.35 * cam_x
        py_main = -0.35 * cam_y

        m1_x = np.clip(x_grid + px_main + flow_x_main * (p1 - 0.5), 0, target_w - 1).astype(np.float32)
        m1_y = np.clip(y_grid + py_main + flow_y_main * (p1 - 0.5), 0, target_h - 1).astype(np.float32)
        m2_x = np.clip(x_grid + px_main + flow_x_main * (p2 - 0.5), 0, target_w - 1).astype(np.float32)
        m2_y = np.clip(y_grid + py_main + flow_y_main * (p2 - 0.5), 0, target_h - 1).astype(np.float32)

        main_w1 = cv2.remap(img_behind_moon, m1_x, m1_y, interpolation=cv2.INTER_LINEAR)
        main_w2 = cv2.remap(img_behind_moon, m2_x, m2_y, interpolation=cv2.INTER_LINEAR)
        adv_planet = main_w1 * w1 + main_w2 * w2

        # Trôi vảy đá nguội
        flk_1 = cv2.remap(flake_texture, m1_x, m1_y, interpolation=cv2.INTER_LINEAR)
        flk_2 = cv2.remap(flake_texture, m2_x, m2_y, interpolation=cv2.INTER_LINEAR)
        adv_flakes = flk_1 * w1 + flk_2 * w2
        magma_flowing = np.clip(adv_planet * adv_flakes[:, :, None], 0.0, 1.0)

        # Nhịp thở nhiệt dung nham (Thermal Breathing +-6%)
        thermal_pulse = 0.06 * np.sin(omega_flow * t)
        magma_glow = magma_flowing * (1.0 + thermal_pulse)

        # Composite Hành tinh: Đá rắn cố định + Magma đối lưu
        frame_planet = adv_planet * (1.0 - magma_mask_f[:, :, None]) + magma_glow * magma_mask_f[:, :, None]

        # C. Bầu Trời Vũ Trụ & Ngôi Sao (k_parallax = -0.10)
        px_star = -0.10 * cam_x
        py_star = -0.10 * cam_y
        star_map_x = np.clip(x_grid + px_star, 0, target_w - 1).astype(np.float32)
        star_map_y = np.clip(y_grid + py_star, 0, target_h - 1).astype(np.float32)
        bg_space = cv2.remap(img_behind_fg, star_map_x, star_map_y, interpolation=cv2.INTER_LINEAR)

        # Tai lửa & Hào quang nhấp nhô
        star_pulse = np.sin(omega0 * t)
        corona_mod = 1.0 + 0.12 * star_pulse
        god_rays_mod = 1.0 + 0.18 * np.sin(omega0 * t + 0.5)

        rays_pass = (god_rays_base[:, :, None] * (0.16 * god_rays_mod)) * np.array([0.9, 0.95, 1.0], dtype=np.float32)
        sun_halo_pass = (sun_corona_radial[:, :, None] * (0.15 * corona_mod)) * np.array([0.95, 0.98, 1.0], dtype=np.float32)
        bg_space += (rays_pass + sun_halo_pass)

        # D. Cột Phun Trào Tro Bụi (Vacuum Pyroclastic Plumes)
        plume_frame = np.zeros_like(img_f)
        for plume in plume_bases:
            # Nhịp nổ xung áp suất
            p_pulse = 0.5 + 0.5 * np.sin(2.0 * np.pi * (plume["d_par"] / 60.0 - t / 3.0))
            p_mask = plume["shape"] * (0.7 + 0.3 * p_pulse) * plume["intensity"]
            # Màu hạt phát sáng: Cam ở cuống, xám tro ở ngọn
            heat_dist = np.clip(1.0 - plume["d_par"] / 180.0, 0.0, 1.0)[:, :, None]
            plume_color = heat_dist * np.array([0.2, 0.55, 1.0], dtype=np.float32) + (1.0 - heat_dist) * np.array([0.45, 0.45, 0.5], dtype=np.float32)
            plume_frame += p_mask[:, :, None] * plume_color

        # Ghép Plumes vào khoảng không
        bg_space = np.clip(bg_space + plume_frame, 0.0, 1.0)

        # Ghép Hành tinh vào Nền vũ trụ
        main_mask_warped = cv2.remap(main_planet_mask, star_map_x, star_map_y, interpolation=cv2.INTER_LINEAR)[:, :, None]
        composite = bg_space * (1.0 - main_mask_warped) + frame_planet * main_mask_warped

        # E. Mặt Trăng Tiền Cảnh (3D Spherical Vector Wobble & Parallax k = -0.85)
        px_moon = -0.85 * cam_x
        py_moon = -0.85 * cam_y

        theta_m = (4.0 * np.pi / 180.0) * np.sin(omega0 * t)
        cos_tm, sin_tm = np.cos(theta_m), np.sin(theta_m)
        dx_m_rot = dx_box * cos_tm - z_box * sin_tm

        map_mx = np.clip(r_int + dx_m_rot * moon_r, 0, 2 * r_int - 1).astype(np.float32)
        map_my = np.clip(r_int + dy_box * moon_r, 0, 2 * r_int - 1).astype(np.float32)
        moon_rot_albedo = cv2.remap(moon_albedo, map_mx, map_my, interpolation=cv2.INTER_CUBIC)

        # Điều chế ánh sáng viền mặt trăng theo nhịp sao
        moon_light_dynamic = moon_light_base * (1.0 + 0.10 * star_pulse)
        moon_rendered = np.clip(moon_rot_albedo * moon_light_dynamic, 0.0, 1.0)

        # Đặt mặt trăng vào khung hình có Parallax
        cur_moon_cx = moon_cx + px_moon
        cur_moon_cy = moon_cy + py_moon
        m_x0_cur = int(cur_moon_cx - r_int)
        m_y0_cur = int(cur_moon_cy - r_int)
        m_x1_cur = m_x0_cur + 2 * r_int
        m_y1_cur = m_y0_cur + 2 * r_int

        # Paste Moon an toàn
        if 0 <= m_x0_cur and m_x1_cur <= target_w and 0 <= m_y0_cur and m_y1_cur <= target_h:
            moon_sub = composite[m_y0_cur:m_y1_cur, m_x0_cur:m_x1_cur]
            m_mask_box = moon_inside[:, :, None].astype(np.float32)
            composite[m_y0_cur:m_y1_cur, m_x0_cur:m_x1_cur] = moon_sub * (1.0 - m_mask_box) + moon_rendered * m_mask_box

        # F. Hành Tinh Tiền Cảnh Dưới Đáy (Foreground Planet Parallax k = -1.45)
        px_fg = -1.45 * cam_x
        py_fg = -1.45 * cam_y
        fg_map_x = np.clip(x_grid + px_fg, 0, target_w - 1).astype(np.float32)
        fg_map_y = np.clip(y_grid + py_fg, 0, target_h - 1).astype(np.float32)
        fg_warped = cv2.remap(img_f, fg_map_x, fg_map_y, interpolation=cv2.INTER_LINEAR)
        fg_mask_warped = cv2.remap(fg_mask, fg_map_x, fg_map_y, interpolation=cv2.INTER_LINEAR)[:, :, None]

        composite = composite * (1.0 - fg_mask_warped) + fg_warped * fg_mask_warped

        # G. Vành Sáng Anamorphic (Cinematic Anamorphic Lens Flare)
        flare_intensity = 0.16 * (1.0 + 0.14 * star_pulse)
        sun_screen_x = sun_x + px_star
        sun_screen_y = sun_y + py_star
        flare_h = np.exp(-((y_grid - sun_screen_y)**2) / (2.0 * (2.2**2))) * np.exp(-((x_grid - sun_screen_x)**2) / (2.0 * (380.0**2)))
        flare_color = np.array([0.95, 0.92, 0.7], dtype=np.float32)  # Ánh kim điện ảnh
        composite += (flare_h[:, :, None] * flare_intensity) * flare_color

        # H. Tinh Cầu Sao Nhấp Nháy (Stellar Scintillation)
        for i, (sx, sy) in enumerate(selected_stars):
            mode = star_harmonics[i]
            phase = star_phases[i]
            star_twinkle = 0.38 * np.sin(mode * omega0 * t + phase)
            # Dịch chuyển theo sao nền
            cur_sx = int(sx + px_star)
            cur_sy = int(sy + py_star)
            if 0 <= cur_sx < target_w - 1 and 0 <= cur_sy < target_h - 1:
                composite[cur_sy-1:cur_sy+2, cur_sx-1:cur_sx+2] += star_twinkle * 0.30

        # I. Tàn Tro Bay Sát Thấu Kính (Foreground Bokeh Embers - k = -2.2)
        for i in range(num_embers):
            z_e = ember_z[i]
            k_p = - (1.0 / z_e) * 0.5
            ex = (ember_x0[i] + ember_amp_x[i] * np.sin(omega0 * t + ember_phase_x[i]) + k_p * cam_x) % target_w
            ey = (ember_y0[i] + ember_amp_y[i] * np.cos(omega0 * t + ember_phase_y[i]) + k_p * cam_y) % target_h
            
            rad = int(ember_size[i])
            alpha_e = ember_alpha[i]
            x_min, x_max = max(0, int(ex - rad)), min(target_w, int(ex + rad + 1))
            y_min, y_max = max(0, int(ey - rad)), min(target_h, int(ey + rad + 1))
            
            if x_max > x_min and y_max > y_min:
                y_sub, x_sub = np.indices((y_max - y_min, x_max - x_min), dtype=np.float32)
                d_sq = (x_sub + x_min - ex)**2 + (y_sub + y_min - ey)**2
                bokeh = np.exp(-d_sq / (2.0 * (rad * 0.6)**2)) * alpha_e
                ember_col = np.array([0.2, 0.6, 1.0], dtype=np.float32)  # BGR: Cam vàng phát sáng
                composite[y_min:y_max, x_min:x_max] += bokeh[:, :, None] * ember_col

        # Xuất khung hình ra FFmpeg buffer
        frame_out = np.clip(composite * 255.0, 0, 255).astype(np.uint8)
        proc.stdin.write(frame_out.tobytes())

        if (f_idx + 1) % 60 == 0 or (f_idx + 1) == total_frames:
            elapsed = time.time() - start_time
            fps_proc = (f_idx + 1) / elapsed
            print(f"[{f_idx + 1:03d}/{total_frames}] {((f_idx + 1)/total_frames)*100:.1f}% hoàn tất - Tốc độ: {fps_proc:.1f} fps")

    proc.stdin.close()
    proc.wait()

    print(f"\nRender hoàn thành trong {time.time() - start_time:.1f} giây!")
    print(f"File video chính: {out_video_path}")
    print(f"Kích thước file: {os.path.getsize(out_video_path) / (1024*1024):.2f} MB")

    # Sao chép sang Downloads
    print(f"Sao chép sang Downloads: {downloads_path}...")
    shutil.copyfile(out_video_path, downloads_path)
    print("HOÀN TẤT THÀNH CÔNG 100%!")

if __name__ == '__main__':
    create_cinematic_lava_universe()
