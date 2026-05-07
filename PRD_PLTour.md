# PRD — PL-Tour

**Phiên bản:** 1.1  
**Ngày cập nhật:** 08/05/2026  
**Phạm vi:** Hệ thống PL-Tour gồm API, Admin, Vendor, và Mobile App

---

## 1. Tóm tắt sản phẩm

PL-Tour là nền tảng du lịch thông minh giúp người dùng khám phá địa điểm, xem tour trên bản đồ, quét QR, nghe thuyết minh đa ngôn ngữ, và theo dõi thông tin dịch vụ từ các vendor. Hệ thống gồm:

- `PLTour.API` — backend REST API
- `PLTour.Admin` — cổng quản trị
- `PLTour.Vendor` — cổng vendor
- `PLTour.App` — ứng dụng MAUI cho khách du lịch
- `PLTour.Shared` — entity/DTO dùng chung

---

## 2. Mục tiêu

1. Hỗ trợ khách du lịch tra cứu điểm đến nhanh và trực quan.
2. Cung cấp trải nghiệm thuyết minh theo ngôn ngữ và audio.
3. Cho phép admin quản lý nội dung, vendor, và thiết bị monitor.
4. Cho phép vendor đăng ký, được duyệt, rồi quản lý cửa hàng/sản phẩm.
5. Đảm bảo app mobile có thể gửi heartbeat và analytics để admin giám sát.

---

## 3. Người dùng mục tiêu

### 3.1 Khách du lịch
- Dùng app mobile để xem tour, bản đồ, POI, nghe audio/TTS.
- Quét QR để mở nhanh nội dung liên quan.

### 3.2 Admin
- Quản lý location, tour, narration, vendor.
- Xem dashboard và monitor thiết bị.
- Theo dõi analytics và trạng thái heartbeat.

### 3.3 Vendor
- Đăng ký gian hàng.
- Đăng nhập sau khi được duyệt.
- Cập nhật hồ sơ, logo, sản phẩm.

---

## 4. Phạm vi chức năng

### 4.1 API
- Đăng nhập và cấp JWT.
- Trả danh sách tour và location.
- Trả narration theo ngôn ngữ.
- Ghi nhận heartbeat thiết bị.
- Ghi nhận analytics event.
- Trả danh sách thiết bị đang hoạt động.

### 4.2 Admin
- Dashboard thống kê.
- CRUD category, location, tour, narration.
- Duyệt vendor.
- Monitor thiết bị và trạng thái online/stale/offline.

### 4.3 Vendor
- Đăng ký tài khoản.
- Đăng nhập.
- Cập nhật profile cửa hàng.
- Quản lý sản phẩm.
- Quản lý ảnh/logo.

### 4.4 Mobile App
- Tải tour/location từ API.
- Hiển thị bản đồ và POI.
- Phát audio hoặc TTS.
- Chọn ngôn ngữ hiển thị/narration.
- Gửi heartbeat và analytics.

---

## 5. Yêu cầu chi tiết

### 5.1 Mobile App
- Tải dữ liệu từ API thành công khi app khởi động hoặc `OnAppearing`.
- Hiển thị POI trên bản đồ.
- Hỗ trợ auto-play thuyết minh khi vào vùng POI nếu được bật.
- Chặn phát lặp khi người dùng vẫn còn ở trong vùng.
- Gửi heartbeat theo chu kỳ để admin monitor nhận biết thiết bị.
- Gửi analytics event khi mở app, mở màn hình, chọn ngôn ngữ, phát audio, quét QR.

### 5.2 Admin Monitor
- Hiển thị danh sách device và trạng thái theo heartbeat gần nhất.
- Hiển thị thông tin thiết bị: tên máy, model, OS, app version, pin, vị trí, thời gian heartbeat.
- Tự động phân loại `online`, `stale`, `offline` theo thời gian.

### 5.3 Vendor
- Đăng ký vendor với thông tin cửa hàng và liên hệ.
- Chờ admin duyệt trước khi login được.
- Cho phép sửa profile, upload logo, cập nhật địa chỉ và mô tả.

---

## 6. Yêu cầu phi chức năng

- **Tính ổn định:** retry queue cho heartbeat/analytics khi mạng yếu.
- **Tính nhất quán:** entity shared giữa các project phải đồng bộ với schema DB.
- **Bảo mật:** mật khẩu hash; admin/vendor auth riêng; không lưu secret trong source code.
- **Khả năng mở rộng:** nhiều thiết bị có thể cùng gửi heartbeat về hệ thống.
- **Hiệu năng:** phản hồi API đủ nhanh cho app mobile.

---

## 7. Luồng nghiệp vụ chính

### 7.1 Khách mở app
1. App tải tour/location từ API.
2. Map render POI.
3. App chọn narration theo ngôn ngữ hiện tại.
4. Nếu có `AudioUrl` thì phát audio, nếu không thì dùng TTS.
5. App gửi heartbeat và analytics về API.

### 7.2 Admin theo dõi thiết bị
1. App gửi heartbeat về `POST /api/monitor/heartbeat`.
2. API cập nhật hoặc tạo record thiết bị trong bảng `ActiveDevices`.
3. Admin mở trang Monitor để xem danh sách thiết bị.
4. Trang Monitor đọc dữ liệu từ `GET /api/monitor/active-devices`.

### 7.3 Vendor đăng ký và được duyệt
1. Vendor đăng ký gian hàng.
2. Tài khoản ở trạng thái pending.
3. Admin duyệt vendor.
4. Vendor login và quản lý cửa hàng/sản phẩm.

---

## 8. Dữ liệu chính

### 8.1 Shared entities
- `Vendor`
- `Product`
- `Tour`
- `TourLocation`
- `Location`
- `Narration`
- `Language`
- `Category`
- `ActiveDevice`
- `AnalyticsEvent`

### 8.2 Các điểm cần lưu ý
- Một số cột trong DB có thể là null.
- Model cần khớp schema hiện tại để tránh lỗi cast.
- Base URL API của app và heartbeat phải đồng nhất hoặc được cấu hình rõ ràng.

---

## 9. Rủi ro hiện tại

1. Sai base URL giữa app data và heartbeat.
2. Schema DB và entity dễ bị lệch khi merge nhiều nhánh.
3. Một số cột nullable trong DB có thể gây runtime exception nếu model không cho phép null.
4. Vendor/Admin/App dùng chung entity nên cần đồng bộ rất cẩn thận.

---

## 10. Tiêu chí hoàn thành

- App tải dữ liệu tour/location thành công.
- Admin Monitor nhận heartbeat và hiển thị thiết bị.
- Vendor đăng ký/login/approve chạy ổn.
- Không còn lỗi schema null/cột không tồn tại ở flow chính.
- Heartbeat, analytics, và monitor hoạt động được trên nhiều thiết bị.

---

## 11. Ghi chú triển khai

- Nên dùng một cấu hình base URL tập trung cho app.
- Nên có migration/backfill nếu DB có dữ liệu cũ thiếu cột mới.
- Khi cập nhật entity `Vendor`, phải kiểm tra toàn bộ Admin + Vendor + API.

---

## 12. Kết luận

PL-Tour tập trung vào trải nghiệm du lịch theo bản đồ, QR, audio đa ngôn ngữ, đồng thời cung cấp bộ công cụ quản trị và vendor để vận hành hệ thống. PRD này nên được cập nhật khi schema hoặc luồng nghiệp vụ thay đổi.
