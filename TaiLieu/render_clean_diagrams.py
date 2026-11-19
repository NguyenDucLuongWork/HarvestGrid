import os
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as patches

OUT_DIR = r"c:\_Luong\Studying\GameDev\HarvestGrid_Project\TaiLieu\Diagrams_Rendered"
os.makedirs(OUT_DIR, exist_ok=True)

def setup_fig(w=12, h=8, dpi=250):
    fig, ax = plt.subplots(figsize=(w, h), dpi=dpi)
    ax.set_xlim(0, 100)
    ax.set_ylim(0, 100)
    ax.axis('off')
    return fig, ax

def draw_box(ax, x, y, w, h, title="", subtitle="", color="#E3F2FD", border="#1565C0", lw=1.5, rx=1.5, title_color="#0D47A1", sub_color="#37474F", title_size=9.5, sub_size=8):
    p = patches.FancyBboxPatch((x, y), w, h, boxstyle=f"round,pad={rx}", fc=color, ec=border, lw=lw, zorder=2)
    ax.add_patch(p)
    if title and subtitle:
        ax.text(x + w/2, y + h*0.68, title, ha='center', va='center', fontsize=title_size, fontweight='bold', color=title_color, zorder=3)
        ax.text(x + w/2, y + h*0.32, subtitle, ha='center', va='center', fontsize=sub_size, color=sub_color, zorder=3)
    elif title:
        ax.text(x + w/2, y + h/2, title, ha='center', va='center', fontsize=title_size, fontweight='bold', color=title_color, zorder=3)

def draw_arrow(ax, x1, y1, x2, y2, label="", color="#37474F", lw=1.5, ls='-', label_offset=(0, 2), label_color=None, label_size=7.5, rad=0.0):
    if label_color is None:
        label_color = color
    connectionstyle = f"arc3,rad={rad}" if rad != 0.0 else "arc3,rad=0"
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=lw, linestyle=ls, 
                                mutation_scale=14, connectionstyle=connectionstyle),
                zorder=4)
    if label:
        # compute label pos
        mx = (x1 + x2) / 2 + label_offset[0]
        my = (y1 + y2) / 2 + label_offset[1]
        ax.text(mx, my, label, ha='center', va='center', fontsize=label_size, color=label_color, fontweight='bold',
                bbox=dict(boxstyle="round,pad=0.25", fc="white", ec=color, lw=0.8, alpha=0.92),
                zorder=5)

