# Giải thích các sơ đồ trong PRD PL-Tour

**Mục đích tài liệu:**
Tài liệu này dùng để giải thích ngắn gọn, rõ ràng và dễ thuyết trình các sơ đồ trong PRD `PRD_PLTour.md`. Nội dung được viết theo hướng báo cáo đồ án, giúp người trình bày có thể nói mạch lạc về luồng nghiệp vụ, vai trò của từng thành phần và ý nghĩa của từng sơ đồ.

**Lưu ý để dễ sửa code sau này:**
Mỗi phần giải thích bên dưới đều kèm theo **đường dẫn file**, **service/controller/method liên quan** để khi cần fix code hoặc tìm lại luồng nghiệp vụ có thể lần ngay tới đúng vị trí trong repo.

---

## 1. Cách đọc tài liệu PRD

Trong PRD của PL-Tour, mỗi nhóm sơ đồ có vai trò khác nhau:

- **Use Case diagram**: cho biết hệ thống có những chức năng nào và ai sử dụng chức năng đó.
- **Sequence diagram**: mô tả thứ tự gọi giữa các thành phần như UI, service, controller, API, database.
- **Activity diagram**: mô tả luồng xử lý theo từng bước, đặc biệt hữu ích khi thuyết trình nghiệp vụ.
- **Flowchart tổng quan**: cho thấy mối liên kết giữa Mobile App, Admin, Vendor và API.

Khi báo cáo, nên đi theo thứ tự:

1. Giới thiệu tổng quan hệ thống.
2. Giải thích Use Case để nói về phạm vi chức năng.
3. Đi vào Sequence để mô tả luồng xử lý chi tiết.
4. Chốt lại bằng Activity để nói rõ logic nghiệp vụ theo từng bước.

---

## 2. Giải thích sơ đồ Use Case

### 2.1 Mục đích

Sơ đồ Use Case thể hiện:

- Ai là người dùng của hệ thống.
- Mỗi vai trò có thể thực hiện những chức năng nào.
- Chức năng nào bắt buộc phải đi qua bước đăng nhập hoặc khởi tạo app.

### 2.2 Các file liên quan

- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour.md` — nơi chứa sơ đồ Use Case và bảng mapping.
- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour_GiaiThichSoDo.md` — tài liệu giải thích để báo cáo.

### 2.3 Cách trình bày khi báo cáo

Bạn có thể nói:

> “Phần Use Case của PL-Tour được chia thành 3 nhóm chính: Mobile App, Admin và Vendor. Mỗi nhóm có các chức năng riêng, và các chức năng này đều được ánh xạ sang sequence diagram và activity diagram tương ứng để mô tả chi tiết luồng xử lý.”

### 2.4 Ý nghĩa từng nhóm

#### Mobile App
- Là phần khách du lịch sử dụng trực tiếp.
- Có các chức năng như khởi tạo app, chọn ngôn ngữ, tải tour/location, xem bản đồ, quét QR, phát narration, gửi heartbeat và analytics.
- Đây là nhóm chức năng lõi của sản phẩm.

**File/method/service hay gặp:**
- `PLTour.App/App.xaml.cs`
- `PLTour.App/Pages/HomePage.xaml.cs`
- `PLTour.App/Pages/MapPage.xaml.cs`
- `PLTour.App/Pages/QrScannerPage.xaml.cs`
- `PLTour.App/Pages/TourDetailPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs`
- `PLTour.App/Services/AudioService.cs`
- `PLTour.App/Services/LocalizationService.cs`
- `PLTour.App/Services/DeviceMonitorService.cs`
- `PLTour.App/Services/MonitorQueueService.cs`

#### Admin
- Là người quản trị hệ thống.
- Có thể quản lý category, location, tour, narration, vendor, monitor thiết bị và xem analytics.
- Mục tiêu là kiểm soát dữ liệu và theo dõi vận hành.

