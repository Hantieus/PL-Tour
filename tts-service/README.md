# Coqui TTS Service

Dịch vụ TTS self-host để dùng với `PLTour.API`.

## Chạy local

```bash
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 5005
```

## API

- `GET /health`
- `POST /tts`

Body mẫu:

```json
{
  "text": "Xin chào",
  "langCode": "vi"
}
```

## Docker

```bash
docker build -t pltour-tts .
docker run -p 5005:5005 pltour-tts
```