# ==============================================================================
# 1. AUTO-FARMING RUNTIME LOOP (Fixing media_1795027489427_532315ca.jpg)
# ==============================================================================
def render_autofarming_loop():
    fig, ax = setup_fig(12.5, 8.2)
    ax.set_title("Luồng Hoạt Động Canh Tác Tự Động (Auto-Farming Runtime Loop)", 
                 fontsize=14, fontweight='bold', pad=18, color="#1B5E20")
    
    # 6 boxes arranged in a clean, elegant circular pipeline (Clockwise flow)
    # Row 1 (Top): Steps 1, 2, 3
    # Row 2 (Bottom): Steps 6, 5, 4
    
    # Box 1: Top-Left (Setup)
    draw_box(ax, 4, 56, 26, 32, 
             "1. Sắp Xếp Kho Đồ (Player Setup)", 
             "Người chơi sắp xếp các vật phẩm:\n• Hạt giống (Seed: AddPlantUse)\n• Nước/Phân/Đèn (ProvideResourceUse)\n• Lưỡi hái (HarvestUse)\n• Kích hoạt hiệu ứng định vị trên lưới", 
             color="#E8F5E9", border="#2E7D32", lw=1.8, title_color="#1B5E20")
             
    # Box 2: Top-Middle (Timer Loop)
    draw_box(ax, 37, 56, 26, 32, 
             "2. Đếm Nhịp Hoạt Động (ProgressTimer)", 
             "Mỗi vật phẩm sở hữu ProgressTimer:\n• Chạy liên tục (duration, loop = true)\n• Cập nhật thanh tiến trình trên UI\n• Khi đạt 100% -> Tự động kích hoạt\n  ItemUse.Apply(context)", 
             color="#E1F5FE", border="#0288D1", lw=1.8, title_color="#01579B")
             
    # Box 3: Top-Right (Targeting & Apply)
    draw_box(ax, 70, 56, 26, 32, 
             "3. Tác Động Nông Trại (Execution)", 
             "Hệ thống tự động tìm slot mục tiêu:\n• AddPlant: tìm slot đất còn trống\n• ProvideResource: tìm cây cần tài nguyên\n• Harvest: tìm cây đã chín (IsFullyGrown)\n-> Kích hoạt phương thức tương ứng", 
             color="#FFF8E1", border="#F57F17", lw=1.8, title_color="#E65100")
             
    # Box 4: Bottom-Right (Plant Growth)
    draw_box(ax, 70, 10, 26, 34, 
             "4. Sinh Trưởng Cây (Plant Growth)", 
             "• Cây nhận tài nguyên: Plant.Absorb()\n• Đáp ứng nextStageRequirement\n• Chuyển giai đoạn: GrowToNextStage()\n• Cập nhật sprite hiển thị từng nấc\n  (PlantStage: Mầm -> Nhỏ -> Lớn -> Chín)", 
             color="#E0F2F1", border="#00796B", lw=1.8, title_color="#004D40")
             
    # Box 5: Bottom-Middle (Auto-Harvest)
    draw_box(ax, 37, 10, 26, 34, 
             "5. Tự Động Thu Hoạch (Auto-Harvest)", 
             "• HarvestUse kích hoạt trên cây chín\n• Tính sản lượng = BaseCount * Effective\n• Đánh giá sao phẩm cấp (1 đến 5 sao)\n• Tự động đưa nông sản vào kho:\n  Inventory.AddCrop(crop, count)\n• Dọn sạch slot để sẵn sàng chu kỳ mới", 
             color="#FCE4EC", border="#C2185B", lw=1.8, title_color="#880E4F")

    # Box 6: Bottom-Left (Progression & Win)
    draw_box(ax, 4, 10, 26, 34, 
             "6. Hoàn Thành Mục Tiêu (Level Goal)", 
             "• Đối chiếu nông sản với LevelSO\n• Khi đạt đủ chỉ tiêu: Kích hoạt Thắng màn\n• Cộng tiền thưởng & lưu điểm số\n• Đồng bộ điểm kỷ lục lên PlayFab Leaderboard\n• Người chơi tiếp tục tái đầu tư/nâng cấp", 
             color="#F3E5F5", border="#7B1FA2", lw=1.8, title_color="#4A148C")

    # Arrows in CLOCKWISE loop - NO DIAGONAL OVERLAYS!
    # 1 -> 2
    draw_arrow(ax, 30, 72, 37, 72, "Kích hoạt Item", color="#2E7D32", lw=2, label_offset=(0, 2.8))
    # 2 -> 3
    draw_arrow(ax, 63, 72, 70, 72, "ItemUse.Apply()", color="#0288D1", lw=2, label_offset=(0, 2.8))
    # 3 -> 4 (Down)
    draw_arrow(ax, 83, 56, 83, 44, "Cung cấp tài nguyên / Gieo hạt", color="#E65100", lw=2, label_offset=(0, 0))
    # 4 -> 5 (Left)
    draw_arrow(ax, 70, 27, 63, 27, "Cây chín (IsFullyGrown)", color="#00796B", lw=2, label_offset=(0, 2.8))
    # 5 -> 6 (Left)
    draw_arrow(ax, 37, 27, 30, 27, "Tích lũy Nông sản", color="#C2185B", lw=2, label_offset=(0, 2.8))
    # 6 -> 1 (Up - Feedback to Player Setup)
    draw_arrow(ax, 17, 44, 17, 56, "Tái sắp xếp & Nâng cấp", color="#7B1FA2", lw=2, label_offset=(0, 0))

    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_autofarming_loop.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered autofarming loop:", p_out)