**File/method/service hay gặp:**
- `PLTour.Admin/Controllers/AccountController.cs`
- `PLTour.Admin/Controllers/CategoryController.cs`
- `PLTour.Admin/Controllers/LocationController.cs`
- `PLTour.Admin/Controllers/TourController.cs`
- `PLTour.Admin/Controllers/NarrationController.cs`
- `PLTour.Admin/Controllers/VendorController.cs`
- `PLTour.Admin/Controllers/MonitorController.cs`
- `PLTour.Admin/Controllers/AnalyticsController.cs`

#### Vendor
- Là đối tác nội dung.
- Quản lý store, sản phẩm, ảnh và subscription.
- Có vai trò cập nhật dữ liệu nghiệp vụ liên quan đến nội dung của mình.

**File/method/service hay gặp:**
- `PLTour.Vendor/Controllers/VendorRegistrationController.cs`
- `PLTour.Vendor/Controllers/VendorLoginController.cs`
- `PLTour.Vendor/Controllers/VendorDashboardController.cs`
- `PLTour.Vendor/Controllers/VendorStoreController.cs`
- `PLTour.Vendor/Controllers/VendorProductController.cs`
- `PLTour.Vendor/Controllers/VendorImageController.cs`
- `PLTour.Vendor/Controllers/VendorSubscriptionController.cs`

### 2.5 Câu nói ngắn khi thuyết trình

- “Use Case giúp em xác định rõ hệ thống có những tác nhân nào và mỗi tác nhân làm được gì.”
- “Mobile App là luồng khách du lịch, Admin là luồng vận hành, Vendor là luồng quản lý nội dung.”
- “Các use case đều được map sang sequence và activity để tránh mô tả mơ hồ.”

---

## 3. Giải thích sơ đồ mapping Use Case → Sequence → Activity

### 3.1 Mục đích

Bảng mapping trong PRD dùng để:

- Xác định mỗi chức năng có sơ đồ nào tương ứng.
- Không bị thiếu sơ đồ khi báo cáo.
- Dễ đối chiếu khi giảng viên hỏi “chức năng này nằm ở đâu trong PRD?”.

### 3.2 File liên quan

- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour.md`
- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour_GiaiThichSoDo.md`

### 3.3 Cách nói dễ hiểu

> “Mỗi use case trong PRD đều có một sequence diagram để mô tả luồng gọi giữa các lớp và một activity diagram để mô tả logic xử lý theo bước. Bảng mapping giúp em đảm bảo không có chức năng nào bị bỏ sót.”

### 3.4 Ví dụ

- `UC-M01` Khởi tạo app → `Sequence 12.1` → `Activity 13.1`
- `UC-A02` Dashboard tổng quan → `Sequence 12.15` → `Activity 13.15`
- `UC-V05` Quản lý sản phẩm → `Sequence 12.28` → `Activity 13.28`

### 3.5 Cách dùng khi sửa code

Nếu giảng viên hỏi “luồng này nằm ở file nào?”, có thể lần theo:

1. Xem ID use case trong bảng mapping.
2. Mở đúng sequence/activity tương ứng.
3. Tìm service/controller/method được ghi trong phần giải thích sequence bên dưới.

---

## 4. Giải thích sơ đồ Sequence

### 4.1 Mục đích

Sequence diagram cho thấy:

- Ai gọi ai.
- Thành phần nào xử lý trước, thành phần nào xử lý sau.
- Dữ liệu đi từ UI đến service, API, database như thế nào.

### 4.2 File liên quan

- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour.md`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\ApiService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\AudioService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\LocalizationService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\DeviceMonitorService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\MonitorQueueService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\MonitorQueueStore.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.API\Controllers\*.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.Admin\Controllers\*.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.Vendor\Controllers\*.cs`

### 4.3 Cách đọc sequence diagram

Khi đọc sequence, nên đi theo 4 câu hỏi:

1. Người dùng làm gì?
2. UI gửi gì?
3. Service/API xử lý ra sao?
4. Kết quả trả về thế nào?

### 4.4 Giải thích theo nhóm chức năng

#### 4.4.1 Khởi tạo ứng dụng

**Sequence tương ứng:** `12.1`

**File/method/service liên quan:**
- `PLTour.App/App.xaml.cs` → `CreateWindow()`
- `PLTour.App/Services/LocalizationService.cs` → `ApplyLanguage(...)`
- `PLTour.App/Services/DeviceMonitorService.cs` → `Start()`
- `PLTour.App/Services/DeviceMonitorService.cs` → `StartHeartbeatLoop()`

