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

def render_nasa_cme_video():
    base_path = r'D:\3D-Shooter-master\GameBackgroundPictures\stage2_var1_diagonal.png'
    cme_path = r'D:\3D-Shooter-master\GameBackgroundPictures\stage2_cme_nasa_style.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_mp4 = os.path.join(out_dir, 'stage2_volcanic_animated.mp4')
    brain_mp4 = r'C:\Users\ADMIN\.gemini\antigravity\brain\bdaefadd-efec-4e27-a837-c761193a9812\stage2_volcanic_animated.mp4'

    print("===========================================================", flush=True)
    print("XUẤT VIDEO NASA CORONAL MASS EJECTION (CME) PROMINENCE LOOP", flush=True)
    print("MẶT TRỜI: NHỊP PHÁT SÁNG TỰ NHIÊN (0 MÀU MÈ, GIỮ NGUYÊN NÉT VẼ)", flush=True)
    print("NÚI LỬA: VÒNG TỪ TRƯỜNG PLASMA CME XOẮN THEO CHUẨN NASA SDO 304A", flush=True)
    print("===========================================================", flush=True)

    img_base = cv2.imread(base_path)
    img_cme = cv2.imread(cme_path)

    target_w, target_h = 1920, 1080
    base_f = cv2.resize(img_base, (target_w, target_h), interpolation=cv2.INTER_LANCZOS4).astype(np.float32) / 255.0
    cme_f = cv2.resize(img_cme, (target_w, target_h), interpolation=cv2.INTER_LANCZOS4).astype(np.float32) / 255.0

    # 1. Trích xuất lớp CME plasma loop
    cme_delta = np.maximum(0.0, cme_f - base_f)
    delta_gray = np.max(cme_delta, axis=2)
    cme_mask = cv2.GaussianBlur(np.clip((delta_gray - 0.04) / 0.12, 0.0, 1.0), (11, 11), 3.0)
    cme_layer = cme_delta * cme_mask[:, :, np.newaxis]

    # 2. Mặt nạ dung nham cơ sở
    b_base, g_base, r_base = base_f[:, :, 0], base_f[:, :, 1], base_f[:, :, 2]
    lava_raw = (r_base > 0.40) & (r_base > b_base * 1.55) & (g_base > b_base * 0.85)
    lava_mask = cv2.GaussianBlur(lava_raw.astype(np.float32), (7, 7), 2.0)

    # 3. Mặt nạ mặt trời tự nhiên (Lấy trực tiếp từ độ sáng tự nhiên của sao và tia lóe)
    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)
    sun_intensity = np.clip(((b_base + g_base + r_base) / 3.0 - 0.35) / 0.65, 0.0, 1.0)
    sun_natural_mask = sun_intensity * (x_grid < 450) * (y_grid < 350)
    sun_natural_mask = cv2.GaussianBlur(sun_natural_mask, (15, 15), 4.0)

    # 4. Mặt nạ sao vũ trụ
    stars_raw = (b_base > 0.55) & (r_base < 0.6) & (y_grid < 550) & (x_grid > 220)
    stars_mask = stars_raw.astype(np.float32)

    # 5. Tọa độ chân miệng núi lửa C1
    scale_x = target_w / 1376.0
    scale_y = target_h / 768.0
    c1_cx = 775.0 * scale_x
    c1_cy = 471.0 * scale_y

    fps = 30
    duration = 6.0  # seconds (Seamless Loop)
    total_frames = int(fps * duration)
    omega = 2.0 * np.pi / duration

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

        # A. KHỐI NỀN TĨNH CƠ SỞ
        r_out = r_base.copy()
        g_out = g_base.copy()
        b_out = b_base.copy()

        # B. MẶT TRỜI: CHỈ CÓ NHỊP PHÁT SÁNG TỰ NHIÊN (Luminance Breathing - 0 Màu Mè)
        # Tăng giảm nhẹ nhàng độ phơi sáng tự nhiên của tia flare và lõi sao, không có vòng tròn hay đổi màu
        sun_pulse = np.sin(theta) * 0.16
        r_out += r_base * sun_natural_mask * sun_pulse
        g_out += g_base * sun_natural_mask * sun_pulse
        b_out += b_base * sun_natural_mask * sun_pulse

        # C. DÒNG DUNG NHAM BẬP BÙNG & ĐỐI LƯU
        flow_wave1 = np.sin(theta * 2.0 + (x_grid + y_grid) * 0.015)
        flow_wave2 = np.cos(theta * 3.0 - (x_grid - y_grid) * 0.02)
        lava_pulse = (0.20 * flow_wave1 + 0.15 * flow_wave2) * 0.30

        r_out += lava_mask * lava_pulse
        g_out += lava_mask * (lava_pulse * 0.55)

        # D. SAO LẤP LÁNH VI MÔ
        star_twinkle = np.sin(theta * 4.0 + x_grid * 0.25 + y_grid * 0.6) * 0.3
        r_out += stars_mask * star_twinkle * 0.35
        g_out += stars_mask * star_twinkle * 0.35
        b_out += stars_mask * star_twinkle * 0.45

        # E. VÒNG TỪ TRƯỜNG PHUN TRÀO NASA CORONAL MASS EJECTION (CME)
        # Chu kỳ: Bắt đầu vọt lên từ t = 0.6s, đạt đỉnh tại t = 2.4s, hạ nhiệt từ t = 3.6s, kết thúc tại t = 5.4s
        erupt_t = t - 0.6
        if 0.0 <= erupt_t <= 4.8:
            prog = erupt_t / 4.8
            plume_intensity = np.sin(prog * np.pi) ** 1.3
            
            # Sóng plasma cuồn cuộn dọc theo các thớ sợi từ trường xoắn
            plasma_wave = np.sin(theta * 5.0 + (x_grid * 0.02 - y_grid * 0.03)) * 0.18 + \
                          np.cos(theta * 3.0 + (x_grid * 0.04 + y_grid * 0.02)) * 0.12
            
            loop_factor = plume_intensity * (1.0 + plasma_wave)
            cme_active = cme_layer * loop_factor[:, :, np.newaxis]
            
            b_out += cme_active[:, :, 0]
            g_out += cme_active[:, :, 1]
            r_out += cme_active[:, :, 2]
            
            # Quầng sáng Magnetic Reconnection Flash cực nóng ở chân miệng núi lửa C1
            dist_c1 = np.hypot(x_grid - c1_cx, y_grid - c1_cy)
            flash_intensity = np.exp(-((prog - 0.25) ** 2) / 0.05) * 1.5
            c1_flash = np.exp(- (dist_c1 ** 2) / (2.0 * (50.0 ** 2))) * flash_intensity
            
            r_out += c1_flash * 1.2
            g_out += c1_flash * 0.9
            b_out += c1_flash * 0.45

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
        print(f"HOÀN TẤT XUẤT VIDEO NASA CME THÀNH CÔNG: {out_mp4}", flush=True)

if __name__ == '__main__':
    render_nasa_cme_video()