# ==============================================================================
# 2. AI DECISION PIPELINE (Fixing media_1795027508211_8a6df3d1.jpg)
# ==============================================================================
def render_ai_pipeline():
    fig, ax = setup_fig(12.5, 8.5)
    ax.set_title("Luồng Xử Lý Trí Tuệ Nhân Tạo (AI Decision & Companion Pipeline)", 
                 fontsize=14, fontweight='bold', pad=18, color="#311B92")
    
    # Top Row: 4 sequential pipeline stages (y: 57..90, h: 33)
    w_top = 21.5
    gap_top = 3.0
    x0 = 3.5
    
    # 1. State Ingestion
    draw_box(ax, x0, 56, w_top, 33, 
             "1. Thu Thập Dữ Liệu\n(State Ingestion)", 
             "Từ Gameplay Runtime:\n• Holdings (vật phẩm sở hữu)\n• StoringSpace (lưới ô trống)\n• Farm Slots & Cây trồng\n• Nhu cầu tài nguyên\n• Mục tiêu màn (LevelSO)\n• Vật phẩm Shop hiện có", 
             color="#EDE7F6", border="#4A148C", lw=1.6, title_color="#311B92")
             
    # 2. Portfolio Valuation
    x1 = x0 + w_top + gap_top
    draw_box(ax, x1, 56, w_top, 33, 
             "2. Đánh Giá Danh Mục\n(Portfolio Valuation)", 
             "Heuristic Evaluation:\n• Baseline Value hiện tại\n• Resource Coverage (Độ phủ)\n• Slot Factor (Tận dụng ô đất)\n• WinRate ước tính\n• Giá trị sản lượng kỳ vọng", 
             color="#EDE7F6", border="#4A148C", lw=1.6, title_color="#311B92")
             
    # 3. Repack Simulation
    x2 = x1 + w_top + gap_top
    draw_box(ax, x2, 56, w_top, 33, 
             "3. Mô Phỏng Đóng Gói\n(Repack Simulation)", 
             "Greedy Best-Fit Decreasing:\n• Thử đặt item vào lưới kho\n• Thử xoay 4 hướng (0, 90..)\n• Mô phỏng tái sắp xếp\n• Đánh giá mức độ di chuyển\n• Kiểm tra hợp lệ không va chạm", 
             color="#EDE7F6", border="#4A148C", lw=1.6, title_color="#311B92")
             
    # 4. Scenario Selection
    x3 = x2 + w_top + gap_top
    draw_box(ax, x3, 56, w_top, 33, 
             "4. Quyết Định Hành Động\n(Scenario Selection)", 
             "So sánh Score kịch bản:\n• BuyItem (Mua ngay)\n• SellThenBuy (Bán rồi mua)\n• ReorganizeThenBuy (Sắp lại)\n• RollShop (Reroll tìm item)\n• Wait (Giữ tiền tích lũy)", 
             color="#EDE7F6", border="#4A148C", lw=1.6, title_color="#311B92")
             
    # Horizontal Pipeline Arrows
    draw_arrow(ax, x0 + w_top, 72.5, x1, 72.5, "State Snapshot", color="#4A148C", lw=1.8, label_offset=(0, 2.5), label_size=7)
    draw_arrow(ax, x1 + w_top, 72.5, x2, 72.5, "Candidate Items", color="#4A148C", lw=1.8, label_offset=(0, 2.5), label_size=7)
    draw_arrow(ax, x2 + w_top, 72.5, x3, 72.5, "Repack Plans", color="#4A148C", lw=1.8, label_offset=(0, 2.5), label_size=7)

    # Bottom Row: 2 Distinct Output Targets with WIDE horizontal separation
    # y: 12..44 (h: 32)
    # Box 5: AI In-Game Companion (x: 10..48, w: 38)
    draw_box(ax, 10, 12, 38, 33, 
             "5. AI In-Game Companion (Local LLM)", 
             "Chuyển đổi ngôn ngữ tự nhiên:\n• Nhận chuỗi kịch bản tối ưu từ AI Engine\n• Gửi prompt tới LLMUnity (GGUF Model chạy cục bộ)\n• Viết lại với giọng điệu thân thiện, hóm hỉnh\n• Hiển thị bong bóng thoại hướng dẫn đồng hành", 
             color="#E8EAF6", border="#283593", lw=1.8, title_color="#1A237E")
             
    # Box 6: Visual Ghost Overlay (x: 52..90, w: 38)
    draw_box(ax, 52, 12, 38, 33, 
             "6. Gợi Ý Vị Trí Đặt Vật Phẩm (Ghost Placement)", 
             "GoalItemSuggestionManager:\n• Phân tích nông sản còn thiếu của màn chơi\n• Tìm hạt giống / công cụ phù hợp (First-Fit)\n• Chiếu hình mờ gợi ý (Ghost Placement Overlay)\n  trên ô kho đồ, định hướng người chơi mua & đặt", 
             color="#FFFDE7", border="#FBC02D", lw=1.8, title_color="#F57F17")

    # Connect Box 4 to Box 5 and Box 6 with CLEAN, SEPARATED, NON-OVERLAPPING PATHS
    # Output 1 to Companion: Starts from bottom-left of Box 4, curves smoothly to Box 5 top
    draw_arrow(ax, x3 + 5, 56, 29, 45, "Kịch Bản Tối Ưu (Optimal Scenario)", color="#283593", lw=2, label_offset=(-2, 2.5), label_size=7.5, rad=-0.15)
    
    # Output 2 to Ghost: Starts from bottom-right of Box 4, goes cleanly to Box 6 top
    draw_arrow(ax, x3 + w_top - 5, 56, 71, 45, "Gợi Ý Vật Phẩm Thiếu (Missing Items)", color="#F57F17", lw=2, label_offset=(3, 2.5), label_size=7.5, rad=0.15)

    # Bottom Note Box
    ax.text(50, 4.5, "[LƯU Ý NGHIÊN CỨU]: Cơ chế Dynamic Difficulty Adjustment (DDA) thuộc định hướng thiết kế & mở rộng (Future Work), chưa tích hợp runtime.", 
            ha='center', fontsize=8.5, fontstyle='italic', color="#C62828", 
            bbox=dict(boxstyle="round,pad=0.4", fc="#FFEBEE", ec="#C62828", lw=1))

    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_ai_pipeline.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered AI pipeline:", p_out)

