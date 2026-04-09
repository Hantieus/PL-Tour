using PLTour.App.Models;
using Mapsui.Styles;
// Tạo alias giống như bên PoiModel để dùng màu sắc của Mapsui
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Services;

public class ApiService
{
    public async Task<List<TourModel>> GetMockToursAsync()
    {
        await Task.Delay(500); // Giả lập độ trễ mạng

        return new List<TourModel>
        {
            new TourModel
            {
                Id = "T01",
                Name = "Tour Khám Phá Trung Tâm",
                Duration = "3 tiếng 30 phút",
                IntroText = "Chào mừng đến với tour trung tâm, nơi bạn sẽ trải nghiệm văn hóa và lịch sử độc đáo của thành phố.",
                ImageUrl = "tour_central.jpg",
                Latitude = 10.779785,
                Longitude = 106.699019,
                Pois = new List<PoiModel>
                {
                    new PoiModel
                    {
                        Name = "Nhà Thờ Đức Bà",
                        Lat = 10.779785,
                        Lng = 106.699019,
                        Radius = 200,
                        Description = "Nhà thờ cổ kính mang kiến trúc Roman, biểu tượng của thành phố.",
                        Address = "01 Công xã Paris, Bến Nghé, Quận 1",
                        Category = PoiCategories.ThamQuan,
                        PinColor = MapsuiColor.Red
                    },
                    new PoiModel
                    {
                        Name = "Bưu Điện Thành Phố",
                        Lat = 10.779872,
                        Lng = 106.700028,
                        Radius = 150,
                        Description = "Công trình kiến trúc Pháp tuyệt đẹp được xây dựng từ thế kỷ 19.",
                        Address = "02 Công xã Paris, Bến Nghé, Quận 1",
                        Category = PoiCategories.ThamQuan,
                        PinColor = MapsuiColor.Blue
                    },
                    new PoiModel
                    {
                        Name = "Dinh Độc Lập",
                        Lat = 10.777089,
                        Lng = 106.695325,
                        Radius = 300,
                        Description = "Di tích lịch sử nổi tiếng, nơi lưu giữ nhiều hiện vật quý giá.",
                        Address = "135 Nam Kỳ Khởi Nghĩa, Bến Thành, Quận 1",
                        Category = PoiCategories.ThamQuan,
                        PinColor = MapsuiColor.Orange
                    },
                    new PoiModel
                    {
                        Name = "Bảo tàng Chứng tích Chiến tranh",
                        Lat = 10.779435,
                        Lng = 106.692223,
                        Radius = 250,
                        Description = "Bảo tàng lưu giữ những chứng tích anh hùng và bi tráng của dân tộc.",
                        Address = "28 Võ Văn Tần, Phường 6, Quận 3",
                        Category = PoiCategories.ThamQuan,
                        PinColor = MapsuiColor.DarkRed
                    }
                }
            },
            new TourModel
            {
                Id = "T02",
                Name = "Tour Ẩm Thực Về Đêm",
                Duration = "2 tiếng 30 phút",
                IntroText = "Cùng thưởng thức những món ăn đường phố ngon nhất Sài Gòn.",
                ImageUrl = "tour_food.jpg",
                Latitude = 10.773534,
                Longitude = 106.703273,
                Pois = new List<PoiModel>
                {
                    new PoiModel
                    {
                        Name = "Phố Đi Bộ Nguyễn Huệ",
                        Lat = 10.773534,
                        Lng = 106.703273,
                        Radius = 400,
                        Description = "Không gian sôi động về đêm với nhiều món ăn vặt và trà sữa.",
                        Address = "Nguyễn Huệ, Bến Nghé, Quận 1",
                        Category = PoiCategories.AnUong,
                        PinColor = MapsuiColor.Green
                    },
                    new PoiModel
                    {
                        Name = "Phố Tây Bùi Viện",
                        Lat = 10.767439,
                        Lng = 106.694017,
                        Radius = 300,
                        Description = "Khu phố nhộn nhịp xuyên đêm với các quán bar, pub và đồ nướng.",
                        Address = "Bùi Viện, Phạm Ngũ Lão, Quận 1",
                        Category = PoiCategories.AnUong,
                        PinColor = MapsuiColor.Green
                    },
                    new PoiModel
                    {
                        Name = "Khu Ẩm Thực Chợ Hồ Thị Kỷ",
                        Lat = 10.765662,
                        Lng = 106.678077,
                        Radius = 200,
                        Description = "Thiên đường ẩm thực đường phố với hàng trăm món ngon các vùng miền.",
                        Address = "Hồ Thị Kỷ, Phường 1, Quận 10",
                        Category = PoiCategories.AnUong,
                        PinColor = MapsuiColor.Yellow
                    }
                }
            },
            new TourModel
            {
                Id = "T03",
                Name = "Tour Giải Trí & Sự Kiện",
                Duration = "4 tiếng",
                IntroText = "Tham gia các hoạt động giải trí và sự kiện văn hóa nghệ thuật đặc sắc.",
                ImageUrl = "tour_event.jpg",
                Latitude = 10.776615,
                Longitude = 106.703144,
                Pois = new List<PoiModel>
                {
                    new PoiModel
                    {
                        Name = "Nhà Hát Thành Phố",
                        Lat = 10.776615,
                        Lng = 106.703144,
                        Radius = 150,
                        Description = "Nơi tổ chức các buổi hòa nhạc, múa ballet và sự kiện nghệ thuật lớn.",
                        Address = "07 Công Trường Lam Sơn, Bến Nghé, Quận 1",
                        Category = PoiCategories.SuKien,
                        PinColor = MapsuiColor.Purple
                    },
                    new PoiModel
                    {
                        Name = "Sân vận động Thống Nhất",
                        Lat = 10.760183,
                        Lng = 106.659972,
                        Radius = 500,
                        Description = "Địa điểm diễn ra các trận bóng đá nảy lửa và các đại nhạc hội ngoài trời.",
                        Address = "138 Đào Duy Từ, Phường 6, Quận 10",
                        Category = PoiCategories.SuKien,
                        PinColor = MapsuiColor.Purple
                    },
                    new PoiModel
                    {
                        Name = "Landmark 81",
                        Lat = 10.795051,
                        Lng = 106.721832,
                        Radius = 600,
                        Description = "Tòa nhà cao nhất Việt Nam, nơi có trượt băng và rạp chiếu phim IMAX.",
                        Address = "720A Điện Biên Phủ, Vinhomes Tân Cảng, Bình Thạnh",
                        Category = PoiCategories.SuKien,
                        PinColor = MapsuiColor.Cyan
                    }
                }
            }
        };
    }
}