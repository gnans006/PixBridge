import json
import logging
import math
from typing import List, Optional

import cv2
import numpy as np

from app.core.config import settings
from app.models.schemas import FaceResult
from app.services.model_loader import get_face_app

logger = logging.getLogger(__name__)

# Quality scoring constants
MIN_FACE_SIZE_RATIO = 0.02   # face must be at least 2% of total pixels
GOOD_FACE_SIZE_RATIO = 0.05  # 5% = full quality size points
MAX_POSE_ANGLE = 35.0        # degrees; beyond this = quality penalty


def _compute_quality_score(
    confidence: float,
    face_area: int,
    image_total_pixels: int,
    pose: Optional[np.ndarray],
) -> float:
    """
    Compute composite quality score (0–100) for a single detected face.

    Breakdown:
      - Detection confidence: 0–50 pts
      - Face size ratio:      0–25 pts
      - Pose angles:          0–25 pts (12 pts if no pose data)
    """
    score = 0.0

    # Component 1: Detection confidence
    score += min(confidence * 50.0, 50.0)

    # Component 2: Face size ratio
    if image_total_pixels > 0:
        ratio = face_area / image_total_pixels
        if ratio >= GOOD_FACE_SIZE_RATIO:
            score += 25.0
        elif ratio >= MIN_FACE_SIZE_RATIO:
            score += (ratio / GOOD_FACE_SIZE_RATIO) * 25.0
        # < MIN → no points (face too small)

    # Component 3: Pose angles
    if pose is not None and len(pose) >= 3:
        max_angle = max(abs(pose[0]), abs(pose[1]), abs(pose[2]))
        if max_angle <= MAX_POSE_ANGLE:
            score += 25.0 * (1.0 - max_angle / MAX_POSE_ANGLE)
        # beyond max_angle → no points
    else:
        # No pose data available — grant partial credit
        score += 12.0

    return round(min(max(score, 0.0), 100.0), 1)


def index_photo(image_path: str) -> List[FaceResult]:
    """
    Detect all faces in the given image file and return one FaceResult per face.
    Each FaceResult now includes quality_score and pose_angles for the .NET quality service.
    """
    import os
    if not image_path or not os.path.exists(image_path):
        raise FileNotFoundError(f"Image not found: {image_path}")

    img = cv2.imread(image_path)
    if img is None:
        raise ValueError(f"Could not decode image: {image_path}")

    h_img, w_img = img.shape[:2]
    image_total_pixels = h_img * w_img

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
        face_w = max(int(box[2] - box[0]), 1)
        face_h = max(int(box[3] - box[1]), 1)
        face_area = face_w * face_h

        bounding_box = json.dumps({
            "x": int(box[0]),
            "y": int(box[1]),
            "width": face_w,
            "height": face_h,
        })

        # Extract pose angles (yaw, pitch, roll) if available from InsightFace
        pose_angles: Optional[List[float]] = None
        if hasattr(face, "pose") and face.pose is not None:
            pose_angles = [float(face.pose[0]), float(face.pose[1]), float(face.pose[2])]

        quality_score = _compute_quality_score(
            float(face.det_score),
            face_area,
            image_total_pixels,
            np.array(pose_angles) if pose_angles else None,
        )

        results.append(FaceResult(
            embedding=emb.tolist(),
            bounding_box=bounding_box,
            confidence=float(face.det_score),
            quality_score=quality_score,
            pose_angles=pose_angles,
        ))

    logger.debug("Detected %d face(s) in %s (total pixels=%d)", len(results), image_path, image_total_pixels)
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