# ==============================================================================
# 3. PHYSICAL DEPLOYMENT ARCHITECTURE (Fixing media_1795027533436_185710a7.jpg)
# ==============================================================================
def render_deployment_arch():
    fig, ax = setup_fig(13.0, 8.5)
    ax.set_title("Kiến Trúc Triển Khai Vật Lý (Physical Deployment Architecture)", 
                 fontsize=14, fontweight='bold', pad=18, color="#1A237E")
    
    # 3 Separate Columns with generous gaps:
    # 1. Player (Left): x: 2..16 (w: 14)
    # 2. Android Device (Middle): x: 25..72 (w: 47)
    # 3. PlayFab Cloud (Right): x: 81..98 (w: 17)
    
    # Actor / Player Box
    draw_box(ax, 2, 35, 15, 30, 
             "Người Chơi\n(Player)", 
             "Mobile Touch Device\n• Thao tác chạm (Tap)\n• Kéo thả (Drag & Drop)\n• Phản hồi thị giác thời gian thực", 
             color="#E1F5FE", border="#0288D1", lw=1.8, title_color="#01579B")
             
    # Android Device Container
    android_box = patches.FancyBboxPatch((23, 6), 49, 88, boxstyle="round,pad=1.8", fc="#F1F8E9", ec="#33691E", lw=2.2, zorder=1)
    ax.add_patch(android_box)
    ax.text(47.5, 90.5, "Thiết Bị Người Dùng (Android Device Runtime)", ha='center', fontsize=11.5, fontweight='bold', color="#1B5E20")
    
    # Inside Android Container: Clean vertical stack
    # UI Layer
    draw_box(ax, 25, 74, 45, 12, 
             "Giao Diện Người Dùng (UI Layer)", 
             "UI Toolkit & uGUI Panels: Render Scene, Touch Events, HUD", 
             color="white", border="#558B2F", lw=1.2, title_color="#2E7D32")
             
    # Game Logic Layer
    draw_box(ax, 25, 56, 45, 15, 
             "Unity Game Engine Runtime (Local C#)", 
             "Game Logic, Managers (FarmMono, InventoryMono, Shop)\nAuto-Farming Timers & Resource Absorption Logic\nInventorySortService (Greedy Best-Fit Decreasing)", 
             color="white", border="#558B2F", lw=1.2, title_color="#2E7D32")
             
    # Local AI Engine
    draw_box(ax, 25, 38, 45, 15, 
             "Local AI Engine & Companion", 
             "AIHelper (Portfolio Valuation & Scenario Analysis)\nLocal LLM Inference Engine (LLMUnity GGUF)\nGoalItemSuggestionManager (Ghost Overlays)", 
             color="white", border="#558B2F", lw=1.2, title_color="#2E7D32")
             
    # Storage row inside Android: Static vs Local Save
    draw_box(ax, 25, 10, 21.5, 24, 
             "Static Data\n(SO Assets)", 
             "Đóng gói trong APK/AAB:\n• ItemPrototype / SO\n• PlantPrototype / SO\n• LevelSO (Quotas, Pool)\n• StoringSpaceSO", 
             color="white", border="#558B2F", lw=1.2, title_color="#2E7D32")
             
    draw_box(ax, 48.5, 10, 21.5, 24, 
             "Local Storage\n(JSON Save)", 
             "Application.persistentDataPath:\n• UserData.json\n• GameData.json\n• Settings.json\n(Guest Mode Persistence)", 
             color="white", border="#558B2F", lw=1.2, title_color="#2E7D32")

    # PlayFab Cloud Container
    cloud_box = patches.FancyBboxPatch((80, 18), 18, 64, boxstyle="round,pad=1.8", fc="#EDE7F6", ec="#4A148C", lw=2.2, zorder=1)
    ax.add_patch(cloud_box)
    ax.text(89, 78.5, "PlayFab Cloud\n(Microsoft Azure)", ha='center', fontsize=11, fontweight='bold', color="#4A148C")
    
    draw_box(ax, 82, 58, 14, 15, 
             "PlayFab Auth", 
             "Login / Register\nUsername / Password\nDevice ID Link", 
             color="white", border="#7B1FA2", lw=1.2, title_color="#4A148C", title_size=9, sub_size=7.5)
             
    draw_box(ax, 82, 40, 14, 15, 
             "Leaderboard", 
             "Global Ranking\nScore & Time Record\nLevel Completion", 
             color="white", border="#7B1FA2", lw=1.2, title_color="#4A148C", title_size=9, sub_size=7.5)
             
    draw_box(ax, 82, 22, 14, 15, 
             "Player Title Data", 
             "Cloud Profile Sync\nSlots & Coins Backup\nRemote JSON Save", 
             color="white", border="#7B1FA2", lw=1.2, title_color="#4A148C", title_size=9, sub_size=7.5)

    # Connections with AMPLE GAP - NO TEXT OVERLAPPING BOX BORDERS!
    # 1. Player to UI Layer:
    draw_arrow(ax, 17, 50, 25, 76, "Touch Events\n(Gesture/Drag)", color="#0288D1", lw=2, label_offset=(-0.5, 3.5), label_size=7.5, rad=-0.1)
    # UI feedback to Player:
    draw_arrow(ax, 25, 73, 17, 47, "Visual Feedback", color="#0288D1", lw=1.5, ls="--", label_offset=(0, -2.5), label_size=7.5, rad=-0.1)

    # 2. Android to PlayFab: Wide 8-unit gap (x: 72..80)
    # Top arrow: Auth
    draw_arrow(ax, 70, 65.5, 82, 65.5, "Auth & Token", color="#6A1B9A", lw=1.8, label_offset=(0, 2.5), label_size=7.5)
    # Middle arrow: Leaderboard
    draw_arrow(ax, 70, 47.5, 82, 47.5, "Submit Score", color="#6A1B9A", lw=1.8, label_offset=(0, 2.5), label_size=7.5)
    # Bottom arrow: Cloud Save
    draw_arrow(ax, 70, 29.5, 82, 29.5, "Cloud Sync", color="#6A1B9A", lw=1.8, label_offset=(0, 2.5), label_size=7.5)

    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_deployment_arch.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered deployment arch:", p_out)

