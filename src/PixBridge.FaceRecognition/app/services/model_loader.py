import logging
import json
from typing import List, Tuple

import numpy as np
from insightface.app import FaceAnalysis

from app.core.config import settings

logger = logging.getLogger(__name__)

# Module-level singleton — loaded once on startup
_face_app: FaceAnalysis | None = None


def get_face_app() -> FaceAnalysis:
    global _face_app
    if _face_app is None:
        raise RuntimeError("FaceAnalysis model not initialised. Call load_model() first.")
    return _face_app


def load_model() -> None:
    """Load InsightFace ArcFace model. Called during FastAPI startup."""
    global _face_app
    logger.info("Loading InsightFace model '%s'...", settings.insightface_model)
    app = FaceAnalysis(
        name=settings.insightface_model,
        allowed_modules=["detection", "recognition"],
    )
    app.prepare(ctx_id=settings.insightface_ctx_id, det_size=settings.det_size)
    _face_app = app
    logger.info("InsightFace model loaded successfully.")


def is_model_loaded() -> bool:
    return _face_app is not None
