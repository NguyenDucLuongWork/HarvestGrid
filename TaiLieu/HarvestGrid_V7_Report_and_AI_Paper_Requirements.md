# YÊU CẦU CẬP NHẬT BÁO CÁO ĐỒ ÁN VÀ VIẾT BÀI BÁO KHOA HỌC – HARVEST GRID

> **Mục đích:** Tài liệu này là brief giao việc cho AI/LLM thực hiện trên các tài liệu đã có trong thư mục dự án.  
> **Đầu ra cần tạo:** (1) Báo cáo đồ án phiên bản **V7** đã cập nhật; (2) một **bài báo khoa học bằng tiếng Anh** dựa trên nội dung, hệ thống và kết quả thực tế của Harvest Grid. Bái báo chuẩn IEEE 
> **Lưu ý:** Hiện tại chỉ sử dụng tài liệu này làm yêu cầu thực hiện. Không thay thế hoặc ghi đè các file nguồn V5/V3 và các tài liệu tham khảo hiện có.

---

## 1. Quy tắc làm việc với thư mục dự án

1. Trước khi chỉnh sửa, hãy kiểm tra toàn bộ thư mục làm việc để xác định:
   - Báo cáo đồ án hiện tại, ưu tiên file có tên/phiên bản **N138_v5_BAO_CAO_KLTN_2026_2027** làm bản nền để tạo V7.
   - Các phiên bản báo cáo cũ (V3, V2...) để đối chiếu nội dung đã từng mô tả chi tiết hơn, đặc biệt là kiến trúc dữ liệu, ScriptableObject, JSON, AI Decision Tree/DDA và các sơ đồ.
   - Các file bổ sung về UI Toolkit, kiến trúc, thiết kế, mã nguồn, hình ảnh giao diện, kết quả kiểm thử và tài liệu tham khảo.
   - Nếu có bản báo cáo hoặc tài liệu mới hơn V5, hãy xác minh ngày/phiên bản và chọn bản nền hợp lý; không tự ý bỏ qua nội dung mới.
2. Đọc nội dung thực tế của file trước khi viết. Không suy luận chỉ từ tên file, mục lục hoặc đoạn trích.
3. **V5 là bản nền dự kiến, không phải nguồn chân lý tuyệt đối.** Đối chiếu với các phiên bản cũ và bằng chứng hiện có trong project để phát hiện nội dung lỗi thời, mâu thuẫn hoặc chưa được triển khai.
4. Giữ lại cấu trúc, định dạng, cách đánh số chương/mục, văn phong học thuật và nội dung hợp lệ của báo cáo hiện tại. Chỉ tái cấu trúc khi thực sự cần thiết để sửa lỗi logic hoặc tăng tính nhất quán.
5. Không bịa đặt chức năng, class, dữ liệu, thuật toán, kết quả kiểm thử, số liệu, tài liệu tham khảo hoặc trạng thái triển khai. Phân biệt rõ:
   - Đã triển khai và có bằng chứng.
   - Đang triển khai/chưa hoàn thiện.
   - Mới là thiết kế hoặc định hướng.
6. Nếu tài liệu cũ mâu thuẫn với mã nguồn hoặc tài liệu mới hơn, ưu tiên bằng chứng hiện hành trong project; ghi nhận điểm cần xác minh thay vì tự chọn một giả định.
7. Giữ nguyên các thuật ngữ chuyên môn nhất quán (Inventory Grid, Auto-Farming, Plant, Farm, FarmSlot, Crop, Item, Level, ScriptableObject, Dynamic Data, Decision Tree, DDA, PlayFab...).
8. Không chỉ sửa câu chữ. Yêu cầu chính là **bổ sung mô hình, sơ đồ và luồng hoạt động**, giúp người đọc hiểu hệ thống mà không phải đọc các đoạn mô tả quá dài.
9. Tạo file mới cho V7 và bài báo khoa học. Không ghi đè các bản nguồn. Dùng tên file rõ ràng, ví dụ:
   - `N138_v7_BAO_CAO_KLTN_2026_2027.docx`
   - `Harvest_Grid_AI_Research_Paper.docx` hoặc `.md`/`.tex` tùy định dạng phù hợp với mẫu bài báo có sẵn.
10. Nếu có mẫu/template bài báo trong thư mục, phải ưu tiên dùng mẫu đó. Nếu không có, tạo bản thảo khoa học có cấu trúc chuẩn, dễ chuyển sang IEEE.

---

## 2. Đầu ra A – Cập nhật báo cáo đồ án lên V7