# ==============================================================================
# 4. LOGICAL ARCHITECTURE (Clean layer spacing)
# ==============================================================================
def render_logical_arch():
    fig, ax = setup_fig(12.5, 8.5)
    ax.set_title("Kiến Trúc Logic Hệ Thống Harvest Grid (Logical Layered Architecture)", 
                 fontsize=14, fontweight='bold', pad=18, color="#004D40")
    
    layers = [
        ("TẦNG TRÌNH DIỄN (Presentation / UI Layer)", 76, 18, "#E0F2F1", "#00695C", [
            (5, 78, 28, 11, "UI Toolkit / uGUI Panels", "FarmPanel, InventoryUI, ShopUI, SettingPanel"),
            (36, 78, 28, 11, "Visual & Animation Mono", "ItemMono, FarmSlotMono, PlantMono"),
            (67, 78, 28, 11, "AI In-Game Companion UI", "AIMessage Popup, Ghost Suggestion Overlay")
        ]),
        ("TẦNG ĐIỀU PHỐI & HỆ THỐNG (Game Systems & Managers Layer)", 48, 24, "#E8F5E9", "#2E7D32", [
            (5, 59, 21, 9.5, "Game / Level Manager", "GameManager, LevelsManager"),
            (28, 59, 21, 9.5, "Inventory Manager", "InventoryMono, ItemManager"),
            (51, 59, 21, 9.5, "Farm System", "FarmMono, Plant Growth Loop"),
            (74, 59, 21, 9.5, "Shop & Economy", "ShopPanel, CosmeticManager"),
            (5, 49.5, 28, 8.5, "Auth & User Manager", "AuthManager, UserManager"),
            (36, 49.5, 28, 8.5, "AI Decision Engine", "GoalItemSuggestionManager, AIHelper"),
            (67, 49.5, 28, 8.5, "Packing & Search Service", "InventorySortService (Greedy BFD)")
        ]),
        ("TẦNG MÔ HÌNH THỰC THỂ (Domain / Entity Layer)", 26, 18, "#FFF8E1", "#F57F17", [
            (5, 27.5, 21, 12, "Farm Domain", "Farm, FarmSlot, Plant,\nPlantStage, Crop"),
            (28, 27.5, 21, 12, "Inventory Domain", "Inventory, StoringSpace,\nFootprint, StoredObject"),
            (51, 27.5, 21, 12, "Item & Activation", "Item, ItemUse (AddPlant,\nProvideResource, Harvest)"),
            (74, 27.5, 21, 12, "User & Metric Data", "UserData, LeaderboardData,\nPlayTime, ProgressTimer")
        ]),
        ("TẦNG TRUY XUẤT DỮ LIỆU & DỊCH VỤ NGOÀI (Data Access & Service Layer)", 4, 18, "#F3E5F5", "#6A1B9A", [
            (5, 5.5, 28, 11.5, "Static Assets (SO)", "ItemPrototype, PlantPrototype,\nLevelSO, StoringSpaceSO"),
            (36, 5.5, 28, 11.5, "Local Persistence", "DataPersistenceManager,\nJSON Serialization (Local Save)"),
            (67, 5.5, 28, 11.5, "Cloud Service Clients", "PlayFabAuthService, TitleData,\nLeaderboard Integration")
        ])
    ]
    
    for title, y, h, bg, border, boxes in layers:
        layer_box = patches.FancyBboxPatch((2, y), 96, h, boxstyle="round,pad=1.5", fc=bg, ec=border, lw=1.6, zorder=1)
        ax.add_patch(layer_box)
        ax.text(4, y + h - 2.5, title, fontsize=9.5, fontweight='bold', color=border, zorder=2)
        for bx, by, bw, bh, bt, bs in boxes:
            draw_box(ax, bx, by, bw, bh, bt, bs, color="white", border=border, lw=1, title_color=border, title_size=9, sub_size=7.5)
            
    # Connect Layers with clear offset arrows
    draw_arrow(ax, 30, 76, 30, 72, "User Events", color="#00695C", lw=1.8, label_offset=(0, 2))
    draw_arrow(ax, 70, 72, 70, 76, "State Updates", color="#00695C", lw=1.8, label_offset=(0, 2))
    
    draw_arrow(ax, 30, 48, 30, 44, "Manipulate & Query", color="#2E7D32", lw=1.8, label_offset=(0, 2))
    draw_arrow(ax, 70, 44, 70, 48, "Domain Events", color="#2E7D32", lw=1.8, label_offset=(0, 2))
    
    draw_arrow(ax, 30, 26, 30, 22, "Read SO Config", color="#F57F17", lw=1.8, label_offset=(0, 2))
    draw_arrow(ax, 70, 26, 70, 22, "Save & Sync Cloud", color="#6A1B9A", lw=1.8, label_offset=(0, 2))
    
    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_logical_arch.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered logical arch:", p_out)