Luồng này mô tả lúc app vừa mở:
- OS mở cửa sổ app qua `App.CreateWindow()`.
- App áp dụng ngôn ngữ đã lưu bằng `LocalizationService.ApplyLanguage(...)`.
- App bật dịch vụ monitor bằng `DeviceMonitorService.Start()`.
- `DeviceMonitorService` chạy vòng heartbeat bằng `StartHeartbeatLoop()`.
- App điều hướng vào màn hình đầu tiên.

Câu nói báo cáo:

> “Sequence này cho thấy app không chỉ mở giao diện mà còn khởi động luôn các dịch vụ nền như localization và heartbeat.”

#### 4.4.2 Chọn ngôn ngữ

**Sequence tương ứng:** `12.2`

**File/method/service liên quan:**
- `PLTour.App/Pages/SettingsPage.xaml.cs`
- `PLTour.App/Services/LocalizationService.cs`
- `Preferences`

Luồng này mô tả:
- Người dùng mở `SettingsPage`.
- Người dùng chọn ngôn ngữ mong muốn từ UI.
- `SettingsPage` gọi `LocalizationService.ApplyLanguage(selectedLang, persist: true)`.
- `LocalizationService` ghi ngôn ngữ vào `Preferences`.
- UI refresh lại toàn bộ text theo culture mới.

Câu nói báo cáo:

> “Luồng này chứng minh ngôn ngữ được lưu cục bộ để giữ trải nghiệm nhất quán cho các lần mở app sau.”

#### 4.4.3 Tải tour và location

**Sequence tương ứng:** `12.3`

**File/method/service liên quan:**
- `PLTour.App/Services/ApiService.cs` → `GetToursAsync()`
- `PLTour.App/Services/ApiService.cs` → `GetLocationsAsync()`
- `PLTour.API/Controllers/ToursController.cs`
- `PLTour.API/Controllers/LocationsController.cs`

Luồng này mô tả:
- `HomePage` hoặc `MapPage` gọi `ApiService.GetToursAsync()`.
- `ApiService` gửi request lên `ToursController`.
- API trả về danh sách `TourDto`.
- App map dữ liệu sang model hiển thị.
- Sau đó `ApiService.GetLocationsAsync()` được gọi để lấy danh sách location.
- `LocationsController` trả về `LocationDto[]`.
- UI render danh sách location hoặc POI trên màn hình.

Câu nói báo cáo:

> “Đây là luồng lấy dữ liệu cơ bản nhất, làm nền cho các chức năng map, POI và chi tiết địa điểm.”

#### 4.4.4 Hiển thị bản đồ và POI

**Sequence tương ứng:** `12.4`

**File/method/service liên quan:**
- `PLTour.App/Pages/MapPage.xaml.cs` → `InitMap()` hoặc luồng load map tương đương
- `PLTour.App/Services/LocationService.cs` → `LoadCurrentLocationAsync()`
- `PLTour.App/Services/ApiService.cs` → `GetLocationsAsync()`
- `PLTour.API/Controllers/LocationsController.cs`

Luồng này mô tả:
- Map khởi tạo tại `MapPage.InitMap()`.
- Nạp vị trí hiện tại bằng `LocationService.LoadCurrentLocationAsync()`.
- Nạp dữ liệu POI bằng `await ApiService.GetLocationsAsync()`.
- Render POI layer và thêm layer vào bản đồ.
- Khi tap marker, `MapPage` chọn POI gần nhất.
- Mở chi tiết POI bằng `PoiCard_Tapped(...)` hoặc điều hướng sang màn chi tiết.

Câu nói báo cáo:

> “Sequence này giải thích cách app kết hợp GPS và dữ liệu POI để hiển thị bản đồ tương tác.”

#### 4.4.5 Xem chi tiết địa điểm

**Sequence tương ứng:** `12.5`

**File/method/service liên quan:**
- `PLTour.App/Pages/MapPage.xaml.cs`
- `PLTour.App/Views/PoiDetailPopupView.xaml`
- `PLTour.App/Pages/TourDetailPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetLocationByIdAsync(...)`
- `PLTour.API/Controllers/LocationsController.cs` → action chi tiết location

