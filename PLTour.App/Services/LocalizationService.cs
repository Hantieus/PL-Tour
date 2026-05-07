using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace PLTour.App.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["vi"] = new Dictionary<string, string>
        {
            ["Home"] = "Trang chủ",
            ["MapFreeMode"] = "Chế độ tự do",
            ["MapTourMode"] = "Chế độ theo tour",
            ["MapQrResult"] = "Kết quả từ QR",
            ["MapNearbyPlaces"] = "Địa điểm gần bạn",
            ["MapFilteringQrPoi"] = "Đang lọc theo POI quét được",
            ["MapChooseTabHint"] = "Chọn tab để lọc",
            ["MapTourNotFound"] = "Không tìm thấy tour",
            ["FarAwayTitle"] = "Thông báo",
            ["FarAwayMessage"] = "{0} đang cách bạn khoảng {1} m. Bạn vẫn muốn nghe không?",
            ["Listen"] = "Nghe",
            ["Stop"] = "Dừng",
            ["Cancel"] = "Hủy",
            ["NoNarrationContent"] = "Không có nội dung thuyết minh.",
            ["Map"] = "Bản đồ",
            ["QR Scan"] = "Quét QR",
            ["Settings"] = "Cài đặt",
            ["Details"] = "Chi tiết",
            ["SearchPlaceHolder"] = "Tìm kiếm địa điểm...",
            ["Expand"] = "Mở rộng",
            ["Collapse"] = "Thu gọn",
            ["NoPoiMatch"] = "Chưa có địa điểm phù hợp",
            ["Selected"] = "Đang chọn",
            ["MapButton"] = "📍 Bản đồ",
            ["GoButton"] = "🧭 Đi",
            ["PoiInfoTitle"] = "📍 Thông tin địa điểm",
            ["Address"] = "Địa chỉ",
            ["Category"] = "Danh mục",
            ["Distance"] = "Khoảng cách",
            ["PoiContentTitle"] = "📖 Nội dung chi tiết",
            ["ViewMap"] = "Xem bản đồ",
            ["Close"] = "✕",
            ["HomeGreeting"] = "Xin chào",
            ["HomeSubtitle"] = "Khám phá Việt Nam cùng PL Tour",
            ["HomeExploreTitle"] = "Đi tự do",
            ["HomeExploreDesc"] = "Xem các điểm tham quan quanh đây trên bản đồ",
            ["HomeOpenMap"] = "Mở Bản Đồ",
            ["HomeSuggestedTours"] = "Tour gợi ý gần bạn nhất",
            ["DistanceFromYou"] = "Cách bạn: {0} km",
            ["LocationUnknown"] = "Vị trí chưa xác định",
            ["Calculating"] = "Đang tính...",
            ["Updating"] = "Đang cập nhật",
            ["DurationHoursMinutes"] = "{0} tiếng {1} phút",
            ["DurationHours"] = "{0} tiếng",
            ["DurationMinutes"] = "{0} phút",
            ["Error"] = "Lỗi",
            ["CannotLoadTourData"] = "Không thể kết nối đến máy chủ để tải dữ liệu tour.",
            ["Information"] = "Thông báo",
            ["NoNarrationYet"] = "Tour này hiện chưa có nội dung thuyết minh.",
            ["OK"] = "OK",
            ["TourDetailTitle"] = "Chi tiết Tour",
            ["TourMapView"] = "Xem bản đồ",
            ["TourSpots"] = "Tham quan",
            ["TourFood"] = "Ăn uống",
            ["TourEvents"] = "Sự kiện",
            ["TourLocations"] = "Các địa điểm trong Tour",
            ["QrScannerTitle"] = "Quét QR",
            ["QrScannerHint"] = "Đưa mã QR vào giữa khung",
            ["QrReady"] = "Sẵn sàng quét...",
            ["QrRescan"] = "Quét lại",
            ["CategoryAll"] = "Tất cả",
            ["CategoryTourism"] = "Tham quan",
            ["CategoryFood"] = "Ăn uống",
            ["CategoryEvent"] = "Sự kiện",
            ["MapTourNotFound"] = "Không tìm thấy tour",
            ["FarAwayTitle"] = "Bạn đang ở xa",
            ["FarAwayMessage"] = "Bạn cách {0} khoảng {1}m. Bạn có muốn nghe thuyết minh ảo từ xa không?",
            ["Cancel"] = "Hủy bỏ",
            ["NoNarrationContent"] = "Không có thông tin thuyết minh.",
        },
        ["en"] = new Dictionary<string, string>
        {
            ["Home"] = "Home",
            ["MapFreeMode"] = "Free mode",
            ["MapTourMode"] = "Tour mode",
            ["MapQrResult"] = "QR result",
            ["MapNearbyPlaces"] = "Nearby places",
            ["MapFilteringQrPoi"] = "Filtering scanned POIs",
            ["MapChooseTabHint"] = "Choose a tab to filter",
            ["MapTourNotFound"] = "Tour not found",
            ["FarAwayTitle"] = "Notice",
            ["FarAwayMessage"] = "{0} is about {1} m away from you. Do you still want to listen?",
            ["Listen"] = "Listen",
            ["Stop"] = "Stop",
            ["Cancel"] = "Cancel",
            ["NoNarrationContent"] = "No narration content available.",
            ["Map"] = "Map",
            ["QR Scan"] = "QR Scan",
            ["Settings"] = "Settings",
            ["Details"] = "Details",
            ["SearchPlaceHolder"] = "Search places...",
            ["Expand"] = "Expand",
            ["Collapse"] = "Collapse",
            ["NoPoiMatch"] = "No matching locations yet",
            ["Selected"] = "Selected",
            ["MapButton"] = "📍 Map",
            ["GoButton"] = "🧭 Go",
            ["PoiInfoTitle"] = "📍 Location information",
            ["Address"] = "Address",
            ["Category"] = "Category",
            ["Distance"] = "Distance",
            ["PoiContentTitle"] = "📖 Details",
            ["ViewMap"] = "View map",
            ["Close"] = "✕",
            ["HomeGreeting"] = "Hello",
            ["HomeSubtitle"] = "Explore Vietnam with PL Tour",
            ["HomeExploreTitle"] = "Free roaming",
            ["HomeExploreDesc"] = "See nearby attractions on the map",
            ["HomeOpenMap"] = "Open Map",
            ["HomeSuggestedTours"] = "Recommended tours near you",
            ["DistanceFromYou"] = "{0} km from you",
            ["LocationUnknown"] = "Location unknown",
            ["Calculating"] = "Calculating...",
            ["Updating"] = "Updating...",
            ["DurationHoursMinutes"] = "{0}h {1}m",
            ["DurationHours"] = "{0}h",
            ["DurationMinutes"] = "{0}m",
            ["Error"] = "Error",
            ["CannotLoadTourData"] = "Unable to connect to the server to load tour data.",
            ["Information"] = "Information",
            ["NoNarrationYet"] = "This tour does not have narration content yet.",
            ["OK"] = "OK",
            ["TourDetailTitle"] = "Tour details",
            ["TourMapView"] = "View map",
            ["TourSpots"] = "Sightseeing",
            ["TourFood"] = "Food",
            ["TourEvents"] = "Events",
            ["TourLocations"] = "Tour locations",
            ["QrScannerTitle"] = "QR Scanner",
            ["QrScannerHint"] = "Place the QR code in the center of the frame",
            ["QrReady"] = "Ready to scan...",
            ["QrRescan"] = "Scan again",
            ["CategoryAll"] = "All",
            ["CategoryTourism"] = "Sightseeing",
            ["CategoryFood"] = "Food",
            ["CategoryEvent"] = "Events",
            ["MapTourNotFound"] = "Tour not found",
            ["FarAwayTitle"] = "You are far away",
            ["FarAwayMessage"] = "You are about {1}m away from {0}. Do you want to listen to the remote narration?",
            ["NoNarrationContent"] = "No narration content available.",
        },
        ["zh"] = new Dictionary<string, string>
        {
            ["Home"] = "首页",
            ["MapFreeMode"] = "自由模式",
            ["MapTourMode"] = "行程模式",
            ["MapQrResult"] = "二维码结果",
            ["MapNearbyPlaces"] = "附近地点",
            ["MapFilteringQrPoi"] = "正在筛选扫描到的地点",
            ["MapChooseTabHint"] = "选择标签进行筛选",
            ["MapTourNotFound"] = "未找到行程",
            ["FarAwayTitle"] = "提示",
            ["FarAwayMessage"] = "{0} 距离您约 {1} 米。仍要收听吗？",
            ["Listen"] = "收听",
            ["Stop"] = "停止",
            ["Cancel"] = "取消",
            ["NoNarrationContent"] = "没有可用的讲解内容。",
            ["Map"] = "地图",
            ["QR Scan"] = "扫码",
            ["Settings"] = "设置",
            ["Details"] = "详情",
            ["SearchPlaceHolder"] = "搜索地点...",
            ["Expand"] = "展开",
            ["Collapse"] = "收起",
            ["NoPoiMatch"] = "未找到匹配地点",
            ["Selected"] = "已选中",
            ["MapButton"] = "📍 地图",
            ["GoButton"] = "🧭 前往",
            ["PoiInfoTitle"] = "📍 地点信息",
            ["Address"] = "地址",
            ["Category"] = "分类",
            ["Distance"] = "距离",
            ["PoiContentTitle"] = "📖 详细内容",
            ["ViewMap"] = "查看地图",
            ["Close"] = "✕",
            ["HomeGreeting"] = "你好",
            ["HomeSubtitle"] = "与 PL Tour 一起探索越南",
            ["HomeExploreTitle"] = "自由探索",
            ["HomeExploreDesc"] = "在地图上查看附近景点",
            ["HomeOpenMap"] = "打开地图",
            ["HomeSuggestedTours"] = "推荐行程",
            ["DistanceFromYou"] = "距您 {0} 公里",
            ["LocationUnknown"] = "位置未知",
            ["Calculating"] = "计算中...",
            ["Updating"] = "更新中...",
            ["DurationHoursMinutes"] = "{0}小时{1}分钟",
            ["DurationHours"] = "{0}小时",
            ["DurationMinutes"] = "{0}分钟",
            ["Error"] = "错误",
            ["CannotLoadTourData"] = "无法连接到服务器以加载行程数据。",
            ["Information"] = "信息",
            ["NoNarrationYet"] = "该行程暂无讲解内容。",
            ["OK"] = "确定",
            ["TourDetailTitle"] = "行程详情",
            ["TourMapView"] = "查看地图",
            ["TourSpots"] = "景点",
            ["TourFood"] = "美食",
            ["TourEvents"] = "活动",
            ["TourLocations"] = "行程地点",
            ["QrScannerTitle"] = "扫码",
            ["QrScannerHint"] = "将二维码放入框内",
            ["QrReady"] = "准备扫描...",
            ["QrRescan"] = "重新扫描",
            ["CategoryAll"] = "全部",
            ["CategoryTourism"] = "景点",
            ["CategoryFood"] = "美食",
            ["CategoryEvent"] = "活动",
            ["FarAwayMessage"] = "您距离 {0} 约 {1} 米。仍要收听远程讲解吗？",
        },
        ["ko"] = new Dictionary<string, string>
        {
            ["Home"] = "홈",
            ["MapFreeMode"] = "자유 모드",
            ["MapTourMode"] = "투어 모드",
            ["MapQrResult"] = "QR 결과",
            ["MapNearbyPlaces"] = "주변 장소",
            ["MapFilteringQrPoi"] = "스캔한 POI를 필터링 중",
            ["MapChooseTabHint"] = "탭을 선택해 필터링하세요",
            ["MapTourNotFound"] = "투어를 찾을 수 없습니다",
            ["FarAwayTitle"] = "알림",
            ["FarAwayMessage"] = "{0}이(가) 당신에게 약 {1}m 떨어져 있습니다. 계속 듣겠습니까?",
            ["Listen"] = "듣기",
            ["Stop"] = "중지",
            ["Cancel"] = "취소",
            ["NoNarrationContent"] = "사용 가능한 해설 내용이 없습니다.",
            ["Map"] = "지도",
            ["QR Scan"] = "QR 스캔",
            ["Settings"] = "설정",
            ["Details"] = "상세",
            ["SearchPlaceHolder"] = "장소 검색...",
            ["Expand"] = "확장",
            ["Collapse"] = "축소",
            ["NoPoiMatch"] = "일치하는 장소가 없습니다",
            ["Selected"] = "선택됨",
            ["MapButton"] = "📍 지도",
            ["GoButton"] = "🧭 이동",
            ["PoiInfoTitle"] = "📍 장소 정보",
            ["Address"] = "주소",
            ["Category"] = "분류",
            ["Distance"] = "거리",
            ["PoiContentTitle"] = "📖 상세 내용",
            ["ViewMap"] = "지도 보기",
            ["Close"] = "✕",
            ["HomeGreeting"] = "안녕하세요",
            ["HomeSubtitle"] = "PL Tour와 함께 베트남을 탐험하세요",
            ["HomeExploreTitle"] = "자유 탐험",
            ["HomeExploreDesc"] = "지도에서 주변 명소를 확인하세요",
            ["HomeOpenMap"] = "지도 열기",
            ["HomeSuggestedTours"] = "추천 투어",
            ["DistanceFromYou"] = "당신으로부터 {0}km",
            ["LocationUnknown"] = "위치 알 수 없음",
            ["Calculating"] = "계산 중...",
            ["Updating"] = "업데이트 중...",
            ["DurationHoursMinutes"] = "{0}시간 {1}분",
            ["DurationHours"] = "{0}시간",
            ["DurationMinutes"] = "{0}분",
            ["Error"] = "오류",
            ["CannotLoadTourData"] = "서버에 연결하여 투어 데이터를 불러올 수 없습니다.",
            ["Information"] = "정보",
            ["NoNarrationYet"] = "이 투어에는 아직 해설 내용이 없습니다.",
            ["OK"] = "확인",
            ["TourDetailTitle"] = "투어 상세",
            ["TourMapView"] = "지도 보기",
            ["TourSpots"] = "관광",
            ["TourFood"] = "음식",
            ["TourEvents"] = "이벤트",
            ["TourLocations"] = "투어 장소",
            ["QrScannerTitle"] = "QR 스캔",
            ["QrScannerHint"] = "QR 코드를 프레임 안에 넣으세요",
            ["QrReady"] = "스캔 준비 완료...",
            ["QrRescan"] = "다시 스캔",
            ["CategoryAll"] = "전체",
            ["CategoryTourism"] = "관광",
            ["CategoryFood"] = "음식",
            ["CategoryEvent"] = "이벤트",
            ["FarAwayMessage"] = "{0}은(는) 당신에게 약 {1}m 떨어져 있습니다. 원격 해설을 계속 들으시겠습니까?",
        },
        ["ja"] = new Dictionary<string, string>
        {
            ["Home"] = "ホーム",
            ["MapFreeMode"] = "自由モード",
            ["MapTourMode"] = "ツアーモード",
            ["MapQrResult"] = "QR結果",
            ["MapNearbyPlaces"] = "近くの場所",
            ["MapFilteringQrPoi"] = "スキャンしたPOIを絞り込み中",
            ["MapChooseTabHint"] = "タブを選んで絞り込み",
            ["MapTourNotFound"] = "ツアーが見つかりません",
            ["FarAwayTitle"] = "お知らせ",
            ["FarAwayMessage"] = "{0} はあなたから約 {1}m 離れています。このまま聞きますか？",
            ["Listen"] = "聞く",
            ["Stop"] = "停止",
            ["Cancel"] = "キャンセル",
            ["NoNarrationContent"] = "利用可能な解説コンテンツがありません。",
            ["Map"] = "地図",
            ["QR Scan"] = "QRスキャン",
            ["Settings"] = "設定",
            ["Details"] = "詳細",
            ["SearchPlaceHolder"] = "場所を検索...",
            ["Expand"] = "展開",
            ["Collapse"] = "折りたたむ",
            ["NoPoiMatch"] = "一致する場所がありません",
            ["Selected"] = "選択中",
            ["MapButton"] = "📍 地図",
            ["GoButton"] = "🧭 行く",
            ["PoiInfoTitle"] = "📍 場所情報",
            ["Address"] = "住所",
            ["Category"] = "カテゴリ",
            ["Distance"] = "距離",
            ["PoiContentTitle"] = "📖 詳細内容",
            ["ViewMap"] = "地図を見る",
            ["Close"] = "✕",
            ["HomeGreeting"] = "こんにちは",
            ["HomeSubtitle"] = "PL Tourでベトナムを探索しよう",
            ["HomeExploreTitle"] = "自由に探索",
            ["HomeExploreDesc"] = "地図で近くの名所を確認",
            ["HomeOpenMap"] = "地図を開く",
            ["HomeSuggestedTours"] = "おすすめツアー",
            ["DistanceFromYou"] = "あなたから {0} km",
            ["LocationUnknown"] = "位置が不明",
            ["Calculating"] = "計算中...",
            ["Updating"] = "更新中...",
            ["DurationHoursMinutes"] = "{0}時間{1}分",
            ["DurationHours"] = "{0}時間",
            ["DurationMinutes"] = "{0}分",
            ["Error"] = "エラー",
            ["CannotLoadTourData"] = "ツアーデータを読み込むためにサーバーへ接続できません。",
            ["Information"] = "情報",
            ["NoNarrationYet"] = "このツアーにはまだ解説内容がありません。",
            ["OK"] = "OK",
            ["TourDetailTitle"] = "ツアー詳細",
            ["TourMapView"] = "地図を見る",
            ["TourSpots"] = "観光",
            ["TourFood"] = "食事",
            ["TourEvents"] = "イベント",
            ["TourLocations"] = "ツアーの場所",
            ["QrScannerTitle"] = "QRスキャン",
            ["QrScannerHint"] = "QRコードを枠の中央に置いてください",
            ["QrReady"] = "スキャン準備完了...",
            ["QrRescan"] = "再スキャン",
            ["CategoryAll"] = "すべて",
            ["CategoryTourism"] = "観光",
            ["CategoryFood"] = "食事",
            ["CategoryEvent"] = "イベント",
            ["FarAwayMessage"] = "{0} はあなたから約 {1}m 離れています。リモート解説を聞き続けますか？",
        }
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public string CurrentLanguageCode => NormalizeLanguageCode(_currentCulture.Name);

    public void ApplyLanguage(string languageCode, bool persist = true)
    {
        languageCode = NormalizeLanguageCode(languageCode);
        if (!_translations.ContainsKey(languageCode))
            languageCode = "vi";

        var culture = new CultureInfo(languageCode);
        _currentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        if (persist)
            Preferences.Default.Set("UserLanguage", languageCode);

        OnPropertyChanged(string.Empty);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string this[string key] => Translate(key);

    public string Translate(string key)
    {
        var lang = CurrentLanguageCode;
        if (_translations.TryGetValue(lang, out var langMap) && langMap.TryGetValue(key, out var value))
            return value;

        if (_translations.TryGetValue("vi", out var fallbackMap) && fallbackMap.TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return "vi";

        languageCode = languageCode.Trim().ToLowerInvariant();
        if (languageCode.StartsWith("zh")) return "zh";
        if (languageCode.StartsWith("ko")) return "ko";
        if (languageCode.StartsWith("ja")) return "ja";
        if (languageCode.StartsWith("en")) return "en";
        return "vi";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