# ==============================================================================
# 5. DATA ARCHITECTURE OVERVIEW (Clean non-overlapping routing)
# ==============================================================================
def render_data_arch():
    fig, ax = setup_fig(12.5, 8.5)
    ax.set_title("Kiến Trúc Dữ Liệu Tổng Thể (Data Architecture Overview)", 
                 fontsize=14, fontweight='bold', pad=18, color="#BF360C")
    
    # Static Data (Top-Left)
    draw_box(ax, 3, 54, 28, 36, 
             "DỮ LIỆU TĨNH (Static Data)", 
             "ScriptableObject đóng gói cùng ứng dụng:\n\n• ItemPrototype: cấu hình vật phẩm, footprint\n• PlantPrototype: cấu hình cây, các stage\n• CropSO: thuộc tính nông sản, giá bán, sao\n• LevelSO: mục tiêu màn, item pool, tiền khởi tạo\n• StoringSpaceSO: kích thước lưới kho đồ", 
             color="#FFF3E0", border="#E65100", lw=1.8, title_color="#BF360C")
             
    # Dynamic Data (Bottom-Left)
    draw_box(ax, 3, 8, 28, 38, 
             "DỮ LIỆU ĐỘNG (Dynamic Data)", 
             "Trạng thái người chơi (JSON / Runtime):\n\n• UserData: coin, slotsLevels, cosmetics\n• GameData: farm slots state, items on grid\n• Settings: âm thanh, cấu hình đồ họa\n• FinishedLevels: thời gian hoàn thành màn\n• Telemetry: thông số phục vụ AI gợi ý", 
             color="#FBE9E7", border="#D84315", lw=1.8, title_color="#BF360C")
             
    # Runtime Systems (Middle)
    draw_box(ax, 37, 20, 28, 62, 
             "RUNTIME GAMEPLAY SYSTEMS", 
             "Khởi Tạo & Vận Hành Trong RAM:\n\n1. Đọc Static Data để cấu hình đối tượng\n2. Nạp Dynamic Data để khôi phục trạng thái\n3. Điều phối Auto-Farming & Inventory Grid\n4. Kích hoạt ItemUse tác động lên nông trại\n5. Cung cấp feature state cho AI Engine\n6. Ghi nhận thay đổi để định kỳ lưu trữ\n7. Đồng bộ đám mây khi hoàn thành màn", 
             color="#EFEBE9", border="#4E342E", lw=2.0, title_color="#3E2723")
             
    # AI Engine (Top-Right)
    draw_box(ax, 71, 54, 26, 36, 
             "HỆ THỐNG AI ENGINE", 
             "Thu Thập State Từ Runtime:\n• Nhu cầu nông trại (FarmDemand)\n• Trạng thái kho (Holdings)\n• Mẫu vật phẩm cửa hàng (ShopPool)\n\n-> Tính toán Portfolio & Repack\n-> GoalItemSuggestionManager\n   Gợi ý ô đặt (Ghost Placement Overlay)", 
             color="#EDE7F6", border="#512DA8", lw=1.8, title_color="#311B92")
             
    # Persistence (Bottom-Right)
    draw_box(ax, 71, 8, 26, 38, 
             "TẦNG LƯU TRỮ (Persistence)", 
             "Lưu Dữ Liệu An Toàn:\n\n[Guest Mode]:\n• Lưu file JSON cục bộ vào\n  Application.persistentDataPath\n\n[PlayFab Cloud Mode]:\n• Đồng bộ tài khoản đám mây\n• Cập nhật BXH toàn cầu\n• Lưu trữ Title Data dự phòng", 
             color="#E8EAF6", border="#283593", lw=1.8, title_color="#1A237E")
             
    # Clean orthogonal connections with wide offsets
    draw_arrow(ax, 31, 72, 37, 72, "Templates Config", color="#E65100", lw=2, label_offset=(0, 2.5))
    draw_arrow(ax, 31, 27, 37, 27, "Restore State", color="#D84315", lw=2, label_offset=(0, 2.5))
    draw_arrow(ax, 37, 21, 31, 21, "Serialize Save", color="#4E342E", lw=1.6, ls="--", label_offset=(0, -2.5))
    
    draw_arrow(ax, 65, 72, 71, 72, "Feature State", color="#512DA8", lw=2, label_offset=(0, 2.5))
    draw_arrow(ax, 71, 62, 65, 62, "Action Suggestion", color="#512DA8", lw=1.6, ls="--", label_offset=(0, -2.5))
    
    draw_arrow(ax, 65, 27, 71, 27, "Sync & Save", color="#283593", lw=2, label_offset=(0, 2.5))
    
    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_data_arch.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered data arch:", p_out)

