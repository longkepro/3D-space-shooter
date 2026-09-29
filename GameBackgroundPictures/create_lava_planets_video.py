import os
import cv2
import numpy as np
import imageio

def create_animated_lava_planets():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_4k_lossless.png'
    out_video_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_animated_loop.mp4'
    out_downloads_path = r'C:\Users\ADMIN\Downloads\lava_planets_animated_loop.mp4'

    print("Loading source image...")
    img_full = cv2.imread(src_path)
    if img_full is None:
        raise FileNotFoundError(f"Cannot load {src_path}")

    # Target resolution: 2560x1440 (2K QHD 16:9)
    target_w, target_h = 2560, 1440
    img = cv2.resize(img_full, (target_w, target_h), interpolation=cv2.INTER_AREA)
    img_f = img.astype(np.float32) / 255.0

    fps = 30
    duration = 12.0  # seconds
    total_frames = int(fps * duration)
    flow_cycle = 6.0  # seconds per magma & cloud cycle (2 full cycles in 12s -> 100% seamless)
    omega0 = 2.0 * np.pi / duration
    omega_pulse = 2.0 * np.pi / flow_cycle

    # 1. Coordinate Grids
    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)

    # Key Landmarks
    sun_x, sun_y = 427.0, 246.0
    moon_cx, moon_cy = 704.0, 760.0
    moon_r = 102.0

    # 2. Celestial Region Masks
    # A. Foreground Planet: y > 0.000199*x^2 - 1.7023*x + 3620.39
    fg_edge_y = 0.000199 * (x_grid**2) - 1.7023 * x_grid + 3620.39
    fg_dist = y_grid - fg_edge_y
    fg_mask = np.clip((fg_dist + 5.0) / 10.0, 0.0, 1.0)  # Smooth antialiased boundary

    # B. Moon Mask: Circle at (moon_cx, moon_cy), r = 102
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask = np.clip((moon_r + 2.0 - moon_dist) / 3.0, 0.0, 1.0)

    # C. Main Planet Mask (excluding FG planet and background void)
    main_cx, main_cy, main_r = 1180.0, 780.0, 870.0
    main_dist = np.hypot(x_grid - main_cx, y_grid - main_cy)
    main_planet_circle = np.clip((main_r + 3.0 - main_dist) / 6.0, 0.0, 1.0)
    main_planet_mask = main_planet_circle * (1.0 - fg_mask)

    # 3. Magma Extraction (Red-orange emission fissures)
    # BGR channels
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    # Magma has high red, moderate green, low blue
    magma_intensity = np.clip((r_ch - b_ch * 1.3) * 1.5, 0.0, 1.0) * np.clip((r_ch - 0.25) / 0.75, 0.0, 1.0)
    magma_mask = (magma_intensity > 0.2) & (r_ch > g_ch * 1.1)
    magma_mask_smooth = cv2.GaussianBlur(magma_mask.astype(np.float32), (11, 11), 0)

    # 4. Construct Viscous Magma Flow Field (Constrained to Fissures)
    print("Generating viscous magma flow field...")
    # Magma flows along fracture lines and downward on the planet sphere
    # Downward gravity component + radial curl around molten lakes
    np.random.seed(555)
    psi_magma = cv2.GaussianBlur(np.random.randn(target_h // 16, target_w // 16).astype(np.float32), (0, 0), 4)
    psi_magma = cv2.resize(psi_magma, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 60.0

    # Divergence-free curl for magma
    v_magma_y = cv2.Sobel(psi_magma, cv2.CV_32F, 0, 1, ksize=5) + 12.0  # slight downward creep
    v_magma_x = -cv2.Sobel(psi_magma, cv2.CV_32F, 1, 0, ksize=5) + 6.0   # slight rightward creep

    # Scale displacement to viscous crawl: ~20px max displacement per cycle
    magma_speed = np.hypot(v_magma_x, v_magma_y) + 1e-5
    v_magma_x = v_magma_x * (20.0 / np.max(magma_speed)) * magma_mask_smooth
    v_magma_y = v_magma_y * (20.0 / np.max(magma_speed)) * magma_mask_smooth

    # 5. Atmospheric Smoke/Dust Drift on Main Planet (Western hemisphere)
    print("Generating atmospheric cloud flow...")
    psi_clouds = cv2.GaussianBlur(np.random.randn(target_h // 32, target_w // 32).astype(np.float32), (0, 0), 4)
    psi_clouds = cv2.resize(psi_clouds, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 120.0
    v_cloud_y = cv2.Sobel(psi_clouds, cv2.CV_32F, 0, 1, ksize=5) * 0.4
    v_cloud_x = (-cv2.Sobel(psi_clouds, cv2.CV_32F, 1, 0, ksize=5) + 18.0) * 0.4 # slow horizontal drift

    # Clouds mask: western hemisphere of main planet where it's dusty/dark
    west_mask = np.clip((center_x_val := 1200.0 - x_grid) / 400.0, 0.0, 1.0) * main_planet_mask * (1.0 - magma_mask_smooth)
    v_cloud_x = v_cloud_x * west_mask
    v_cloud_y = v_cloud_y * west_mask

    # Combined base flow on main planet
    flow_x_main = v_magma_x + v_cloud_x
    flow_y_main = v_magma_y + v_cloud_y

    # 6. Deep Space Background Starfield & Twinkling
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    tophat = cv2.morphologyEx(gray, cv2.MORPH_TOPHAT, kernel)
    space_mask = (1.0 - main_planet_mask) * (1.0 - fg_mask)
    star_mask = (tophat > 25) & (space_mask > 0.8) & (np.hypot(x_grid - sun_x, y_grid - sun_y) > 140.0)

    num_stars, star_labels, stats, centroids = cv2.connectedComponentsWithStats(star_mask.astype(np.uint8))
    valid_stars = []
    for i in range(1, num_stars):
        area = int(stats[i, cv2.CC_STAT_AREA])
        if 1 <= area <= 20:
            scx, scy = centroids[i]
            valid_stars.append((int(scx), int(scy)))
    
    np.random.seed(101)
    if len(valid_stars) > 80:
        indices = np.random.choice(len(valid_stars), 80, replace=False)
        selected_stars = [valid_stars[i] for i in indices]
    else:
        selected_stars = valid_stars

    star_harmonics = np.random.choice([1, 2, 3], len(selected_stars))
    star_phases = np.random.uniform(0, 2 * np.pi, len(selected_stars))

    # 7. Precompute Sun Optics (Sun at 427, 246)
    r_sun_sq = (x_grid - sun_x)**2 + (y_grid - sun_y)**2
    sun_halo = np.exp(-r_sun_sq / (2.0 * (160.0**2)))

    # Sun diffraction spikes
    spike_h = np.exp(-((y_grid - sun_y)**2) / (2.0 * (2.5**2))) * np.exp(-((x_grid - sun_x)**2) / (2.0 * (350.0**2)))
    spike_v = np.exp(-((x_grid - sun_x)**2) / (2.0 * (2.5**2))) * np.exp(-((y_grid - sun_y)**2) / (2.0 * (250.0**2)))
    u_diag = (x_grid - sun_x) + (y_grid - sun_y)
    v_diag = (x_grid - sun_x) - (y_grid - sun_y)
    spike_d1 = np.exp(-(v_diag**2) / (2.0 * (3.0**2))) * np.exp(-(u_diag**2) / (2.0 * (300.0**2)))
    spike_d2 = np.exp(-(u_diag**2) / (2.0 * (3.0**2))) * np.exp(-(v_diag**2) / (2.0 * (300.0**2)))
    sun_spikes = (spike_h + spike_v + spike_d1 + spike_d2)

    # Atmospheric Rim Light on Main Planet (Sunlit crescent edge)
    # Brightest near the sun between the sun and the planet limb
    rim_mask_main = np.clip(1.0 - np.abs(main_dist - main_r) / 45.0, 0.0, 1.0)
    sun_dir_angle = np.arctan2(y_grid - sun_y, x_grid - sun_x)
    sun_facing = np.clip(np.cos(sun_dir_angle - np.pi/4), 0.0, 1.0)
    rim_glow = rim_mask_main * sun_facing * np.exp(-r_sun_sq / (2.0 * (700.0**2)))

    # 8. Setup Video Writer
    print(f"Initializing video writer: {out_video_path} (2560x1440 @ 30fps, {total_frames} frames)...")
    writer = imageio.get_writer(
        out_video_path,
        fps=fps,
        codec='libx264',
        quality=None,
        bitrate='16M',
        pixelformat='yuv420p',
        macro_block_size=None
    )

    print("Rendering animated lava planets loop...")
    for frame_idx in range(total_frames):
        t = frame_idx / fps

        # A. 2-Phase Flow Advection for Magma & Atmosphere on Main Planet
        u = t / flow_cycle
        p1 = u % 1.0
        p2 = (u + 0.5) % 1.0
        w1 = 1.0 - 2.0 * abs(p1 - 0.5)
        w2 = 1.0 - w1

        map1_x = np.clip(x_grid + flow_x_main * (p1 - 0.5), 0, target_w - 1).astype(np.float32)
        map1_y = np.clip(y_grid + flow_y_main * (p1 - 0.5), 0, target_h - 1).astype(np.float32)

        map2_x = np.clip(x_grid + flow_x_main * (p2 - 0.5), 0, target_w - 1).astype(np.float32)
        map2_y = np.clip(y_grid + flow_y_main * (p2 - 0.5), 0, target_h - 1).astype(np.float32)

        main_warped1 = cv2.remap(img_f, map1_x, map1_y, interpolation=cv2.INTER_LINEAR)
        main_warped2 = cv2.remap(img_f, map2_x, map2_y, interpolation=cv2.INTER_LINEAR)
        frame_main = main_warped1 * w1 + main_warped2 * w2

        # B. Moon Micro-Rotation (Rocking ~1.5 degrees, 100% closed loop)
        theta_moon = 1.5 * np.pi / 180.0 * np.sin(omega0 * t)
        cos_m, sin_m = np.cos(theta_moon), np.sin(theta_moon)
        # Coordinates relative to moon center
        dx_m = x_grid - moon_cx
        dy_m = y_grid - moon_cy
        rot_mx = np.clip(moon_cx + cos_m * dx_m - sin_m * dy_m, 0, target_w - 1).astype(np.float32)
        rot_my = np.clip(moon_cy + sin_m * dx_m + cos_m * dy_m, 0, target_h - 1).astype(np.float32)
        moon_warped = cv2.remap(img_f, rot_mx, rot_my, interpolation=cv2.INTER_LINEAR)

        # Composite Moon onto Main Planet
        frame_composite = frame_main * (1.0 - moon_mask[:, :, None]) + moon_warped * moon_mask[:, :, None]

        # C. Foreground Planet Parallax (Slow 3D camera drift)
        fg_dx = 5.0 * np.sin(omega0 * t)
        fg_dy = 3.5 * (np.cos(omega0 * t) - 1.0)
        fg_map_x = np.clip(x_grid + fg_dx, 0, target_w - 1).astype(np.float32)
        fg_map_y = np.clip(y_grid + fg_dy, 0, target_h - 1).astype(np.float32)
        fg_warped = cv2.remap(img_f, fg_map_x, fg_map_y, interpolation=cv2.INTER_LINEAR)

        # Composite Foreground Planet
        frame_composite = frame_composite * (1.0 - fg_mask[:, :, None]) + fg_warped * fg_mask[:, :, None]

        # D. Magma Fissure Glow Modulation (LỰA CHỌN A: Giữ 100% màu gốc, chỉ đổi độ rực sáng)
        pulse = np.sin(omega_pulse * t)
        magma_intensity_pulse = 0.22 * pulse  # -0.22 to +0.22
        # Apply additive glow using original pixel color proportions (strictly preserving Hue/Saturation)
        magma_glow_layer = img_f * (magma_mask_smooth[:, :, None] * magma_intensity_pulse)
        frame_composite = frame_composite + magma_glow_layer

        # E. Sun & Lens Flare Breathing
        sun_pulse = np.sin(omega_pulse * t)
        sun_halo_factor = 1.0 + 0.12 * sun_pulse
        sun_streak_factor = 1.0 + 0.18 * sun_pulse

        sun_flare_color = np.array([0.9, 0.95, 1.0], dtype=np.float32) # brilliant bright white
        halo_pass = (sun_halo[:, :, None] * (0.18 * sun_halo_factor)) * sun_flare_color
        spikes_pass = (sun_spikes[:, :, None] * (0.16 * sun_streak_factor)) * sun_flare_color
        frame_composite += (halo_pass + spikes_pass)

        # F. Rim Light Synchronous Pulse on Planet
        rim_boost = 0.10 * (1.0 + sun_pulse)
        rim_color = np.array([0.85, 0.92, 1.0], dtype=np.float32)
        frame_composite += (rim_glow[:, :, None] * rim_boost) * rim_color

        # G. Deep Space Starfield Twinkling
        for i, (sx, sy) in enumerate(selected_stars):
            mode = star_harmonics[i]
            phase = star_phases[i]
            star_pulse = 0.45 * np.sin(mode * omega0 * t + phase)
            y_min, y_max = max(0, sy - 1), min(target_h, sy + 2)
            x_min, x_max = max(0, sx - 1), min(target_w, sx + 2)
            frame_composite[y_min:y_max, x_min:x_max] += star_pulse * 0.35

        # Output frame
        frame_out = np.clip(frame_composite * 255.0, 0, 255).astype(np.uint8)
        frame_rgb = cv2.cvtColor(frame_out, cv2.COLOR_BGR2RGB)
        writer.append_data(frame_rgb)

        if frame_idx % 60 == 0:
            print(f"Rendered {frame_idx}/{total_frames} frames ({frame_idx/total_frames*100:.1f}%)...")

    writer.close()
    print("Video rendered successfully!")

    # Copy to Downloads
    import shutil
    shutil.copyfile(out_video_path, out_downloads_path)
    print(f"Copied to: {out_downloads_path}")

if __name__ == '__main__':
    create_animated_lava_planets()