### 2.1. Mục tiêu cập nhật

Tạo bản **V7 hoàn chỉnh hơn V5**, xử lý các nhận xét của giảng viên/nhóm phản biện trong ảnh đính kèm và các điểm thiếu được phát hiện khi rà soát tài liệu. Báo cáo cần đồng bộ giữa:
- Chức năng thực tế của game và Use Case.
- Use Case và sơ đồ kiến trúc.
- Kiến trúc logic và kiến trúc triển khai vật lý.
- Mô hình dữ liệu, ScriptableObject, dữ liệu động và cơ chế lưu trữ.
- Mô tả AI, đầu vào/đầu ra, vị trí thực thi và cách AI tác động ngược lại gameplay.
- Phần mô tả, sơ đồ, mã nguồn minh họa và kết quả kiểm thử.

### 2.2. Các nhận xét bắt buộc phải xử lý

#### A. Chức năng nông trại chưa thể hiện rõ trong Use Case/UI

Nhận xét: Người xem không tìm thấy chức năng trồng cây, chăm sóc/nông trại hoặc thu hoạch.

Yêu cầu:
- Rà soát Use Case tổng thể và các Use Case chi tiết.
- Bổ sung/điều chỉnh các chức năng nông trại đúng theo những gì game thực sự hỗ trợ, có thể gồm:
  - Xem và quản lý nông trại.
  - Chọn/đặt hạt giống hoặc thực hiện thao tác trồng cây, nếu đã triển khai.
  - Theo dõi trạng thái và giai đoạn phát triển của cây.
  - Tác động lên cây bằng các vật phẩm/công cụ (ví dụ tưới nước, bón phân hoặc tác động tài nguyên) nếu có trong bản triển khai.
  - Hệ thống tự động sinh trưởng (Auto-Farming).
  - Thu hoạch và đưa nông sản vào kho đồ.
  - Mua hoặc nâng cấp Farm Slot nếu chức năng này tồn tại.
- Không thêm thao tác thủ công như trồng/tưới/thu hoạch nếu gameplay thực tế tự động xử lý hoặc chưa có chức năng đó. Cần mô tả đúng vòng lặp thực tế.
- Thể hiện được mối quan hệ giữa Inventory/Item Usage và Farm/Plant/Crop: vật phẩm hoặc cấu hình trang bị ảnh hưởng thế nào đến hoạt động nông trại và sản lượng.
- Đối chiếu Use Case với giao diện thật. Nếu đã có màn hình Farm Grid, cần dùng ảnh giao diện hoặc sơ đồ minh họa để chứng minh chức năng.

#### B. Use Case và sơ đồ kiến trúc không đồng bộ

Nhận xét: Có thành phần trong kiến trúc nhưng không xuất hiện trong Use Case, bao gồm Farm Management (Buy/Upgrade Slot), Settings, Profile, Shop AI; Auto-Farming cũng thiếu trong Use Case.

Yêu cầu:
- Lập bảng đối chiếu **Use Case ↔ Subsystem/Manager ↔ UI Screen ↔ Data**.
- Bổ sung các Use Case còn thiếu khi chúng thực sự thuộc phạm vi sản phẩm:
  - Farm Management: xem nông trại, mua/nâng cấp slot (nếu có).
  - Auto-Farming: xử lý sinh trưởng tự động và thu hoạch tự động.
  - Settings: xem/thay đổi thiết lập và lưu thiết lập.
  - Profile: xem/cập nhật hồ sơ người chơi theo phạm vi đã triển khai.
  - Shop: xem vật phẩm, mua vật phẩm, reroll (nếu có).
  - Shop AI: mô tả đúng vai trò AI trong lựa chọn/đề xuất hoặc sinh danh sách vật phẩm của Shop.
- Use Case phải thể hiện Actor phù hợp (Player, Admin hoặc hệ thống bên ngoài nếu có), ranh giới hệ thống và quan hệ include/extend khi cần.
- Không vẽ Use Case chỉ để khớp tên Manager. Chỉ đưa vào những chức năng người dùng/hệ thống thực sự thực hiện.
- Kiểm tra tất cả chức năng được nhắc trong báo cáo có xuất hiện hợp lý trong Use Case, sơ đồ kiến trúc và phần mô tả chi tiết.

#### C. Nhầm lẫn kiến trúc logic với kiến trúc triển khai vật lý

Nhận xét: Sơ đồ “Client/Users (Android)” đang bị vẽ thành một khối tách rời, nối mũi tên hai chiều với “Game System”, khiến người đọc hiểu nhầm Game System chạy trên server. Thực tế Game System, Static Data và Local Save chạy trên thiết bị Android; PlayFab mới là dịch vụ cloud.