# ==============================================================================
# 6. SCRIPTABLE OBJECT MODEL
# ==============================================================================
def render_scriptable_objects():
    fig, ax = setup_fig(12.5, 8.5)
    ax.set_title("Mô Hình Cấu Trúc ScriptableObject (Static Data Configuration)", 
                 fontsize=14, fontweight='bold', pad=18, color="#1565C0")
    
    # Tree layout
    # Center root: LevelSO (x: 37..63, y: 72..90)
    draw_box(ax, 37, 72, 26, 18, 
             "LevelSO", 
             "requiringCrops: Dict<Crop, int>\nitemAndChancePool: Dict<Item, float>\nstartingMoney: int\ntimeBudget: float, maxSlotCount: int", 
             color="#E3F2FD", border="#0D47A1", lw=2.0, title_color="#0D47A1")
             
    # Left Branch: Items & Footprints
    draw_box(ax, 5, 45, 26, 18, 
             "ItemPrototype / ItemSO", 
             "item: Item\nitemFootprintSO: ItemFootprintSO\nrarity: ItemRarity\nprice: int, role: ItemRole", 
             color="#E8F5E9", border="#1B5E20", lw=1.8, title_color="#1B5E20")
             
    draw_box(ax, 5, 12, 26, 18, 
             "ItemFootprintSO", 
             "width: int, height: int\nrequiringFlat: bool[]\nmatrix: bool[,]\nRotate(): void", 
             color="#E8F5E9", border="#1B5E20", lw=1.8, title_color="#1B5E20")
             
    # Right Branch: Plants & Crops
    draw_box(ax, 69, 45, 26, 18, 
             "PlantPrototype / PlantSO", 
             "plant: Plant, plantID: string\nplantName: string, averageCropCount: int\nstages: List<PlantStage>\ncrop: Crop", 
             color="#FFF3E0", border="#E65100", lw=1.8, title_color="#E65100")
             
    draw_box(ax, 69, 12, 26, 18, 
             "CropSO", 
             "crop: Crop, cropID: string\ncropName: string\nbaseSellPrice: int, rarity: ItemRarity", 
             color="#FFF3E0", border="#E65100", lw=1.8, title_color="#E65100")
             
    # Bottom Middle: StoringSpaceSO
    draw_box(ax, 37, 12, 26, 18, 
             "StoringSpaceSO", 
             "width: int (e.g. 5), height: int (e.g. 5)\ndefaultCellTypes: CellType[,]\nmaxCapacity: int", 
             color="#F3E5F5", border="#4A148C", lw=1.8, title_color="#4A148C")
    
    # Connections with clear routing
    draw_arrow(ax, 37, 78, 22, 63, "Item Pool", color="#0D47A1", lw=2, label_offset=(-2, 2.5), rad=0.1)
    draw_arrow(ax, 63, 78, 78, 63, "Goal Quotas", color="#0D47A1", lw=2, label_offset=(2, 2.5), rad=-0.1)
    draw_arrow(ax, 50, 72, 50, 30, "Grid Size Config", color="#4A148C", lw=1.8, label_offset=(0, 0))
    draw_arrow(ax, 18, 45, 18, 30, "Has Footprint", color="#1B5E20", lw=2, label_offset=(0, 0))
    draw_arrow(ax, 82, 45, 82, 30, "Produces Crop", color="#E65100", lw=2, label_offset=(0, 0))
    
    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_scriptable_objects.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered scriptable objects:", p_out)

