import os
import cv2
import numpy as np
import imageio

def create_billowing_smoke_nebula():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\green_nebula_hd_4k_nowatermark.png'
    out_video_path = r'D:\3D-Shooter-master\GameBackgroundPictures\green_nebula_animated_loop.mp4'
    out_downloads_path = r'C:\Users\ADMIN\Downloads\green_nebula_animated_loop.mp4'

    print("Loading source image...")
    img_full = cv2.imread(src_path)
    if img_full is None:
        raise FileNotFoundError(f"Cannot load {src_path}")

    # Target resolution: 2560x1440 (2K QHD 16:9)
    target_w, target_h = 2560, 1440
    img = cv2.resize(img_full, (target_w, target_h), interpolation=cv2.INTER_AREA)

    fps = 30
    duration = 12.0  # seconds
    total_frames = int(fps * duration)
    flow_cycle = 6.0  # seconds per continuous billowing cycle (2 cycles in 12s -> 100% seamless)
    omega0 = 2.0 * np.pi / duration
    omega_pulse = 2.0 * np.pi / flow_cycle

    # 1. Coordinate Grid
    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)
    center_x = 0.508 * target_w
    center_y = 0.519 * target_h

    # 2. Extract Starfield & Clean Smoke Base
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    dist_from_star = np.hypot(x_grid - center_x, y_grid - center_y)

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))
    tophat = cv2.morphologyEx(gray, cv2.MORPH_TOPHAT, kernel)
    star_mask = (tophat > 25) & (dist_from_star > 120)

    # Clean smoke base: inpaint/median blur star positions so stars do NOT get dragged by smoke
    img_f = img.astype(np.float32) / 255.0
    smoke_median = cv2.medianBlur(img, 7).astype(np.float32) / 255.0
    # Blend: keep original wherever not a star, use median blur where stars are
    star_weight = cv2.GaussianBlur(star_mask.astype(np.float32), (5, 5), 0)[:, :, None]
    smoke_base = img_f * (1.0 - star_weight) + smoke_median * star_weight

    # Fixed Star Layer (Rigidly anchored in deep space)
    stars_rigid = np.maximum(img_f - smoke_base, 0.0)

    # Extract individual stars for selective twinkling
    num_star_labels, star_labels, stats, centroids = cv2.connectedComponentsWithStats(star_mask.astype(np.uint8))
    valid_stars = []
    for i in range(1, num_star_labels):
        area = int(stats[i, cv2.CC_STAT_AREA])
        if 2 <= area <= 30:
            scx, scy = centroids[i]
            valid_stars.append((int(scx), int(scy)))
    
    np.random.seed(42)
    if len(valid_stars) > 100:
        indices = np.random.choice(len(valid_stars), 100, replace=False)
        selected_stars = [valid_stars[i] for i in indices]
    else:
        selected_stars = valid_stars

    star_harmonics = np.random.choice([1, 2, 3], len(selected_stars))
    star_phases = np.random.uniform(0, 2 * np.pi, len(selected_stars))

    # 3. Construct Billowing Smoke Curl Velocity Field (Incompressible, Zero Divergence)
    print("Constructing multi-scale curl velocity field (vorticity)...")
    # Stream function components:
    # A. Convective rising smoke plume from center-bottom
    dx_plume = (x_grid - center_x) / (target_w * 0.45)
    dy_plume = (y_grid - (center_y + 250.0)) / (target_h * 0.45)
    r_plume_sq = dx_plume**2 + (dy_plume * 1.35)**2
    # Stream function for mushroom / convective rolling billow
    psi_plume = dx_plume * np.exp(-r_plume_sq * 1.4) * 220.0

    # B. Left smoke pillar rolling vortex (Counter-clockwise roll)
    dx_l = (x_grid - 0.28 * target_w) / (target_w * 0.26)
    dy_l = (y_grid - 0.55 * target_h) / (target_h * 0.26)
    psi_left = np.exp(-(dx_l**2 + dy_l**2) * 1.3) * 160.0

    # C. Right smoke pillar rolling vortex (Clockwise roll)
    dx_r = (x_grid - 0.75 * target_w) / (target_w * 0.26)
    dy_r = (y_grid - 0.48 * target_h) / (target_h * 0.26)
    psi_right = -np.exp(-(dx_r**2 + dy_r**2) * 1.3) * 160.0

    # D. Multi-octave fine curl turbulence (wispy smoke tendrils & micro-eddies)
    np.random.seed(777)
    # Octave 1: Macro eddies
    n1 = cv2.GaussianBlur(np.random.randn(target_h // 16, target_w // 16).astype(np.float32), (0, 0), 4)
    n1 = cv2.resize(n1, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 80.0

    # Octave 2: Mid eddies
    n2 = cv2.GaussianBlur(np.random.randn(target_h // 8, target_w // 8).astype(np.float32), (0, 0), 3)
    n2 = cv2.resize(n2, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 40.0

    # Octave 3: Micro ripples
    n3 = cv2.GaussianBlur(np.random.randn(target_h // 4, target_w // 4).astype(np.float32), (0, 0), 2)
    n3 = cv2.resize(n3, (target_w, target_h), interpolation=cv2.INTER_CUBIC) * 18.0

    total_psi = psi_plume + psi_left + psi_right + n1 + n2 + n3

    # Divergence-free velocity field: v = (d_psi/dy, -d_psi/dx)
    # Incompressible flow -> div(v) = 0 -> mathematically impossible to stretch/squash like slime!
    v_y = cv2.Sobel(total_psi, cv2.CV_32F, 0, 1, ksize=5)
    v_x = -cv2.Sobel(total_psi, cv2.CV_32F, 1, 0, ksize=5)

    # Normalize and scale to natural smoke speed (~48 px maximum billowing travel per cycle)
    vel_mag = np.hypot(v_x, v_y)
    max_vel = np.max(vel_mag) + 1e-5
    target_displacement = 52.0  # pixels at 1440p
    v_x = v_x * (target_displacement / max_vel)
    v_y = v_y * (target_displacement / max_vel)

    # Gas Weighting & Boundary Stabilization
    green_dom = np.clip(img_f[:, :, 1] - 0.5 * (img_f[:, :, 0] + img_f[:, :, 2]), 0, 1)
    gas_density = cv2.GaussianBlur(img_f[:, :, 1] * 0.65 + green_dom * 0.35, (45, 45), 0)
    
    border_x = np.minimum(x_grid / 140.0, (target_w - 1 - x_grid) / 140.0)
    border_y = np.minimum(y_grid / 140.0, (target_h - 1 - y_grid) / 140.0)
    border_fade = np.clip(border_x, 0, 1) * np.clip(border_y, 0, 1)
    
    v_x = v_x * (gas_density * border_fade)
    v_y = v_y * (gas_density * border_fade)

    # 4. Central Star Optics Precomputation
    r0_sq = (x_grid - center_x)**2 + (y_grid - center_y)**2
    # Breathing Halo
    star_halo = np.exp(-r0_sq / (2.0 * (135.0**2)))
    # Horizontal Anamorphic Streak
    flare_streak = np.exp(-((y_grid - center_y)**2) / (2.0 * (4.2**2))) * \
                   np.exp(-((x_grid - center_x)**2) / (2.0 * (420.0**2)))
    # 4-Point Diffraction Spikes
    spike_h = np.exp(-((y_grid - center_y)**2) / (2.0 * (2.2**2))) * np.exp(-((x_grid - center_x)**2) / (2.0 * (210.0**2)))
    spike_v = np.exp(-((x_grid - center_x)**2) / (2.0 * (2.2**2))) * np.exp(-((y_grid - center_y)**2) / (2.0 * (190.0**2)))
    u_diag = (x_grid - center_x) + (y_grid - center_y)
    v_diag = (x_grid - center_x) - (y_grid - center_y)
    spike_d1 = np.exp(-(v_diag**2) / (2.0 * (3.0**2))) * np.exp(-(u_diag**2) / (2.0 * (250.0**2)))
    spike_d2 = np.exp(-(u_diag**2) / (2.0 * (3.0**2))) * np.exp(-(v_diag**2) / (2.0 * (250.0**2)))
    star_spikes = (spike_h + spike_v + spike_d1 + spike_d2)

    # Rim Light on cloud edges facing the central star
    grad_x = cv2.Sobel(gas_density, cv2.CV_32F, 1, 0, ksize=5)
    grad_y = cv2.Sobel(gas_density, cv2.CV_32F, 0, 1, ksize=5)
    norm_r = np.sqrt(r0_sq) + 1e-4
    dir_star_x = -(x_grid - center_x) / norm_r
    dir_star_y = -(y_grid - center_y) / norm_r
    facing = grad_x * dir_star_x + grad_y * dir_star_y
    rim_mask = np.clip(facing * 3.5, 0, 1) * np.exp(-r0_sq / (2.0 * (720.0**2))) * border_fade
    rim_mask = cv2.GaussianBlur(rim_mask, (15, 15), 0)

    # 5. Initialize Video Writer
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

    print("Rendering 2-Phase Flow Advection (Billowing Smoke)...")
    for frame_idx in range(total_frames):
        t = frame_idx / fps

        # A. 2-Phase Continuous Flow Advection (Constant forward billowing, NO rubber-band recoil!)
        u = t / flow_cycle
        p1 = u % 1.0
        p2 = (u + 0.5) % 1.0

        # Linear triangle cross-fade weights (w1 + w2 == 1.0 always)
        w1 = 1.0 - 2.0 * abs(p1 - 0.5)
        w2 = 1.0 - w1

        # Coordinates for Phase 1 & Phase 2
        map1_x = np.clip(x_grid + v_x * (p1 - 0.5), 0, target_w - 1).astype(np.float32)
        map1_y = np.clip(y_grid + v_y * (p1 - 0.5), 0, target_h - 1).astype(np.float32)

        map2_x = np.clip(x_grid + v_x * (p2 - 0.5), 0, target_w - 1).astype(np.float32)
        map2_y = np.clip(y_grid + v_y * (p2 - 0.5), 0, target_h - 1).astype(np.float32)

        # Advect the clean smoke base
        smoke_phase1 = cv2.remap(smoke_base, map1_x, map1_y, interpolation=cv2.INTER_LINEAR)
        smoke_phase2 = cv2.remap(smoke_base, map2_x, map2_y, interpolation=cv2.INTER_LINEAR)
        
        # Blended continuously billowing smoke
        smoke_blended = smoke_phase1 * w1 + smoke_phase2 * w2

        # B. Recombine Anchored Deep Stars (Stars remain rock-solid in space)
        frame_composite = smoke_blended + stars_rigid

        # C. Star Twinkling (Independent sinusoidal modulation)
        for i, (sx, sy) in enumerate(selected_stars):
            mode = star_harmonics[i]
            phase = star_phases[i]
            star_pulse = 0.45 * np.sin(mode * omega0 * t + phase)
            y_min, y_max = max(0, sy - 1), min(target_h, sy + 2)
            x_min, x_max = max(0, sx - 1), min(target_w, sx + 2)
            frame_composite[y_min:y_max, x_min:x_max] += star_pulse * 0.35

        # D. Breathing Central Star & Lens Flare
        pulse = np.sin(omega_pulse * t)
        pulse_halo = 1.0 + 0.12 * pulse
        pulse_streak = 1.0 + 0.18 * pulse

        flare_color = np.array([0.92, 1.0, 0.95], dtype=np.float32)
        halo_layer = (star_halo[:, :, None] * (0.16 * pulse_halo)) * flare_color
        streak_layer = (flare_streak[:, :, None] * (0.24 * pulse_streak)) * flare_color
        spikes_layer = (star_spikes[:, :, None] * (0.15 * pulse_halo)) * np.array([0.96, 1.0, 0.92], dtype=np.float32)

        frame_composite += (halo_layer + streak_layer + spikes_layer)

        # E. Physical Rim Light on Smoke Edges
        rim_boost = 0.08 * (1.0 + pulse)
        rim_color = np.array([0.5, 0.96, 0.62], dtype=np.float32)
        frame_composite += (rim_mask[:, :, None] * rim_boost) * rim_color

        # F. Cosmic Shooting Star (t in [3.2s, 4.1s])
        if 3.2 <= t <= 4.1:
            t_m = (t - 3.2) / 0.9
            m_intensity = np.sin(t_m * np.pi)
            m_curr_x = 1850.0 + t_m * (2280.0 - 1850.0)
            m_curr_y = 180.0  + t_m * (420.0  - 180.0)
            tail_dx = -(2280.0 - 1850.0) * 0.16
            tail_dy = -(420.0  - 180.0) * 0.16
            
            cv2.line(frame_composite, 
                     (int(m_curr_x + tail_dx), int(m_curr_y + tail_dy)), 
                     (int(m_curr_x), int(m_curr_y)), 
                     (float(0.85 * m_intensity), float(1.0 * m_intensity), float(0.9 * m_intensity)), 
                     thickness=2, lineType=cv2.LINE_AA)
            cv2.circle(frame_composite, (int(m_curr_x), int(m_curr_y)), 3, 
                       (float(1.0 * m_intensity), float(1.0 * m_intensity), float(1.0 * m_intensity)), 
                       -1, lineType=cv2.LINE_AA)

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
    create_billowing_smoke_nebula()
