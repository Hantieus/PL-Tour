using PLTourApp.Models;
using PLTourApp.Services;
using PLTourApp.Database;

namespace PLTourApp.Engines;

public class NarrationEngine
{
    AudioService audio;
    TTSService tts;
    SQLiteHelper db;

    Queue<Poi> queue = new();

    bool isPlaying = false;

    public NarrationEngine(
        AudioService audioService,
        TTSService ttsService,
        SQLiteHelper database)
    {
        audio = audioService;
        tts = ttsService;
        db = database;
    }

    public async Task Enqueue(Poi poi)
    {
        bool playedRecently = await db.WasPlayedRecently(poi.Id);

        if (playedRecently)
            return;

        queue.Enqueue(poi);

        if (!isPlaying)
        {
            await PlayNext();
        }
    }

    async Task PlayNext()
    {
        if (queue.Count == 0)
        {
            isPlaying = false;
            return;
        }

        isPlaying = true;

        var poi = queue.Dequeue();

        await PlayPoi(poi);

        await PlayNext();
    }

    async Task PlayPoi(Poi poi)
    {
        try
        {
            // Nếu có file audio thì phát
            if (!string.IsNullOrEmpty(poi.AudioFile))
            {
                await audio.Play(poi.AudioFile);
            }
            else
            {
                // nếu không có audio thì dùng TTS
                await tts.Speak(poi.TtsScript ?? "Địa điểm du lịch", poi.Language ?? "vi");
            }

            // ghi log đã phát
            await db.InsertLog(new PlayLog
            {
                PoiId = poi.Id,
                PlayedAt = DateTime.Now
            });
        }
        catch
        {
            // tránh crash app nếu audio lỗi
        }
    }

    // hàm test audio nhanh
    public async Task TestAudio()
    {
        await audio.Play("poi1.mp3");
    }

    public void Stop()
    {
        audio.Stop();
        queue.Clear();
        isPlaying = false;
    }
}