Yêu cầu:
- Tách rõ hai loại sơ đồ:
  1. **Logical/System Architecture:** mô tả các module/layer và quan hệ phụ thuộc bên trong ứng dụng.
  2. **Physical/Deployment Architecture:** mô tả ứng dụng chạy ở đâu, dữ liệu nằm ở đâu và giao tiếp qua mạng với dịch vụ nào.
- Trong sơ đồ triển khai, thể hiện ranh giới rõ:
  - **Android Device:** Unity Game Application, UI, Game Systems/Managers, gameplay logic, AI runtime nếu thực tế chạy local, Static Data/ScriptableObject được đóng gói cùng ứng dụng, Local Save (JSON hoặc định dạng thực tế).
  - **Cloud:** PlayFab và các dịch vụ thực sự được tích hợp; chỉ thể hiện dữ liệu/tác vụ thực sự được gửi hoặc lưu trên cloud.
  - **Player:** tương tác với ứng dụng trên thiết bị, không phải một server hoặc một node ngang hàng với Game System.
- Thể hiện luồng đăng nhập/xác thực, tải/lưu/đồng bộ dữ liệu tài khoản giữa Android và PlayFab theo đúng triển khai.
- Phân biệt Guest/Local mode với tài khoản PlayFab:
  - Guest: dữ liệu người chơi được lưu local theo cơ chế thực tế.
  - Đăng nhập: dữ liệu nào được đồng bộ/lưu trên PlayFab phải được chỉ rõ.
- Không vẽ PlayFab như nơi chạy toàn bộ gameplay nếu gameplay không chạy trên server.
- Ghi chú rõ trường hợp offline, lỗi mạng hoặc đồng bộ (nếu có cơ chế xử lý thực tế). Nếu chưa có, nêu giới hạn thay vì tự thêm.

#### D. Mô hình dữ liệu và ScriptableObject còn quá dài dòng, thiếu hình minh họa

Nhận xét: Mục kiến trúc dữ liệu có mô tả nhưng quá dài và không có mô hình.

Yêu cầu:
- Viết lại phần kiến trúc dữ liệu theo hướng **có sơ đồ tổng quan + sơ đồ chi tiết + bảng mô tả**, hạn chế đoạn văn liệt kê kéo dài.
- Làm rõ sự khác nhau giữa:
  - **Static Data:** dữ liệu cấu hình do nhà phát triển thiết lập, tổ chức bằng ScriptableObject.
  - **Dynamic Data:** trạng thái và dữ liệu phát sinh trong quá trình chơi của từng người chơi, được serialize/lưu bằng JSON hoặc cơ chế thực tế.
- Bổ sung sơ đồ **Data UML/Class Diagram** thể hiện các nhóm dữ liệu, class/asset chính và quan hệ giữa chúng. Không nhầm sơ đồ dữ liệu với sơ đồ kiến trúc triển khai.
- Bổ sung sơ đồ riêng cho **ScriptableObject (SO)**: các asset cấu hình, thuộc tính quan trọng và quan hệ tham chiếu. Sơ đồ phải giúp người đọc thấy dữ liệu nào là nguồn cấu hình, dữ liệu nào được runtime system sử dụng.
- Có thể sử dụng UML Class Diagram, ERD hoặc Mermaid class diagram tùy mức độ phù hợp; ưu tiên sơ đồ dễ đọc, có tên lớp/field/quan hệ cụ thể.
- Không đưa toàn bộ field của mọi class vào một sơ đồ quá lớn. Chia thành các nhóm: Item/Inventory, Farm/Plant/Crop, Level/Progress, Player/Profile/Settings, Shop/AI nếu cần.
- Bổ sung bảng mô tả mỗi ScriptableObject: mục đích, dữ liệu chính, thành phần sử dụng.
- Bổ sung bảng mô tả các Dynamic Data/JSON: file hoặc nhóm dữ liệu, trường chính, nơi lưu, hệ thống đọc/ghi và mục đích.

**Các nhóm Static Data cần kiểm tra trong project và đối chiếu với báo cáo:**
- `PlantSO`: cấu hình cây trồng, loại cây và các giai đoạn phát triển.
- `ItemSO`: cấu hình vật phẩm, footprint/kích thước chiếm dụng và thuộc tính liên quan.
- `LevelSO`: cấu hình màn chơi, điều kiện/yêu cầu, vật phẩm có thể xuất hiện và thông tin khởi tạo màn.
- `CropSO`: cấu hình nông sản.
- Các asset cấu hình khác như Level Tree, Footprint, Cosmetic, AI Config... chỉ đưa vào nếu thực sự tồn tại trong phiên bản hiện hành. Đối chiếu tên class thực tế, không tự chuẩn hóa tên thành một class không có trong source.

