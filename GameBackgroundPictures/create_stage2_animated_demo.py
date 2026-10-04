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

def render_simple_clean_animated_demo():
    src_path = r'D:\3D-Shooter-master\GameBackgroundPictures\stage2_var1_diagonal.png'
    out_dir = r'D:\3D-Shooter-master\GameBackgroundPictures'
    out_mp4 = os.path.join(out_dir, 'stage2_volcanic_animated.mp4')
    brain_mp4 = r'C:\Users\ADMIN\.gemini\antigravity\brain\bdaefadd-efec-4e27-a837-c761193a9812\stage2_volcanic_animated.mp4'

    print("===========================================================", flush=True)
    print("XUẤT VIDEO ĐIỆN ẢNH ĐƠN GIẢN, TINH TẾ (0 MÀU MÈ, 0 PHỨC TẠP)", flush=True)
    print(f"Nguồn ảnh gốc V1: {src_path}", flush=True)
    print("===========================================================", flush=True)

    img = cv2.imread(src_path)
    if img is None:
        raise FileNotFoundError(f"Không thể mở file: {src_path}")

    target_w, target_h = 1920, 1080
    img = cv2.resize(img, (target_w, target_h), interpolation=cv2.INTER_LANCZOS4)
    img_f = img.astype(np.float32) / 255.0

    b_base = img_f[:, :, 0]
    g_base = img_f[:, :, 1]
    r_base = img_f[:, :, 2]

    fps = 30
    duration = 6.0  # seconds (Seamless Loop)
    total_frames = int(fps * duration)
    omega = 2.0 * np.pi / duration

    y_grid, x_grid = np.indices((target_h, target_w), dtype=np.float32)

    # 1. Trích xuất mặt nạ dung nham tự nhiên (Lava mask)
    lava_raw = (r_base > 0.40) & (r_base > b_base * 1.55) & (g_base > b_base * 0.85)
    lava_mask = cv2.GaussianBlur(lava_raw.astype(np.float32), (5, 5), 1.5)

    # 2. Trích xuất mặt nạ mặt trời tự nhiên (Sun natural mask)
    sun_intensity = np.clip(((b_base + g_base + r_base) / 3.0 - 0.35) / 0.65, 0.0, 1.0)
    sun_mask = sun_intensity * (x_grid < 450) * (y_grid < 350)
    sun_mask = cv2.GaussianBlur(sun_mask, (15, 15), 4.0)

    # 3. Trích xuất sao vũ trụ (Stars mask)
    stars_raw = (b_base > 0.55) & (r_base < 0.6) & (y_grid < 550) & (x_grid > 220)
    stars_mask = stars_raw.astype(np.float32)

    # 4. Vành khí quyển chân trời
    scale_x = target_w / 1376.0
    scale_y = target_h / 768.0
    poly_coeffs = np.polyfit([310 * scale_x, 775 * scale_x, 1376 * scale_x], 
                             [768 * scale_y, 471 * scale_y, 205 * scale_y], 2)
    horizon_y = np.polyval(poly_coeffs, x_grid)
    dist_to_horizon = np.abs(y_grid - horizon_y)
    atmo_mask = np.exp(- (dist_to_horizon ** 2) / (2.0 * (25.0 ** 2))) * (x_grid > 250 * scale_x)

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

        # A. KHỐI NỀN TĨNH GỐC 100% (Solid celestial body - Không méo, không vẽ thêm vật thể lạ)
        r_out = r_base.copy()
        g_out = g_base.copy()
        b_out = b_base.copy()

        # B. DÒNG DUNG NHAM BẬP BÙNG NHẸ NHÀNG (Subtle Thermal Breathing - Chuẩn Integer Harmonics)
        # Sóng đối lưu nhiệt êm ái luân chuyển dọc mạng lưới nứt nẻ (Bội số nguyên chu kỳ 2.0 và 1.0)
        flow_wave = np.sin(theta * 2.0 + (x_grid + y_grid) * 0.015) * 0.12 + \
                    np.cos(theta * 1.0 - (x_grid - y_grid) * 0.02) * 0.08
        r_out += lava_mask * flow_wave * 0.35
        g_out += lava_mask * flow_wave * 0.18

        # C. MẶT TRỜI: NHỊP PHÁT SÁNG TỰ NHIÊN (Luminance Breathing - Bội số nguyên chu kỳ 1.0)
        # Chỉ tăng giảm độ sáng vi tế của chính các pixel ngôi sao và tia flare sẵn có
        sun_pulse = np.sin(theta * 1.0) * 0.15
        r_out += r_base * sun_mask * sun_pulse
        g_out += g_base * sun_mask * sun_pulse
        b_out += b_base * sun_mask * sun_pulse

        # D. VÀNH KHÍ QUYỂN BẬP BÙNG VI MÔ (Gentle Horizon Shimmer - Bội số nguyên chu kỳ 2.0)
        atmo_shimmer = np.sin(theta * 2.0 + x_grid * 0.006) * 0.08
        r_out += atmo_mask * atmo_shimmer * 0.25
        g_out += atmo_mask * atmo_shimmer * 0.12

        # E. SAO VŨ TRỤ LẤP LÁNH VI MÔ (Subtle Star Twinkle)
        star_twinkle = np.sin(theta * 3.0 + x_grid * 0.25 + y_grid * 0.6) * 0.25
        r_out += stars_mask * star_twinkle * 0.3
        g_out += stars_mask * star_twinkle * 0.3
        b_out += stars_mask * star_twinkle * 0.4

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
        print(f"HOÀN TẤT XUẤT VIDEO ĐƠN GIẢN THÀNH CÔNG: {out_mp4}", flush=True)

if __name__ == '__main__':
    render_simple_clean_animated_demo()
