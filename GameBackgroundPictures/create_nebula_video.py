import os
import cv2
import numpy as np
import imageio

def create_animated_nebula():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\green_nebula_hd_4k_nowatermark.png'
    out_video_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_video_path = os.path.join(out_video_dir, 'green_nebula_animated_loop.mp4')
    out_downloads_path = r'C:\Users\ADMIN\Downloads\green_nebula_animated_loop.mp4'

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
    omega0 = 2.0 * np.pi / duration
    omega_pulse = 2.0 * omega0  # 2 full breath cycles in 12s -> 6s per pulse cycle

    # 1. Coordinate Grid
    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)
    center_x = 0.508 * target_w
    center_y = 0.519 * target_h

    # 2. Extract Star Core, Starfield, and Nebula Gas Masks
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    dist_from_star = np.hypot(x_grid - center_x, y_grid - center_y)

    # Starfield extraction via Top-Hat morphology
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))
    tophat = cv2.morphologyEx(gray, cv2.MORPH_TOPHAT, kernel)
    star_mask = (tophat > 22) & (dist_from_star > 120)

    # Detect individual star coordinates for selective twinkling
    num_star_labels, star_labels, stats, centroids = cv2.connectedComponentsWithStats(star_mask.astype(np.uint8))
    # Select up to 100 brightest small stars
    valid_stars = []
    for i in range(1, num_star_labels):
        area = int(stats[i, cv2.CC_STAT_AREA])
        if 2 <= area <= 30:
            scx, scy = centroids[i]
            valid_stars.append((int(scx), int(scy)))
    
    np.random.seed(42)
    if len(valid_stars) > 120:
        indices = np.random.choice(len(valid_stars), 120, replace=False)
        selected_stars = [valid_stars[i] for i in indices]
    else:
        selected_stars = valid_stars

    # Assign each star a harmonic frequency (1, 2, or 3) and phase
    star_harmonics = np.random.choice([1, 2, 3], len(selected_stars))
    star_phases = np.random.uniform(0, 2 * np.pi, len(selected_stars))

    # Gas Weight (highest in green nebula, 0 at image borders to avoid edge pull)
    # Greenness: G - max(R, B)
    green_dominance = np.clip(img_f[:, :, 1] - 0.5 * (img_f[:, :, 0] + img_f[:, :, 2]), 0, 1)
    gas_density = cv2.GaussianBlur(img_f[:, :, 1] * 0.7 + green_dominance * 0.3, (31, 31), 0)
    
    # Boundary fade mask (so edges don't distort)
    border_x = np.minimum(x_grid / 100.0, (target_w - 1 - x_grid) / 100.0)
    border_y = np.minimum(y_grid / 100.0, (target_h - 1 - y_grid) / 100.0)
    border_fade = np.clip(border_x, 0, 1) * np.clip(border_y, 0, 1)
    gas_weight = gas_density * border_fade

    # 3. Swirling Vortices Construction
    # Vortex 1: Left cloud pillar (counter-clockwise)
    v1_x, v1_y = 0.28 * target_w, 0.58 * target_h
    dx1 = x_grid - v1_x
    dy1 = y_grid - v1_y
    r1_sq = dx1*dx1 + dy1*dy1
    v1_falloff = np.exp(-r1_sq / (2.0 * (380.0**2)))
    # Tangential flow (-dy, dx)
    flow1_x = -dy1 / (np.sqrt(r1_sq) + 50.0) * v1_falloff * 25.0
    flow1_y =  dx1 / (np.sqrt(r1_sq) + 50.0) * v1_falloff * 25.0

    # Vortex 2: Right cloud pillar (clockwise)
    v2_x, v2_y = 0.76 * target_w, 0.48 * target_h
    dx2 = x_grid - v2_x
    dy2 = y_grid - v2_y
    r2_sq = dx2*dx2 + dy2*dy2
    v2_falloff = np.exp(-r2_sq / (2.0 * (420.0**2)))
    flow2_x =  dy2 / (np.sqrt(r2_sq) + 50.0) * v2_falloff * 28.0
    flow2_y = -dx2 / (np.sqrt(r2_sq) + 50.0) * v2_falloff * 28.0

    # Vortex 3: Core gentle radial breathing expansion
    dx0 = x_grid - center_x
    dy0 = y_grid - center_y
    r0_sq = dx0*dx0 + dy0*dy0
    v0_falloff = np.exp(-r0_sq / (2.0 * (350.0**2)))
    flow0_x = dx0 / (np.sqrt(r0_sq) + 80.0) * v0_falloff * 14.0
    flow0_y = dy0 / (np.sqrt(r0_sq) + 80.0) * v0_falloff * 14.0

    # Combine flows into base field
    base_flow_x = (flow1_x + flow2_x + flow0_x) * gas_weight
    base_flow_y = (flow1_y + flow2_y + flow0_y) * gas_weight

    # Secondary orthogonal flow for true elliptical orbital swirl
    ortho_flow_x = -base_flow_y * 0.7
    ortho_flow_y =  base_flow_x * 0.7

    # 4. Precompute Star Lens Flare Components
    # Horizontal Anamorphic Streak
    flare_streak = np.exp(-((y_grid - center_y)**2) / (2.0 * (4.5**2))) * \
                   np.exp(-((x_grid - center_x)**2) / (2.0 * (450.0**2)))
    
    # 4-Point Diffraction Spikes
    # Axis 1: horizontal + vertical
    spike_h = np.exp(-((y_grid - center_y)**2) / (2.0 * (2.5**2))) * np.exp(-((x_grid - center_x)**2) / (2.0 * (200.0**2)))
    spike_v = np.exp(-((x_grid - center_x)**2) / (2.0 * (2.5**2))) * np.exp(-((y_grid - center_y)**2) / (2.0 * (180.0**2)))
    # Diagonal spikes
    u_diag1 = (x_grid - center_x) + (y_grid - center_y)
    v_diag1 = (x_grid - center_x) - (y_grid - center_y)
    spike_d1 = np.exp(-(v_diag1**2) / (2.0 * (3.5**2))) * np.exp(-(u_diag1**2) / (2.0 * (240.0**2)))
    spike_d2 = np.exp(-(u_diag1**2) / (2.0 * (3.5**2))) * np.exp(-(v_diag1**2) / (2.0 * (240.0**2)))
    star_spikes = (spike_h + spike_v + spike_d1 + spike_d2)

    # Star Halo (Breathing Bloom)
    star_halo = np.exp(-r0_sq / (2.0 * (140.0**2)))

    # 5. Rim Lighting Mask (Clouds facing the central star)
    # Gradient of nebula clouds oriented towards the star
    grad_x = cv2.Sobel(gas_density, cv2.CV_32F, 1, 0, ksize=5)
    grad_y = cv2.Sobel(gas_density, cv2.CV_32F, 0, 1, ksize=5)
    norm_r = np.sqrt(r0_sq) + 1e-4
    dir_to_star_x = -dx0 / norm_r
    dir_to_star_y = -dy0 / norm_r
    # Dot product: positive when normal faces star
    facing = grad_x * dir_to_star_x + grad_y * dir_to_star_y
    rim_mask = np.clip(facing * 3.5, 0, 1) * np.exp(-r0_sq / (2.0 * (700.0**2))) * border_fade
    rim_mask = cv2.GaussianBlur(rim_mask, (15, 15), 0)

    # 6. Setup Video Writer
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

    print("Rendering video loop...")
    for frame_idx in range(total_frames):
        t = frame_idx / fps

        # A. Harmonic Loop Functions (strictly 0 at t=0 and t=duration)
        s1 = np.sin(omega0 * t)
        c1 = np.cos(omega0 * t) - 1.0

        # Displace coordinates
        disp_x = base_flow_x * s1 + ortho_flow_x * c1
        disp_y = base_flow_y * s1 + ortho_flow_y * c1

        map_x = np.clip(x_grid + disp_x, 0, target_w - 1).astype(np.float32)
        map_y = np.clip(y_grid + disp_y, 0, target_h - 1).astype(np.float32)

        # Warp image
        frame_warped = cv2.remap(img_f, map_x, map_y, interpolation=cv2.INTER_LINEAR)

        # B. Central Star Breathing & Flare Pulsing
        # Pulse factor: 6s period (2 cycles per 12s)
        pulse = np.sin(omega_pulse * t)
        pulse_halo = 1.0 + 0.12 * pulse
        pulse_streak = 1.0 + 0.18 * pulse

        # Apply lens flare and spikes (additive blending)
        flare_color = np.array([0.9, 1.0, 0.95], dtype=np.float32)  # Bright cyan-white
        halo_layer = (star_halo[:, :, None] * (0.15 * pulse_halo)) * flare_color
        streak_layer = (flare_streak[:, :, None] * (0.22 * pulse_streak)) * flare_color
        spikes_layer = (star_spikes[:, :, None] * (0.14 * pulse_halo)) * np.array([0.95, 1.0, 0.9], dtype=np.float32)

        frame_lit = frame_warped + halo_layer + streak_layer + spikes_layer

        # C. Physical Rim Light Modulation
        # Rim light glows brighter in sync with star pulse
        rim_boost = 0.08 * (1.0 + pulse)
        rim_color = np.array([0.5, 0.95, 0.6], dtype=np.float32)  # Emerald green glow
        frame_lit += (rim_mask[:, :, None] * rim_boost) * rim_color

        # D. Starfield Selective Twinkling
        for i, (sx, sy) in enumerate(selected_stars):
            mode = star_harmonics[i]
            phase = star_phases[i]
            # Smooth sinusoidal modulation
            star_pulse = 0.4 * np.sin(mode * omega0 * t + phase)
            # 3x3 star patch
            y_min, y_max = max(0, sy - 1), min(target_h, sy + 2)
            x_min, x_max = max(0, sx - 1), min(target_w, sx + 2)
            frame_lit[y_min:y_max, x_min:x_max] += star_pulse * 0.35

        # E. Cosmic Shooting Star (Meteor)
        # Window: t between 3.2s and 4.1s
        if 3.2 <= t <= 4.1:
            t_m = (t - 3.2) / 0.9  # 0 to 1
            # Intensity envelope: ramp up, then fade
            m_intensity = np.sin(t_m * np.pi)
            
            # Trajectory: upper right quadrant across dark sky
            # Start: (1850, 180) -> End: (2280, 420)
            m_curr_x = 1850.0 + t_m * (2280.0 - 1850.0)
            m_curr_y = 180.0  + t_m * (420.0  - 180.0)
            
            # Tail length: ~70px behind head
            tail_dx = -(2280.0 - 1850.0) * 0.16
            tail_dy = -(420.0  - 180.0) * 0.16
            
            # Draw meteor line with glow
            cv2.line(frame_lit, 
                     (int(m_curr_x + tail_dx), int(m_curr_y + tail_dy)), 
                     (int(m_curr_x), int(m_curr_y)), 
                     (float(0.85 * m_intensity), float(1.0 * m_intensity), float(0.9 * m_intensity)), 
                     thickness=2, lineType=cv2.LINE_AA)
            cv2.circle(frame_lit, (int(m_curr_x), int(m_curr_y)), 3, 
                       (float(1.0 * m_intensity), float(1.0 * m_intensity), float(1.0 * m_intensity)), 
                       -1, lineType=cv2.LINE_AA)

        # Clamp and convert to RGB uint8 for video writer
        frame_out = np.clip(frame_lit * 255.0, 0, 255).astype(np.uint8)
        # OpenCV is BGR -> imageio expects RGB
        frame_rgb = cv2.cvtColor(frame_out, cv2.COLOR_BGR2RGB)
        writer.append_data(frame_rgb)

        if frame_idx % 60 == 0:
            print(f"Rendered {frame_idx}/{total_frames} frames ({frame_idx/total_frames*100:.1f}%)...")

    writer.close()
    print("Video rendered successfully!")

    # Copy to Downloads folder as well
    import shutil
    shutil.copyfile(out_video_path, out_downloads_path)
    print(f"Copied to: {out_downloads_path}")

if __name__ == '__main__':
    create_animated_nebula()