**Các nhóm Dynamic Data cần kiểm tra:**
- `profile`: định danh và thông tin hồ sơ.
- `settings`: thiết lập người chơi.
- `progress`: màn chơi hiện tại, màn đã mở khóa và thống kê tiến độ/hiệu suất nếu có.
- `inventory`: lưới kho đồ, danh sách vật phẩm đang lưu, tài nguyên/tiền tệ nếu có.
- `farm` hoặc dữ liệu trạng thái nông trại: chỉ tách riêng nếu cấu trúc lưu trữ thực tế có nhóm này.
- Dữ liệu AI/telemetry: chỉ mô tả nếu thực tế có lưu hoặc có thể trích xuất; không tự giả định đã có file riêng.

**Sơ đồ dữ liệu cần thể hiện tối thiểu:**
- Static Data/ScriptableObject cung cấp cấu hình cho gameplay systems.
- Dynamic Data lưu trạng thái người chơi và tiến trình.
- Runtime systems đọc Static Data + Dynamic Data để thực thi gameplay.
- Khi trạng thái thay đổi, hệ thống cập nhật Dynamic Data và chuyển đến Local Save/PlayFab theo chế độ tài khoản.
- AI nhận các feature/trạng thái cần thiết từ gameplay/progress/inventory và trả quyết định hoặc tham số điều chỉnh về subsystem liên quan.

#### E. AI và DDA bị mô tả thiếu hoặc sai phạm vi

Nhận xét: Báo cáo/kiến trúc chỉ còn “Shop AI (AI Decision Tree)”; DDA biến mất, AI bị thu hẹp thành gợi ý trong Shop. Không có luồng dữ liệu từ tiến độ người chơi vào AI, không rõ mô hình được xây dựng/huấn luyện ở đâu và tích hợp vào game thế nào.

Yêu cầu:
- Rà soát lại toàn bộ phần AI trong báo cáo và mã nguồn/tài liệu hiện có.
- Phân biệt rõ hai vai trò nếu cả hai thực sự tồn tại:
  1. **Shop Decision / Item Generation:** quyết định hoặc sinh danh sách vật phẩm trong Shop, reroll, lựa chọn vật phẩm.
  2. **Dynamic Difficulty Adjustment (DDA):** phân tích trạng thái và tiến độ người chơi để điều chỉnh độ khó, xác suất xuất hiện vật phẩm/nông sản hoặc tham số gameplay.
- Không gộp hai chức năng thành một nhãn “Shop AI” nếu chúng có đầu vào, mục tiêu và đầu ra khác nhau.
- Vẽ **AI Data Flow / Decision Pipeline** có ít nhất các bước:
  1. Thu thập trạng thái từ gameplay: inventory, cách bố trí/footprint, item đang sở hữu hoặc sử dụng, farm/plant/crop state, level, thời gian, kết quả màn chơi và thống kê người chơi (chỉ dùng feature thực sự có).
  2. Tiền xử lý/tạo feature: chỉ rõ dữ liệu thô được chuyển thành những biến đầu vào nào.
  3. Decision Tree/DDA: thể hiện các điều kiện/nhánh quyết định ở mức có thể kiểm chứng.
  4. Output: quyết định điều chỉnh hoặc đề xuất cụ thể (ví dụ difficulty parameter, item pool/rarity/weight, shop item, crop/yield probability... chỉ khi có triển khai).
  5. Integration: hệ thống nào nhận output và áp dụng vào gameplay/Shop/Level.
  6. Feedback loop: kết quả chơi tiếp tục được ghi nhận và trở thành đầu vào cho lần đánh giá sau, nếu hệ thống có cơ chế này.
- Làm rõ AI chạy ở đâu:
  - Nếu Decision Tree là logic C# chạy trong Unity trên Android, phải ghi rõ inference/decision runtime diễn ra local.
  - Nếu có công cụ hoặc script bên ngoài dùng để phân tích/tuning, phân biệt nó với runtime trong game.
  - Chỉ mô tả huấn luyện mô hình khi thực tế có dữ liệu huấn luyện, quy trình huấn luyện và artifact/model được tích hợp. Nếu đây là Decision Tree dạng luật (rule-based), gọi đúng là rule-based Decision Tree; không tuyên bố machine-learning training.