Luồng này mô tả:
- Người dùng chọn một POI từ bản đồ hoặc danh sách.
- UI gọi `ApiService.GetLocationByIdAsync(id)`.
- `LocationsController` xử lý request chi tiết.
- API trả về `LocationDto`.
- App map dữ liệu vào `PoiDetailViewModel`.
- `PoiDetailPopupView` hoặc `TourDetailPage` hiển thị tên, mô tả, ảnh và narration.

Câu nói báo cáo:

> “Luồng này cho thấy khi người dùng chọn một địa điểm, hệ thống lấy chi tiết từ API rồi chuyển vào ViewModel để hiển thị.”

#### 4.4.6 Quét QR

**Sequence tương ứng:** `12.6`

**File/method/service liên quan:**
- `PLTour.App/Pages/QrScannerPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetLocationByQrAsync(qrCode)`
- `PLTour.API/Controllers/LocationsController.cs` → endpoint QR
- `PLTour.App/Services/Navigation` hoặc Shell navigation

Luồng này mô tả:
- Người dùng mở `QrScannerPage`.
- Camera quét QR code và trả chuỗi mã cho page.
- `QrScannerPage` gọi `ApiService.GetLocationByQrAsync(qrCode)`.
- `LocationsController` xử lý endpoint QR.
- API trả về dữ liệu location hoặc thông tin điều hướng.
- App dùng Shell navigation để mở màn hình chi tiết tương ứng.

Câu nói báo cáo:

> “QR là cổng vào nhanh giúp người dùng đi thẳng đến nội dung mong muốn.”

#### 4.4.7 Phát narration audio

**Sequence tương ứng:** `12.7`

**File/method/service liên quan:**
- `PLTour.App/Pages/TourDetailPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetNarrationAsync(locationId, language)`
- `PLTour.App/Services/AudioService.cs` → `PlayAsync(audioUrl)`
- `PLTour.API/Controllers/NarrationsController.cs`

Luồng này mô tả:
- Người dùng bấm nút Play trong `TourDetailPage`.
- `TourDetailPage` gọi `ApiService.GetNarrationAsync(locationId, language)`.
- `ApiService` gửi request đến `NarrationsController`.
- API trả về `NarrationDto`.
- Nếu có `AudioUrl`, `AudioService.PlayAsync(audioUrl)` sẽ stream file audio.
- Nếu không có audio, app chuyển sang TTS fallback.

Câu nói báo cáo:

> “Sequence này thể hiện logic ưu tiên audio có sẵn trước, nếu thiếu thì fallback sang TTS.”

#### 4.4.8 Ghi lịch sử phát

**Sequence tương ứng:** `12.9`

**File/method/service liên quan:**
- `PLTour.App/Services/AnalyticsService.cs`
- `PLTour.App/Services/ApiService.cs` → `PostListenAsync(...)`
- `PLTour.API/Controllers/ListenAnalyticsController.cs` hoặc controller analytics tương ứng

Luồng này mô tả:
- Audio kết thúc và UI phát sinh sự kiện `OnPlaybackEnded(duration)`.
- `AnalyticsService` nhận duration và tạo payload listen.
- `AnalyticsService` gọi `ApiService.PostListenAsync(locationId, duration)`.
- `ApiService` gửi request lên backend analytics controller.
- API trả kết quả `accepted` hoặc `duplicate`.
- App ghi nhận kết quả để phục vụ thống kê nghe.

Câu nói báo cáo:

> “Luồng này dùng để ghi nhận hành vi nghe của người dùng phục vụ thống kê.”

#### 4.4.9 Gửi heartbeat thiết bị

**Sequence tương ứng:** `12.10`

**File/method/service liên quan:**
- `PLTour.App/Services/DeviceMonitorService.cs`
- `PLTour.App/Services/MonitorQueueService.cs` → `EnqueueAsync(...)`
- `PLTour.App/Services/MonitorQueueStore.cs`
- `PLTour.API/Controllers/MonitorController.cs` → `Heartbeat(...)`