# ==============================================================================
# 7. USE CASE DIAGRAM (Clean spacing)
# ==============================================================================
def render_usecase():
    fig, ax = setup_fig(12.5, 8.5)
    ax.set_title("Sơ đồ Use Case Tổng Thể Hệ Thống Harvest Grid (V7)", 
                 fontsize=14, fontweight='bold', pad=18, color="#1A237E")
    
    # System Boundary Box
    system_box = patches.FancyBboxPatch((24, 4), 73, 90, boxstyle="round,pad=2", fc="#FAFAFA", ec="#37474F", lw=2.2, linestyle='--', zorder=1)
    ax.add_patch(system_box)
    ax.text(60.5, 91.5, "Harvest Grid System Boundary", ha='center', fontsize=12, fontweight='bold', color="#263238")
    
    # Actor Player (Left)
    ax.plot([10], [52], 'o', ms=16, color="#0288D1", zorder=3)
    ax.plot([10, 10], [49, 41], color="#0288D1", lw=3.5, zorder=3)
    ax.plot([5, 15], [45, 45], color="#0288D1", lw=3.5, zorder=3)
    ax.plot([10, 6], [41, 33], color="#0288D1", lw=3.5, zorder=3)
    ax.plot([10, 14], [41, 33], color="#0288D1", lw=3.5, zorder=3)
    ax.text(10, 28, "Người Chơi\n(Player)", ha='center', fontsize=10.5, fontweight='bold', color="#01579B")
    
    # 10 well-spaced use case boxes in two neat columns
    # Col 1: x=29, w=30
    # Col 2: x=64, w=30
    col1_x = 28
    col2_x = 64
    w_uc = 30
    h_uc = 7.5
    
    draw_box(ax, col1_x, 80, w_uc, h_uc, "Đăng Nhập / Đăng Ký", "PlayFab Auth / Guest Mode", color="#E8EAF6", border="#3949AB")
    draw_box(ax, col2_x, 80, w_uc, h_uc, "Quản Lý Hồ Sơ & BXH", "Profile & Leaderboard", color="#E8EAF6", border="#3949AB")
    
    draw_box(ax, col1_x, 66, w_uc, h_uc, "Quản Lý Kho Đồ", "Inventory Grid / Polyomino Placement", color="#E8EAF6", border="#3949AB")
    draw_box(ax, col2_x, 66, w_uc, h_uc, "Sắp Xếp Kho Tự Động", "Greedy Best-Fit Sorting", color="#E8EAF6", border="#3949AB")
    
    draw_box(ax, col1_x, 52, w_uc, h_uc, "Quản Lý Nông Trại", "Xem Farm, Mua & Nâng Cấp Slot", color="#E8EAF6", border="#3949AB")
    draw_box(ax, col2_x, 52, w_uc, h_uc, "Canh Tác & Tự Động Hóa", "Auto-Growth & Auto-Harvest", color="#E8EAF6", border="#3949AB")
    
    draw_box(ax, col1_x, 38, w_uc, h_uc, "Cửa Hàng Vật Phẩm", "Mua Item, Reroll Cửa Hàng", color="#E8EAF6", border="#3949AB")
    draw_box(ax, col2_x, 38, w_uc, h_uc, "Gợi Ý Vật Phẩm AI", "GoalItemSuggestionManager (Ghost Placement)", color="#E8EAF6", border="#3949AB")
    
    draw_box(ax, col1_x, 24, w_uc, h_uc, "Chơi Màn & Hoàn Thành Goal", "Level Progression & Quotas", color="#E8EAF6", border="#3949AB")
    draw_box(ax, col2_x, 24, w_uc, h_uc, "Thiết Lập Hệ Thống", "Settings (Âm thanh/Đồ họa)", color="#E8EAF6", border="#3949AB")
    
    draw_box(ax, 46, 10, 32, h_uc, "Lưu & Khôi Phục Dữ Liệu", "Local JSON / PlayFab Cloud Sync", color="#E8EAF6", border="#3949AB")
    
    # Actor Connections
    for ty in [83.5, 69.5, 55.5, 41.5, 27.5]:
        draw_arrow(ax, 16, 45, col1_x, ty, color="#1565C0", lw=1.6)
        
    # Inter-usecase relations with clean horizontal lines
    draw_arrow(ax, col1_x + w_uc, 69.5, col2_x, 69.5, "<<include>>", color="#C2185B", ls=":", lw=1.5, label_offset=(0, 2.5))
    draw_arrow(ax, col1_x + w_uc, 55.5, col2_x, 55.5, "<<interact>>", color="#2E7D32", ls="--", lw=1.5, label_offset=(0, 2.5))
    draw_arrow(ax, col1_x + w_uc, 41.5, col2_x, 41.5, "<<extend>>", color="#C2185B", ls=":", lw=1.5, label_offset=(0, 2.5))

    plt.tight_layout()
    p_out = os.path.join(OUT_DIR, "diagram_usecase.png")
    fig.savefig(p_out, dpi=250)
    plt.close()
    print("Rendered usecase:", p_out)

if __name__ == '__main__':
    render_autofarming_loop()
    render_ai_pipeline()
    render_deployment_arch()
    render_logical_arch()
    render_data_arch()
    render_scriptable_objects()
    render_usecase()
    print("ALL 7 DIAGRAMS RERENDERED SUCCESSFULLY WITHOUT OVERLAYS!")