- Nếu có tham số do developer cấu hình trong `LevelSO`/AI Config, mô tả vai trò của chúng và cách runtime sử dụng.
- Cần trả lời được: **AI nhận dữ liệu gì? Dữ liệu đến từ đâu? Được biến đổi thế nào? Quyết định ở đâu? Output là gì? Subsystem nào áp dụng? Kết quả ảnh hưởng gameplay ra sao?**
- Nếu DDA chỉ được đề xuất nhưng chưa hoàn thiện, phải ghi là thiết kế/định hướng hoặc giới hạn hiện tại. Không trình bày như tính năng đã hoàn tất.
- Bổ sung bảng: Input Feature | Nguồn dữ liệu | Decision/Rule | Output | Subsystem nhận output | Trạng thái triển khai.
- Bổ sung sơ đồ AI riêng, đồng thời cập nhật AI trong sơ đồ kiến trúc tổng thể và Use Case.

### 2.3. Những phần khác cần rà soát đồng bộ

- **Mục lục và đánh số:** sửa heading trùng, sai số mục, sai số hình/bảng, tham chiếu chéo lỗi. Đặc biệt kiểm tra lỗi tiêu đề con trong phần Static Data (ví dụ heading 3.4.1 nhưng bên dưới lại đánh số 3.1).
- **Use Case:** đồng bộ với chức năng thật và các màn hình hiện có.
- **Class Diagram:** đồng bộ với tên class, quan hệ và trách nhiệm trong code. Không biến Manager/Service thành entity/data class nếu code không thể hiện như vậy.
- **Sequence/Activity Diagram:** bổ sung luồng quan trọng, ưu tiên:
  - Mở game và nạp dữ liệu.
  - Guest vs PlayFab login/load/save.
  - Inventory placement/usage.
  - Auto-Farming: tiến trình sinh trưởng và thu hoạch.
  - Shop purchase/reroll và AI decision (nếu có).
  - DDA evaluation và áp dụng kết quả (nếu có).
- **ERD/Data Model:** chỉ dùng ERD cho dữ liệu có quan hệ lưu trữ phù hợp; với object graph/runtime data có thể dùng UML Class Diagram. Giải thích lựa chọn sơ đồ.
- **UI:** đảm bảo chức năng được mô tả có màn hình hoặc bằng chứng tương ứng. Không chỉ liệt kê màn hình; cần giải thích tương tác chính và kết nối với system.
- **Kiểm thử:** cập nhật bảng kiểm thử theo chức năng hiện hành. Không tự tạo kết quả, tỷ lệ thành công, FPS hoặc số liệu nếu không có log/bằng chứng.
- **Kết luận/giới hạn/hướng phát triển:** phản ánh đúng mức độ hoàn thiện của Auto-Farming, AI/DDA, cloud sync và các module khác.
- **Tài liệu tham khảo:** giữ nguồn đang dùng nếu hợp lệ; kiểm tra trích dẫn trong nội dung và danh mục. Không tự tạo DOI, tác giả hoặc nguồn.
- **Hình ảnh:** ưu tiên ảnh chụp game, sơ đồ sinh từ mô hình thật và bảng ngắn gọn. Tránh sơ đồ quá nhỏ, chữ khó đọc hoặc một sơ đồ chứa quá nhiều thông tin.

### 2.4. Bộ sơ đồ tối thiểu cần có trong V7

1. Use Case tổng thể đã cập nhật.
2. Logical/System Architecture.
3. Physical Deployment Architecture: Android Device ↔ PlayFab Cloud.
4. Data Architecture Overview: Static Data + Dynamic Data + Runtime Systems + Persistence.
5. ScriptableObject Model: các SO/asset cấu hình và quan hệ.
6. Data UML/Class Diagram: các nhóm dữ liệu chính và quan hệ.
7. Auto-Farming Activity/Sequence Diagram.
8. AI Decision Tree/DDA Data Flow Diagram.
9. Các sơ đồ Sequence bổ sung cho login/save/load, inventory/shop nếu báo cáo hiện tại chưa có hoặc chưa đủ rõ.

Mỗi sơ đồ phải:
- Có tiêu đề, chú thích và được nhắc đến trong phần nội dung.
- Có tên thành phần nhất quán với code/tài liệu.
- Có mũi tên thể hiện đúng chiều dữ liệu hoặc điều khiển.
- Có ranh giới Device/Cloud rõ khi liên quan triển khai.
- Không đưa chi tiết chưa được xác minh vào sơ đồ.
- Có độ phân giải và bố cục đủ đọc khi đặt trong trang báo cáo.

