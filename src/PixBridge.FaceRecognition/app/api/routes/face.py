import logging
from fastapi import APIRouter, HTTPException, UploadFile, File, status

from app.models.schemas import (
    IndexPhotoRequest,
    IndexPhotoResponse,
    FaceResult,
    EmbeddingResponse,
    HealthResponse,
)
from app.services.face_service import index_photo, generate_embedding_from_bytes
from app.services.model_loader import is_model_loaded
from app.core.config import settings

router = APIRouter()
logger = logging.getLogger(__name__)


@router.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    """Returns service health and model status."""
    return HealthResponse(
        status="ok" if is_model_loaded() else "model_not_loaded",
        model_loaded=is_model_loaded(),
        version=settings.app_version,
    )


@router.post(
    "/index-photo",
    response_model=IndexPhotoResponse,
    status_code=status.HTTP_200_OK,
    tags=["Face Recognition"],
    summary="Detect and embed all faces in an image file",
)
async def index_photo_endpoint(request: IndexPhotoRequest):
    """
    Reads the image at `image_path` from disk, detects all faces,
    generates a 512-dim ArcFace embedding per face, and returns them.
    Called by the .NET FaceIndexingService worker.
    """
    try:
        faces = index_photo(request.image_path)
        return IndexPhotoResponse(face_count=len(faces), faces=faces)
    except FileNotFoundError as e:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(e))
    except Exception as e:
        logger.exception("Error in /index-photo: %s", e)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=str(e))


@router.post(
    "/generate-embedding",
    response_model=EmbeddingResponse,
    status_code=status.HTTP_200_OK,
    tags=["Face Recognition"],
    summary="Generate a 512-dim embedding from a selfie upload",
)
async def generate_embedding_endpoint(selfie: UploadFile = File(...)):
    """
    Accepts a selfie as multipart/form-data and returns a 512-dim ArcFace embedding.
    Called when a guest uploads their selfie to start a face-search session.
    """
    if not selfie.content_type or not selfie.content_type.startswith("image/"):
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="File must be an image (JPEG, PNG, etc.).",
        )

    image_bytes = await selfie.read()
    if len(image_bytes) == 0:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Empty file uploaded.")

    try:
        embedding = generate_embedding_from_bytes(image_bytes)
        return EmbeddingResponse(embedding=embedding)
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail=str(e))
    except Exception as e:
        logger.exception("Error in /generate-embedding: %s", e)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=str(e))
