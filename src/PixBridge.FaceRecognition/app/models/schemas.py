from pydantic import BaseModel, Field
from typing import List, Optional


class BoundingBox(BaseModel):
    x: int
    y: int
    width: int
    height: int


class FaceResult(BaseModel):
    embedding: List[float] = Field(..., description="512-dimensional ArcFace embedding vector")
    bounding_box: str = Field(..., description="JSON string of face bounding box")
    confidence: float = Field(..., ge=0.0, le=1.0)


class IndexPhotoRequest(BaseModel):
    image_path: str = Field(..., description="Absolute path to the photo on disk")


class IndexPhotoResponse(BaseModel):
    face_count: int
    faces: List[FaceResult]


class EmbeddingResponse(BaseModel):
    embedding: List[float] = Field(..., description="512-dimensional ArcFace embedding vector")


class SearchRequest(BaseModel):
    event_id: str
    embedding: List[float] = Field(..., description="Selfie embedding to search against")
    threshold: float = Field(default=0.75, ge=0.0, le=1.0)
    top_k: int = Field(default=200, ge=1, le=500)


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    version: str = "1.0.0"