### 2.5. Kết quả bàn giao V7

- Một file báo cáo V7 mới, hoàn chỉnh và có thể mở/chỉnh sửa.
- Các sơ đồ ở định dạng có thể chỉnh sửa (Mermaid/PlantUML/draw.io hoặc định dạng nguồn phù hợp) nếu workflow hỗ trợ; đồng thời nhúng/xuất hình vào báo cáo.
- Không để placeholder kiểu `[Insert diagram]`, `[TODO]`, hoặc mô tả sơ đồ bằng chữ thay cho sơ đồ thực tế.
- Một changelog ngắn liệt kê mục đã sửa/bổ sung so với V5.
- Một danh sách riêng các điểm chưa thể xác minh hoặc chưa triển khai, không được âm thầm biến thành tính năng hoàn chỉnh.

---

## 3. Đầu ra B – Viết bài báo khoa học bằng tiếng Anh

### 3.1. Mục tiêu và định hướng

Sau khi rà soát tài liệu và xác định chính xác trạng thái hệ thống, viết một bài báo khoa học **bằng tiếng Anh** dựa trên Harvest Grid. Bài báo nên tập trung vào hướng ứng dụng AI trong game, đặc biệt là mối liên hệ giữa Inventory Management, Auto-Farming và Decision Tree/Dynamic Difficulty Adjustment.

Không dịch nguyên văn toàn bộ báo cáo đồ án thành tiếng Anh. Bài báo phải có một vấn đề nghiên cứu, phương pháp, kiến trúc/thiết kế, cách đánh giá và đóng góp rõ ràng; phạm vi phải phù hợp với bằng chứng thực tế.

### 3.2. Hướng nghiên cứu đề xuất

Hướng ưu tiên:

**An AI-Assisted Adaptive Gameplay Framework for an Inventory Management and Auto-Farming Game**

Có thể điều chỉnh tiêu đề sau khi đã kiểm tra nội dung và kết quả thực tế. Trọng tâm cần làm rõ:
- Inventory layout và item configuration tạo ra trạng thái/đặc trưng ảnh hưởng đến hiệu quả nông trại.
- Auto-Farming tạo ra một gameplay loop có kết quả quan sát được theo thời gian.
- Player progress và gameplay state có thể được dùng làm input cho Decision Tree/DDA.
- AI decision tác động đến shop/item generation hoặc difficulty/gameplay parameters như thế nào.
- Cách thiết kế này tạo ra vòng lặp giữa quyết định quản lý kho của người chơi và hiệu quả hệ thống tự động.

Nếu bằng chứng hiện tại cho thấy DDA chưa được triển khai hoặc chưa có thực nghiệm, hãy thu hẹp tiêu đề và đóng góp về **AI-assisted item/shop decision** hoặc **framework design/prototype**, đồng thời trình bày DDA là hướng mở rộng. Không giữ một tuyên bố nghiên cứu lớn hơn những gì hệ thống chứng minh được.

### 3.3. Cấu trúc bài báo

Ưu tiên template có sẵn trong thư mục. Nếu không có, sử dụng cấu trúc khoa học phù hợp với IEEE:

1. **Title**
2. **Abstract** – vấn đề, mục tiêu, phương pháp, hệ thống đề xuất, kết quả thực tế và đóng góp. Không nêu kết quả chưa đo.
3. **Index Terms / Keywords**
4. **I. Introduction**
   - Bối cảnh và vấn đề.
   - Khoảng trống/vấn đề mà đề tài hướng tới.
   - Mục tiêu và câu hỏi nghiên cứu (nếu phù hợp).
   - Contributions, liệt kê ngắn gọn và có thể kiểm chứng.
5. **II. Related Work**
   - Inventory-management games, automated farming/game simulation, adaptive difficulty, Decision Tree hoặc AI-driven content/item selection.
   - Chỉ sử dụng nguồn học thuật đáng tin cậy, có thông tin thư mục xác minh được.
6. **III. System Design and Architecture**
   - Tổng quan Harvest Grid.
   - Inventory Grid/Item representation.
   - Auto-Farming model.
   - Static/Dynamic Data và runtime architecture khi cần để giải thích phương pháp.
7. **IV. AI-Based Decision and Adaptive Mechanism**
   - AI objective và phạm vi.
   - Input features, preprocessing/state representation.
   - Decision Tree/rules.
   - DDA policy và các tham số điều chỉnh (nếu thực sự có).
   - Shop/item generation hoặc recommendation (nếu có).
   - Runtime integration và feedback loop.
