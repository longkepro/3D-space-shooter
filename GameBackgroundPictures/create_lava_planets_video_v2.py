import os
import cv2
import numpy as np
import imageio

def create_animated_lava_planets_v2():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_4k_lossless.png'
    out_video_path = r'D:\3D-Shooter-master\GameBackgroundPictures\lava_planets_animated_loop.mp4'
    out_downloads_path = r'C:\Users\ADMIN\Downloads\lava_planets_animated_loop.mp4'

    print("Loading source image for V2...")
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
    flow_cycle = 6.0  # seconds per continuous magma advection cycle (2 full cycles in 12s -> 100% seamless)
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
    fg_mask = np.clip((fg_dist + 5.0) / 10.0, 0.0, 1.0)  # Antialiased boundary

    # B. Moon Mask: Circle at (moon_cx, moon_cy), r = 102
    moon_dist = np.hypot(x_grid - moon_cx, y_grid - moon_cy)
    moon_mask = np.clip((moon_r + 2.0 - moon_dist) / 3.0, 0.0, 1.0)

    # C. Main Planet Mask
    main_cx, main_cy, main_r = 1180.0, 780.0, 870.0
    main_dist = np.hypot(x_grid - main_cx, y_grid - main_cy)
    main_planet_circle = np.clip((main_r + 3.0 - main_dist) / 6.0, 0.0, 1.0)
    main_planet_mask = main_planet_circle * (1.0 - fg_mask)

    # 3. Magma Extraction (Channel & Lake Isolation)
    b_ch, g_ch, r_ch = img_f[:, :, 0], img_f[:, :, 1], img_f[:, :, 2]
    # Magma has high red, moderate green, low blue
    magma_score = np.clip((r_ch - b_ch * 1.3) * 1.6, 0.0, 1.0) * np.clip((r_ch - 0.25) / 0.75, 0.0, 1.0)
    magma_mask = (magma_score > 0.22) & (r_ch > g_ch * 1.1)
    magma_mask_f = cv2.GaussianBlur(magma_mask.astype(np.float32), (9, 9), 0)

    # 4. Construct High-Contrast Viscous Magma Flow Field
    print("Generating high-contrast magma flow vectors & moving thermal landmarks...")
    # Gravity down-and-outward flow + convective eddies inside molten lakes
    np.random.seed(42)
    psi_magma = cv2.GaussianBlur(np.random.randn(target_h // 16, target_w // 16).astype(np.float32), (0, 0), 4)
    psi_magma = cv2.resize(psi_magma, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 80.0

    # Downward gravity stream + tangential branch flow
    v_magma_y = (cv2.Sobel(psi_magma, cv2.CV_32F, 0, 1, ksize=5) + 36.0) * magma_mask_f
    v_magma_x = (-cv2.Sobel(psi_magma, cv2.CV_32F, 1, 0, ksize=5) + 18.0) * magma_mask_f

    # Normalize and scale to visible flowing crawl: ~38px displacement per cycle
    magma_spd = np.hypot(v_magma_x, v_magma_y) + 1e-5
    v_magma_x = v_magma_x * (38.0 / np.max(magma_spd)) * magma_mask_f
    v_magma_y = v_magma_y * (38.0 / np.max(magma_spd)) * magma_mask_f

    # High-contrast moving thermal streaks (filaments of bright molten liquid & cooling crust flakes)
    np.random.seed(999)
    t_noise1 = cv2.GaussianBlur(np.random.randn(target_h // 8, target_w // 8).astype(np.float32), (0, 0), 2)
    t_noise1 = cv2.resize(t_noise1, (target_w, target_h), interpolation=cv2.INTER_CUBIC)
    thermal_streaks = np.clip(1.0 + 0.40 * t_noise1, 0.55, 1.45)

    # 5. Atmospheric Tropospheric Cloud Flow (Western hemisphere of Main Planet)
    psi_clouds = cv2.GaussianBlur(np.random.randn(target_h // 32, target_w // 32).astype(np.float32), (0, 0), 4)
    psi_clouds = cv2.resize(psi_clouds, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 120.0
    v_cloud_y = cv2.Sobel(psi_clouds, cv2.CV_32F, 0, 1, ksize=5) * 0.4
    v_cloud_x = (-cv2.Sobel(psi_clouds, cv2.CV_32F, 1, 0, ksize=5) + 20.0) * 0.4

    west_mask = np.clip((1200.0 - x_grid) / 400.0, 0.0, 1.0) * main_planet_mask * (1.0 - magma_mask_f)
    v_cloud_x = v_cloud_x * west_mask
    v_cloud_y = v_cloud_y * west_mask

    # Combined base flow on main planet (Basalt rock crust has STRICTLY v_x=0, v_y=0!)
    flow_x_main = v_magma_x + v_cloud_x
    flow_y_main = v_magma_y + v_cloud_y

    # 6. Precompute Moon 3D Spherical Geometry
    print("Preparing 3D spherical projection for the moon...")
    r_int = int(moon_r) + 8
    y_box, x_box = np.indices((2 * r_int, 2 * r_int), dtype=np.float32)
    dx_box = (x_box - r_int) / moon_r
    dy_box = (y_box - r_int) / moon_r
    r_sq_box = dx_box**2 + dy_box**2
    moon_inside = r_sq_box <= 1.0
    z_box = np.sqrt(np.maximum(0.0, 1.0 - r_sq_box))

    # Directional Solar Lighting Field for Moon (Sun at top-left)
    lx = sun_x - moon_cx
    ly = sun_y - moon_cy
    l_mag = np.hypot(lx, ly)
    lx, ly = lx / l_mag, ly / l_mag
    lz = 0.35
    l_norm = np.sqrt(lx**2 + ly**2 + lz**2)
    lx, ly, lz = lx / l_norm, ly / l_norm, lz / l_norm
    ndotl = dx_box * lx + dy_box * ly + z_box * lz
    moon_light_field = np.clip(0.35 + 0.65 * np.clip(ndotl, 0, 1) + 0.25 * np.clip(-dx_box*0.8 - dy_box*0.6, 0, 1)**3, 0.15, 1.3)[:, :, None]

    # Moon Bounding Box in image
    m_x0, m_y0 = int(moon_cx - r_int), int(moon_cy - r_int)
    m_x1, m_y1 = m_x0 + 2 * r_int, m_y0 + 2 * r_int
    moon_crop_orig = img_f[m_y0:m_y1, m_x0:m_x1]
    # Decouple albedo from light field
    moon_albedo = np.clip(moon_crop_orig / moon_light_field, 0.0, 1.0)
    moon_albedo[~moon_inside] = 0

    # 7. Deep Space Background Starfield & Twinkling
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

    # 8. Precompute Sun Optics (Sun at 427, 246)
    r_sun_sq = (x_grid - sun_x)**2 + (y_grid - sun_y)**2
    sun_halo = np.exp(-r_sun_sq / (2.0 * (160.0**2)))

    spike_h = np.exp(-((y_grid - sun_y)**2) / (2.0 * (2.5**2))) * np.exp(-((x_grid - sun_x)**2) / (2.0 * (350.0**2)))
    spike_v = np.exp(-((x_grid - sun_x)**2) / (2.0 * (2.5**2))) * np.exp(-((y_grid - sun_y)**2) / (2.0 * (250.0**2)))
    u_diag = (x_grid - sun_x) + (y_grid - sun_y)
    v_diag = (x_grid - sun_x) - (y_grid - sun_y)
    spike_d1 = np.exp(-(v_diag**2) / (2.0 * (3.0**2))) * np.exp(-(u_diag**2) / (2.0 * (300.0**2)))
    spike_d2 = np.exp(-(u_diag**2) / (2.0 * (3.0**2))) * np.exp(-(v_diag**2) / (2.0 * (300.0**2)))
    sun_spikes = (spike_h + spike_v + spike_d1 + spike_d2)

    rim_mask_main = np.clip(1.0 - np.abs(main_dist - main_r) / 45.0, 0.0, 1.0)
    sun_dir_angle = np.arctan2(y_grid - sun_y, x_grid - sun_x)
    sun_facing = np.clip(np.cos(sun_dir_angle - np.pi/4), 0.0, 1.0)
    rim_glow = rim_mask_main * sun_facing * np.exp(-r_sun_sq / (2.0 * (700.0**2)))

    # 9. Setup Video Writer
    print(f"Initializing video writer V2: {out_video_path} (2560x1440 @ 30fps, {total_frames} frames)...")
    writer = imageio.get_writer(
        out_video_path,
        fps=fps,
        codec='libx264',
        quality=None,
        bitrate='16M',
        pixelformat='yuv420p',
        macro_block_size=None
    )

    print("Rendering V2 animated lava planets loop...")
    theta_moon_max = 15.0 * np.pi / 180.0  # Majestic ~26px equator travel

    for frame_idx in range(total_frames):
        t = frame_idx / fps

        # A. 2-Phase Flow Advection for Magma & Atmosphere
        u = t / flow_cycle
        p1 = u % 1.0
        p2 = (u + 0.5) % 1.0
        w1 = 1.0 - 2.0 * abs(p1 - 0.5)
        w2 = 1.0 - w1

        m1_x = np.clip(x_grid + flow_x_main * (p1 - 0.5), 0, target_w - 1).astype(np.float32)
        m1_y = np.clip(y_grid + flow_y_main * (p1 - 0.5), 0, target_h - 1).astype(np.float32)
        m2_x = np.clip(x_grid + flow_x_main * (p2 - 0.5), 0, target_w - 1).astype(np.float32)
        m2_y = np.clip(y_grid + flow_y_main * (p2 - 0.5), 0, target_h - 1).astype(np.float32)

        main_warped1 = cv2.remap(img_f, m1_x, m1_y, interpolation=cv2.INTER_LINEAR)
        main_warped2 = cv2.remap(img_f, m2_x, m2_y, interpolation=cv2.INTER_LINEAR)
        adv_main = main_warped1 * w1 + main_warped2 * w2

        # Advect thermal streaks to produce clearly visible moving magma landmarks
        s1 = cv2.remap(thermal_streaks, m1_x, m1_y, interpolation=cv2.INTER_LINEAR)
        s2 = cv2.remap(thermal_streaks, m2_x, m2_y, interpolation=cv2.INTER_LINEAR)
        adv_streaks = s1 * w1 + s2 * w2
        magma_flowing = np.clip(adv_main * adv_streaks[:, :, None], 0.0, 1.0)

        # Composite Main Planet: Basalt Rock is 100% FROZEN + Magma Channels are FLOWING!
        frame_main = img_f * (1.0 - magma_mask_f[:, :, None]) + magma_flowing * magma_mask_f[:, :, None]

        # B. 3D Spherical Vector Rotation for Moon (100% Crisp, Zero Ghosting)
        theta_m = theta_moon_max * np.sin(omega0 * t)
        cos_tm, sin_tm = np.cos(theta_m), np.sin(theta_m)
        dx_m_rot = dx_box * cos_tm - z_box * sin_tm
        
        # Sample directly from decoupled albedo inside moon ROI
        map_mx = np.clip(r_int + dx_m_rot * moon_r, 0, 2 * r_int - 1).astype(np.float32)
        map_my = np.clip(r_int + dy_box * moon_r, 0, 2 * r_int - 1).astype(np.float32)
        
        moon_rot_albedo = cv2.remap(moon_albedo, map_mx, map_my, interpolation=cv2.INTER_CUBIC)
        # Re-apply fixed solar directional light field
        moon_rendered = np.clip(moon_rot_albedo * moon_light_field, 0.0, 1.0)
        
        # Insert Moon back onto Main Planet
        frame_composite = frame_main.copy()
        moon_sub = frame_composite[m_y0:m_y1, m_x0:m_x1]
        m_mask_box = moon_inside[:, :, None].astype(np.float32)
        frame_composite[m_y0:m_y1, m_x0:m_x1] = moon_sub * (1.0 - m_mask_box) + moon_rendered * m_mask_box

        # C. Foreground Planet Parallax (Slow 3D camera drift)
        fg_dx = 5.0 * np.sin(omega0 * t)
        fg_dy = 3.5 * (np.cos(omega0 * t) - 1.0)
        fg_map_x = np.clip(x_grid + fg_dx, 0, target_w - 1).astype(np.float32)
        fg_map_y = np.clip(y_grid + fg_dy, 0, target_h - 1).astype(np.float32)
        fg_warped = cv2.remap(img_f, fg_map_x, fg_map_y, interpolation=cv2.INTER_LINEAR)

        # Composite Foreground Planet
        frame_composite = frame_composite * (1.0 - fg_mask[:, :, None]) + fg_warped * fg_mask[:, :, None]

        # D. Magma Fissure Glow Modulation (LỰA CHỌN A: Giữ 100% màu gốc, nhịp thở dịu nhẹ +-6%)
        pulse = np.sin(omega_pulse * t)
        magma_glow_pulse = 0.07 * pulse  # Gentle breathing, no disco flickering!
        magma_glow_layer = img_f * (magma_mask_f[:, :, None] * magma_glow_pulse)
        frame_composite = np.clip(frame_composite + magma_glow_layer, 0.0, 1.0)

        # E. Sun & Lens Flare Breathing
        sun_pulse = np.sin(omega_pulse * t)
        sun_halo_factor = 1.0 + 0.10 * sun_pulse
        sun_streak_factor = 1.0 + 0.14 * sun_pulse

        sun_flare_color = np.array([0.9, 0.95, 1.0], dtype=np.float32)
        halo_pass = (sun_halo[:, :, None] * (0.16 * sun_halo_factor)) * sun_flare_color
        spikes_pass = (sun_spikes[:, :, None] * (0.14 * sun_streak_factor)) * sun_flare_color
        frame_composite += (halo_pass + spikes_pass)

        # F. Rim Light Synchronous Pulse on Planet Limb
        rim_boost = 0.08 * (1.0 + sun_pulse)
        rim_color = np.array([0.85, 0.92, 1.0], dtype=np.float32)
        frame_composite += (rim_glow[:, :, None] * rim_boost) * rim_color

        # G. Deep Space Starfield Twinkling
        for i, (sx, sy) in enumerate(selected_stars):
            mode = star_harmonics[i]
            phase = star_phases[i]
            star_pulse = 0.40 * np.sin(mode * omega0 * t + phase)
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
    print("V2 Video rendered successfully!")

    # Copy to Downloads
    import shutil
    shutil.copyfile(out_video_path, out_downloads_path)
    print(f"Copied to: {out_downloads_path}")

if __name__ == '__main__':
    create_animated_lava_planets_v2()