Luồng này mô tả:
- `DeviceMonitorService` chạy timer và tạo heartbeat định kỳ.
- Heartbeat payload được đưa vào `MonitorQueueService.EnqueueAsync(...)`.
- `MonitorQueueService` lưu item vào queue và persist xuống store.
- Worker của queue gửi POST lên `MonitorController.Heartbeat(...)`.
- API cập nhật hoặc tạo mới `ActiveDevice` trong database.
- Admin sẽ nhìn thấy thiết bị online ở dashboard monitor.

Câu nói báo cáo:

> “Heartbeat giúp admin biết thiết bị nào đang online và theo dõi được trạng thái thực tế.”

#### 4.4.10 Queue khi mạng yếu

**Sequence tương ứng:** `12.12`

**File/method/service liên quan:**
- `PLTour.App/Services/MonitorQueueService.cs`
- `PLTour.App/Services/MonitorQueueStore.cs`
- `PLTour.API/Controllers/MonitorController.cs`
- `PLTour.App/Services/QueuedActionService.cs` (nếu luồng nội bộ cần xếp hàng)

Luồng này mô tả:
- Khi request heartbeat hoặc analytics thất bại, item không bị mất.
- `MonitorQueueService` giữ item trong memory queue và lưu xuống store.
- Worker tiếp tục retry theo chu kỳ.
- Nếu gửi thành công thì item bị remove khỏi queue.
- Nếu vẫn thất bại thì item được requeue để thử lại lần sau.

Câu nói báo cáo:

> “Cơ chế queue giúp đảm bảo heartbeat và analytics không bị mất khi mạng chập chờn.”

#### 4.4.11 Tự động phát theo preference

**Sequence tương ứng:** `12.13`

**File/method/service liên quan:**
- `PLTour.App/Pages/TourDetailPage.xaml.cs`
- `PLTour.App/Services/AudioService.cs`
- `PLTour.App/Properties/Preferences`

Luồng này mô tả:
- `TourDetailPage` đọc cờ autoplay từ `Preferences`.
- Nếu autoplay đang bật, app chọn nội dung gợi ý tiếp theo.
- `AudioService` phát bài kế tiếp mà không cần người dùng bấm lại.
- Nếu autoplay tắt, app dừng ở trạng thái chờ thao tác người dùng.

Câu nói báo cáo:

> “Luồng này thể hiện việc hệ thống tôn trọng cấu hình của người dùng.”

### 4.5 Công thức nói chung về sequence

Khi bị hỏi bất kỳ sequence nào, có thể trả lời theo mẫu:

> “Sequence này mô tả thứ tự từ người dùng → UI → service → API → database, sau đó phản hồi ngược lại để cập nhật giao diện.”

---

## 5. Giải thích sơ đồ Activity

### 5.1 Mục đích

Activity diagram mô tả logic xử lý theo từng bước, phù hợp để giải thích:

- điều kiện rẽ nhánh,
- vòng lặp,
- retry,
- xử lý thành công / thất bại,
- các bước nghiệp vụ bên trong một chức năng.

### 5.2 File liên quan