8. **V. Implementation**
   - Unity/C# và các thành phần liên quan.
   - Nơi thực thi AI.
   - Cách dữ liệu được truyền giữa gameplay và AI.
   - Chỉ mô tả implementation đã xác minh.
9. **VI. Experiments and Evaluation**
   - Thiết kế kiểm thử, test scenarios, metrics, baseline và kết quả.
   - Có thể đánh giá tính đúng đắn của decision, độ phù hợp độ khó, phân phối vật phẩm, hiệu quả gameplay hoặc phản hồi người chơi nếu có dữ liệu.
   - Nếu chưa có thực nghiệm người chơi/đối chứng, không giả lập số liệu. Trình bày prototype validation hoặc functional tests đúng với bằng chứng.
10. **VII. Discussion**
    - Ý nghĩa, trade-offs, hạn chế, tính khái quát.
11. **VIII. Conclusion and Future Work**
12. **References**

Có thể gộp/tách các mục theo giới hạn trang của template, nhưng không bỏ các nội dung phương pháp và đánh giá cốt lõi.

### 3.4. Yêu cầu học thuật và nguồn tham khảo

- Viết bằng tiếng Anh học thuật, tự nhiên, rõ ràng, tránh câu dài và diễn đạt quảng cáo.
- Không dùng giọng văn “project report” hoặc liệt kê tính năng như tài liệu giới thiệu sản phẩm.
- Phân biệt **proposed**, **implemented**, **evaluated** và **future work**.
- Không khẳng định mô hình được “trained” nếu thực tế chỉ là Decision Tree dựa trên luật/điều kiện do developer thiết kế.
- Không tuyên bố AI tự học, machine learning, neural network hoặc personalization theo thời gian nếu không có bằng chứng.
- Không tạo dữ liệu thực nghiệm, bảng kết quả, biểu đồ, số người tham gia, baseline hoặc metric giả.
- Related Work cần nguồn học thuật có thể kiểm chứng (IEEE Xplore, ACM Digital Library, Springer, ScienceDirect, arXiv khi phù hợp). Ghi đủ tác giả, tên bài, nơi xuất bản, năm, DOI/URL nếu xác minh được.
- Trích dẫn trong nội dung phải khớp với References. Không dùng nguồn không tồn tại hoặc citation do AI tự suy đoán.
- Nếu được phép nghiên cứu web, hãy tìm nguồn học thuật thật và ghi lại URL/DOI. Ưu tiên bài gốc và nguồn có liên quan trực tiếp.
- Mọi tuyên bố định lượng phải có nguồn dữ liệu hoặc phép đo tương ứng.

### 3.5. Hình và bảng trong bài báo

Ưu tiên hình/bảng có giá trị khoa học:
- Overall system architecture.
- Inventory-to-farming gameplay loop.
- AI decision pipeline / Decision Tree.
- Bảng input features và output decisions.
- Bảng cấu hình/tham số hoặc test scenarios.
- Biểu đồ kết quả chỉ khi có dữ liệu đo thật.

Mỗi hình/bảng phải được dẫn chiếu trong phần nội dung, có caption tiếng Anh và dễ đọc ở kích thước bài báo. Không dùng ảnh UI trang trí thay cho sơ đồ phương pháp.

### 3.6. Đầu ra bài báo

- Một bản thảo tiếng Anh hoàn chỉnh, theo template có sẵn nếu có.
- File nguồn có thể chỉnh sửa (DOCX/LaTeX/Markdown tùy template và workflow).
- Danh mục References đã kiểm chứng.
- Danh sách ngắn các dữ liệu/thực nghiệm còn thiếu để có thể nâng cấp thành bài báo có đánh giá mạnh hơn.

---

## 4. Trình tự thực hiện bắt buộc

Thực hiện theo thứ tự để tránh viết bài báo dựa trên mô tả sai:

