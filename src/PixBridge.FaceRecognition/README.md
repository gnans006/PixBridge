# PixBridge.FaceRecognition

Offline FastAPI service providing face detection and ArcFace embedding generation
for the PixBridge photo-sharing platform.

## Stack
- **Python 3.11+**
- **FastAPI** + Uvicorn
- **InsightFace** (buffalo_l model — ArcFace, 512-dim embeddings)
- **OpenCV** (image decode)
- Runs locally on the same machine as the .NET API

## Setup

```bash
# 1. Create virtual environment
python -m venv .venv
.venv\Scripts\activate

# 2. Install dependencies
pip install -r requirements.txt

# 3. Run the service
python run.py
```

Service starts on **http://localhost:8080**.

InsightFace downloads the `buffalo_l` model automatically on first run (~300 MB).

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Service health + model status |
| POST | /index-photo | Detect faces + generate embeddings from a file path |
| POST | /generate-embedding | Generate embedding from selfie upload (multipart) |

## Configuration

Environment variables (prefix: `PIXBRIDGE_FR_`):

| Variable | Default | Description |
|----------|---------|-------------|
| `PIXBRIDGE_FR_PORT` | 8080 | Listening port |
| `PIXBRIDGE_FR_INSIGHTFACE_MODEL` | buffalo_l | InsightFace model name |
| `PIXBRIDGE_FR_INSIGHTFACE_CTX_ID` | -1 | -1=CPU, 0=GPU |
| `PIXBRIDGE_FR_MIN_FACE_CONFIDENCE` | 0.5 | Minimum detection confidence |

## Windows Service (Production)

```bat
nssm install PixBridgeFaceRecognition "C:\Python311\python.exe" "C:\PixBridge\face_recognition\run.py"
nssm set PixBridgeFaceRecognition AppDirectory "C:\PixBridge\face_recognition"
nssm start PixBridgeFaceRecognition
```
