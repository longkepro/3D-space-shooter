import cv2
import numpy as np
import sys
import shutil
import os

if sys.stdout:
    sys.stdout.reconfigure(encoding='utf-8')
if sys.stderr:
    sys.stderr.reconfigure(encoding='utf-8')

def clean_stage1_stripes():
    src_path = r'D:\3D-Shooter-master\Assets\Textures\Skybox\stage1.png'
    dest_path = r'D:\3D-Shooter-master\GameBackgroundPictures\stage1.png'

    print("===========================================================", flush=True)
    print("KHỬ NHIỄU SỌC MA TRẬN KHÍ QUYỂN TRÁI ĐẤT (CURVILINEAR TANGENT FILTER)", flush=True)
    print(f"Xử lý file: {src_path}", flush=True)
    print("===========================================================", flush=True)

    img = cv2.imread(src_path)
    if img is None:
        raise FileNotFoundError(f"Không thể mở: {src_path}")

    h, w = img.shape[:2]
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    y_grid, x_grid = np.indices((h, w), dtype=np.float32)

    # 1. Định vị chính xác đường cong chân trời Trái Đất
    edge_pts_x = []
    edge_pts_y = []
    for x in range(250, 1360, 5):
        col = gray[:, x]
        pts = np.where((col > 45) & (y_grid[:, x] > 180))[0]
        if len(pts) > 0:
            edge_pts_x.append(x)
            edge_pts_y.append(pts[0])

    poly = np.polyfit(edge_pts_x, edge_pts_y, 2)
    print(f"Horizon poly: {poly}", flush=True)

    # 2. Chuyển đổi tọa độ cong (Curvilinear Ribbon)
    d_range = np.arange(-40, 90, 1, dtype=np.float32)
    s_range = np.arange(0, w, 1, dtype=np.float32)
    s_grid, d_grid = np.meshgrid(s_range, d_range)

    map_x = s_grid.astype(np.float32)
    map_y = (np.polyval(poly, s_grid) + d_grid).astype(np.float32)

    ribbon = cv2.remap(img, map_x, map_y, cv2.INTER_LANCZOS4)

    # 3. Lọc phẳng các sọc dọc 101 độ chỉ dọc theo trục s (tiếp tuyến chân trời)
    ribbon_cleaned = cv2.GaussianBlur(ribbon, (55, 1), sigmaX=16.0, sigmaY=0)
    ribbon_cleaned = cv2.bilateralFilter(ribbon_cleaned, d=5, sigmaColor=30, sigmaSpace=10)

    # 4. Tái lập tọa độ Descartes (Cartesian Remap)
    dist_d = y_grid - np.polyval(poly, x_grid)
    remap_ribbon_y = (dist_d + 40.0).astype(np.float32)
    remap_ribbon_x = x_grid.astype(np.float32)
    atmo_restored = cv2.remap(ribbon_cleaned, remap_ribbon_x, remap_ribbon_y, cv2.INTER_LINEAR)

    # 5. Bảo vệ tuyệt đối Vệ tinh băng, Ngôi sao mẹ và Lục địa
    scale = 1376.0 / 2560.0
    mcx, mcy, mr = 352.3 * scale, 1317.3 * scale, 916.7 * scale
    dist_m = np.hypot(x_grid - mcx, y_grid - mcy)
    moon_body = dist_m < mr + 10.0

    star_cx, star_cy = 1098.0, 212.0
    dist_star = np.hypot(x_grid - star_cx, y_grid - star_cy)
    star_body = dist_star < 80.0

    atmo_zone = (dist_d >= -15) & (dist_d <= 70) & (~moon_body) & (~star_body) & (x_grid > 250) & (img[:, :, 0] > 45)

    blend_weight = np.clip(1.0 - np.abs(dist_d - 25.0) / 45.0, 0.0, 1.0)
    blend_weight = (np.sin(blend_weight * np.pi / 2.0) ** 2) * atmo_zone.astype(np.float32)
    blend_weight = cv2.GaussianBlur(blend_weight.astype(np.float32), (15, 15), 4.0)[:, :, None]

    final_cleaned = (img.astype(np.float32) * (1.0 - blend_weight) + atmo_restored.astype(np.float32) * blend_weight)
    final_cleaned = np.clip(final_cleaned, 0, 255).astype(np.uint8)

    # Lưu đè file texture sạch
    cv2.imwrite(src_path, final_cleaned)
    cv2.imwrite(dest_path, final_cleaned)
    print(f"Đã lưu thành công texture sạch vào: {src_path} và {dest_path}", flush=True)

if __name__ == '__main__':
    clean_stage1_stripes()