1. **Inventory the project files:** liệt kê tài liệu, báo cáo, mã nguồn, sơ đồ và bằng chứng có liên quan.
2. **Evidence review:** đối chiếu V5 với phiên bản cũ, source code và tài liệu bổ sung; lập bảng trạng thái tính năng.
3. **Resolve inconsistencies:** xác định chức năng thực tế, đặc biệt Farm/Auto-Farming, Use Case, AI/DDA, Static/Dynamic Data và PlayFab.
4. **Update report to V7:** sửa nội dung và tạo đầy đủ sơ đồ trước.
5. **Validate V7:** kiểm tra chéo các sơ đồ, bảng, heading, số trang, tham chiếu hình/bảng và thuật ngữ.
6. **Define the paper scope:** dựa trên những gì V7 và bằng chứng thực tế xác nhận; chọn tuyên bố nghiên cứu vừa đủ.
7. **Research and write the English paper:** tìm nguồn học thuật thật, viết theo cấu trúc bài báo và trích dẫn đầy đủ.
8. **Final consistency check:** bảo đảm bài báo không đưa ra tuyên bố mâu thuẫn với V7 hoặc code.
9. **Deliver:** bàn giao V7, bài báo, file nguồn sơ đồ (nếu có), changelog và danh sách vấn đề chưa xác minh.

---

## 5. Tiêu chí nghiệm thu

### Báo cáo V7
- [ ] Có chức năng trồng/chăm sóc/Auto-Farming/thu hoạch được mô tả đúng và thể hiện trong Use Case phù hợp.
- [ ] Use Case đồng bộ với kiến trúc và chức năng thực tế; có Farm Management, Settings, Profile, Shop AI khi các chức năng này thực sự tồn tại.
- [ ] Tách rõ Logical Architecture và Physical Deployment Architecture.
- [ ] Android Device chứa Game System, Static Data và Local Save; PlayFab được đặt ở Cloud đúng vai trò.
- [ ] Static Data và Dynamic Data được giải thích rõ bằng sơ đồ và bảng.
- [ ] Có Data UML/Class Diagram và sơ đồ ScriptableObject riêng, dùng tên lớp/asset đã xác minh.
- [ ] AI có input, output, decision flow, runtime location và subsystem integration rõ ràng.
- [ ] DDA không bị bỏ sót nếu đã triển khai/được thiết kế trong phạm vi đề tài; trạng thái thực tế được ghi chính xác.
- [ ] Không có nội dung tự bịa, sơ đồ placeholder hoặc kết quả kiểm thử không có bằng chứng.
- [ ] Mục lục, heading, hình/bảng và trích dẫn nhất quán.

### Bài báo khoa học tiếng Anh
- [ ] Có research problem, objective và contributions rõ ràng.
- [ ] Tập trung vào AI/gameplay mechanism thay vì chỉ mô tả danh sách chức năng.
- [ ] Method và architecture có sơ đồ/bảng giải thích.
- [ ] Phân biệt rõ mô hình rule-based với machine-learning training.
- [ ] Có evaluation dựa trên dữ liệu thật; nếu chưa có thì nêu giới hạn, không bịa số liệu.
- [ ] Related Work và References có thể kiểm chứng.
- [ ] Các tuyên bố trong bài báo nhất quán với V7 và implementation.

---

## 6. Các câu hỏi phải tự xác minh trước khi kết luận

Nếu không tìm thấy bằng chứng trong thư mục/source code, hãy ghi vào danh sách cần xác nhận thay vì tự trả lời:

1. Người chơi có trực tiếp trồng cây hay chỉ cấu hình vật phẩm để hệ thống tự động vận hành nông trại?
2. Thu hoạch diễn ra hoàn toàn tự động hay có thao tác của người chơi?
3. Farm Slot có thể mua/nâng cấp không? Giá và điều kiện lấy từ đâu?
4. AI hiện tại chỉ quyết định nội dung Shop, hay còn điều chỉnh độ khó/xác suất gameplay?
5. DDA đã được code và chạy trong game chưa, hay mới dừng ở thiết kế?
6. Decision Tree là luật/nhánh điều kiện viết bằng C#, hay mô hình được huấn luyện từ dữ liệu?
7. AI lấy dữ liệu trực tiếp từ Inventory, Farm, Level/Progress hay từ một tập thống kê đã tổng hợp?
8. AI chạy local trên Android hay có phần xử lý trên cloud?
9. Những dữ liệu nào thực sự được lưu local và những dữ liệu nào được đồng bộ lên PlayFab?
10. Có log, test case, playtest hoặc dữ liệu đo nào đủ để đánh giá hiệu quả AI/DDA không?

**Nguyên tắc cuối cùng:** Ưu tiên tính đúng đắn, khả năng kiểm chứng và sự nhất quán giữa báo cáo – sơ đồ – mã nguồn hơn việc làm tài liệu có vẻ đầy đủ. Nếu một chức năng mới chỉ là ý tưởng, hãy ghi rõ là đề xuất/hướng phát triển; tuyệt đối không mô tả nó như tính năng đã hoàn thành.
