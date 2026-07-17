import os
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    app_name: str = "PixBridge Face Recognition"
    app_version: str = "1.0.0"
    host: str = "0.0.0.0"
    port: int = 8080

    # InsightFace model name — buffalo_l provides ArcFace (512-dim embeddings)
    insightface_model: str = "buffalo_l"
    insightface_ctx_id: int = -1  # -1 = CPU; 0 = first GPU

    # Detection settings
    det_size: tuple = (640, 640)
    min_face_confidence: float = 0.5

    class Config:
        env_prefix = "PIXBRIDGE_FR_"


settings = Settings()