- `C:\Users\LENOVO\source\repos\PL-Tour\PRD_PLTour.md`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\MonitorQueueService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\QueuedActionService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\DeviceMonitorService.cs`
- `C:\Users\LENOVO\source\repos\PL-Tour\PLTour.App\Services\AudioService.cs`

### 5.3 Cách trình bày khi báo cáo

> “Nếu sequence cho thấy ai gọi ai thì activity cho thấy hệ thống xử lý theo trình tự nào và rẽ nhánh ra sao.”

### 5.4 Giải thích một số activity quan trọng

#### 5.4.1 Khởi tạo ứng dụng

**Activity tương ứng:** `13.1`

**File/method/service liên quan:**
- `PLTour.App/App.xaml.cs` → `CreateWindow()`
- `PLTour.App/Services/LocalizationService.cs` → `ApplyLanguage(...)`
- `PLTour.App/Services/DeviceMonitorService.cs` → `Start()`

- Mở app.
- Load ngôn ngữ.
- Khởi động DeviceMonitorService.
- Điều hướng sang trang đầu tiên.

#### 5.4.2 Chọn ngôn ngữ

**Activity tương ứng:** `13.2`

**File/method/service liên quan:**
- `PLTour.App/Pages/SettingsPage.xaml.cs`
- `PLTour.App/Services/LocalizationService.cs`
- `Preferences`

- Mở Settings.
- Chọn ngôn ngữ.
- ApplyLanguage.
- Save Preferences.
- Refresh UI.

#### 5.4.3 Tải tour và location

**Activity tương ứng:** `13.3`

**File/method/service liên quan:**
- `PLTour.App/Pages/HomePage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetToursAsync()`, `GetLocationsAsync()`
- `PLTour.API/Controllers/ToursController.cs`
- `PLTour.API/Controllers/LocationsController.cs`

- HomePage mở.
- Gọi API tour.
- Gọi API location.
- Map dữ liệu vào giao diện.

#### 5.4.4 Hiển thị bản đồ

**Activity tương ứng:** `13.4`

**File/method/service liên quan:**
- `PLTour.App/Pages/MapPage.xaml.cs`
- `PLTour.App/Services/LocationService.cs`
- `PLTour.App/Services/ApiService.cs`

- Lấy vị trí hiện tại.
- Tải POI.
- Render marker.

#### 5.4.5 Xem chi tiết địa điểm

**Activity tương ứng:** `13.5`

**File/method/service liên quan:**
- `PLTour.App/Pages/MapPage.xaml.cs`
- `PLTour.App/Views/PoiDetailPopupView.xaml`
- `PLTour.App/Services/ApiService.cs` → `GetLocationByIdAsync(...)`

- User chọn POI.
- Load detail.
- Map sang ViewModel.
- Render detail screen.

#### 5.4.6 Quét QR

**Activity tương ứng:** `13.6`

**File/method/service liên quan:**
- `PLTour.App/Pages/QrScannerPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetLocationByQrAsync(...)`
- `PLTour.App/Services/Navigation`

- Mở scanner.
- Quét mã.
- Kiểm tra hợp lệ.
- Nếu đúng thì mở chi tiết, nếu sai thì báo lỗi.

#### 5.4.7 Phát narration

**Activity tương ứng:** `13.7` và `13.8`

**File/method/service liên quan:**
- `PLTour.App/Pages/TourDetailPage.xaml.cs`
- `PLTour.App/Services/ApiService.cs` → `GetNarrationAsync(...)`
- `PLTour.App/Services/AudioService.cs` → `PlayAsync(...)`, `SpeakTextAsync(...)`

- Mở màn hình chi tiết.
- Load narration.
- Nếu có audio thì phát.
- Nếu không có thì TTS fallback.

#### 5.4.8 Ghi lịch sử phát

**Activity tương ứng:** `13.9`

**File/method/service liên quan:**
- `PLTour.App/Services/AnalyticsService.cs`
- `PLTour.App/Services/ApiService.cs` → `PostListenAsync(...)`

- Audio kết thúc.
- Tạo payload listen.
- Gửi analytics.
- Lưu kết quả.

#### 5.4.9 Heartbeat thiết bị

**Activity tương ứng:** `13.10`

**File/method/service liên quan:**
- `PLTour.App/Services/DeviceMonitorService.cs`
- `PLTour.App/Services/MonitorQueueService.cs`
- `PLTour.API/Controllers/MonitorController.cs` → `Heartbeat(...)`

- Timer tick.
- Enqueue heartbeat.
- Gửi lên API.
- Cập nhật ActiveDevice.

#### 5.4.10 Queue khi mạng yếu

**Activity tương ứng:** `13.12`

**File/method/service liên quan:**
- `PLTour.App/Services/MonitorQueueService.cs`
- `PLTour.App/Services/MonitorQueueStore.cs`
- `PLTour.App/Services/QueuedActionService.cs`

- Request fail.
- Save vào local queue.
- Retry loop.
- Thành công thì xóa item.

### 5.5 Câu nói chốt khi báo cáo activity

> “Activity diagram giúp em chứng minh rằng các luồng nghiệp vụ của hệ thống không chỉ chạy đúng mà còn có xử lý điều kiện, retry và trạng thái thành công/thất bại rõ ràng.”

---

## 6. Giải thích sơ đồ tổng thể giữa vai trò và API

### 6.1 Ý nghĩa

Sơ đồ này cho thấy:

- Mobile App, Admin và Vendor đều gọi vào cùng một API trung tâm.
- API là lớp xử lý nghiệp vụ và lưu dữ liệu.
- Database là nơi lưu trạng thái cuối cùng.

### 6.2 Câu nói báo cáo

> “PL-Tour được thiết kế theo mô hình tập trung, trong đó API đóng vai trò trung gian giữa các client và database.”

---

## 7. Giải thích sơ đồ monitor và analytics

### 7.1 Monitor thiết bị

Sơ đồ monitor cho thấy:

- Mobile App gửi heartbeat.
- Queue service trung gian xử lý gửi request.
- API cập nhật trạng thái ActiveDevice.
- Admin xem danh sách online/offline trên dashboard.

**File/method/service liên quan:**
- `PLTour.App/Services/DeviceMonitorService.cs`
- `PLTour.App/Services/MonitorQueueService.cs` → `EnqueueAsync(...)`, `Start()`, `Stop()`
- `PLTour.App/Services/MonitorQueueStore.cs`
- `PLTour.API/Controllers/MonitorController.cs` → `Heartbeat(...)`, `TrackEvent(...)`
- `PLTour.Admin/Controllers/MonitorController.cs`

Câu nói báo cáo:

> “Monitor giúp admin biết app nào đang hoạt động, app nào stale hoặc offline.”

### 7.2 Analytics

Sơ đồ analytics cho thấy:

- App gửi analytics event.
- Event có thể đi qua queue.
- API lưu vào bảng thống kê.
- Admin xem biểu đồ và dashboard.

**File/method/service liên quan:**
- `PLTour.App/Services/AnalyticsService.cs`
- `PLTour.App/Services/ApiService.cs` → `PostListenAsync(...)`, `TrackEventAsync(...)`
- `PLTour.API/Controllers/AnalyticsController.cs` hoặc controller analytics tương ứng
- `PLTour.Admin/Controllers/AnalyticsController.cs`

Câu nói báo cáo:

> “Analytics giúp hệ thống theo dõi hành vi người dùng để phục vụ báo cáo và cải tiến sản phẩm.”

---

## 8. Cách nói ngắn gọn khi giảng viên hỏi

### 8.1 Khi hỏi “PRD này có gì?”

> “PRD này mô tả đầy đủ hệ thống PL-Tour gồm Mobile App, Admin, Vendor và API, đồng thời có use case, sequence, activity, mapping và phần dữ liệu chính để phục vụ báo cáo đồ án.”

### 8.2 Khi hỏi “Sơ đồ nào quan trọng nhất?”

> “Quan trọng nhất là mapping use case → sequence → activity, vì nó chứng minh tính đầy đủ và giúp đối chiếu từng chức năng với luồng xử lý cụ thể.”

### 8.3 Khi hỏi “Sequence khác Activity ở điểm nào?”

> “Sequence mô tả thứ tự gọi giữa các thành phần, còn Activity mô tả logic xử lý và rẽ nhánh nghiệp vụ.”

### 8.4 Khi hỏi “Lý do có queue?”

> “Queue dùng để bảo đảm dữ liệu heartbeat và analytics không bị mất khi mạng yếu, đồng thời giúp hệ thống retry an toàn.”

### 8.5 Khi hỏi “Nếu cần sửa code thì tìm ở đâu?”

> “Em sẽ dựa vào bảng mapping, rồi mở đúng sequence/activity để biết luồng đó nằm ở file nào, service nào, controller nào và method nào.”

---

## 9. Kết luận để báo cáo

Bạn có thể chốt phần PRD bằng câu:

> “PRD của PL-Tour đã được chuẩn hóa theo cấu trúc rõ ràng, có mapping chức năng đầy đủ, sơ đồ use case/sequence/activity đồng bộ và có giải thích nghiệp vụ cụ thể, nên phù hợp để dùng trực tiếp trong báo cáo đồ án.”
