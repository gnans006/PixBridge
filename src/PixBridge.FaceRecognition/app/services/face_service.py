import json
import logging
from typing import List

import cv2
import numpy as np

from app.core.config import settings
from app.models.schemas import FaceResult
from app.services.model_loader import get_face_app

logger = logging.getLogger(__name__)


def index_photo(image_path: str) -> List[FaceResult]:
    """
    Detect all faces in the given image file and return one FaceResult per face.
    Each FaceResult contains:
      - embedding: 512-dim ArcFace vector (normalised to unit length)
      - bounding_box: JSON {x, y, width, height}
      - confidence: face detection confidence score
    """
    if not image_path or not __import__("os").path.exists(image_path):
        raise FileNotFoundError(f"Image not found: {image_path}")

    img = cv2.imread(image_path)
    if img is None:
        raise ValueError(f"Could not decode image: {image_path}")

    faces = get_face_app().get(img)
    results: List[FaceResult] = []

    for face in faces:
        if face.det_score < settings.min_face_confidence:
            continue
        if face.embedding is None:
            continue

        # Normalise embedding to unit length for cosine similarity
        emb = face.embedding.astype(np.float32)
        norm = np.linalg.norm(emb)
        if norm > 0:
            emb = emb / norm

        box = face.bbox.astype(int)
        bounding_box = json.dumps({
            "x": int(box[0]),
            "y": int(box[1]),
            "width": int(box[2] - box[0]),
            "height": int(box[3] - box[1]),
        })

        results.append(FaceResult(
            embedding=emb.tolist(),
            bounding_box=bounding_box,
            confidence=float(face.det_score),
        ))

    logger.debug("Detected %d face(s) in %s", len(results), image_path)
    return results


def generate_embedding_from_bytes(image_bytes: bytes) -> List[float]:
    """
    Generate a single 512-dim embedding from a selfie image.
    Raises ValueError if no face is detected.
    """
    nparr = np.frombuffer(image_bytes, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if img is None:
        raise ValueError("Could not decode selfie image.")

    faces = get_face_app().get(img)
    valid = [f for f in faces if f.det_score >= settings.min_face_confidence and f.embedding is not None]

    if not valid:
        raise ValueError("No face detected in the selfie. Please try again with a clearer photo.")

    # Use the highest-confidence face
    best = max(valid, key=lambda f: f.det_score)
    emb = best.embedding.astype(np.float32)
    norm = np.linalg.norm(emb)
    if norm > 0:
        emb = emb / norm

    return emb.tolist()
