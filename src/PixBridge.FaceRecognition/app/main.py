import logging
import time

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager

from app.api.routes import face
from app.core.config import settings
from app.services.model_loader import load_model

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s - %(message)s",
)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Load InsightFace model on startup; release resources on shutdown."""
    load_model()
    logger.info("PixBridge FaceRecognition service started on port %s", settings.port)
    yield
    logger.info("PixBridge FaceRecognition service shutting down.")


app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    description="Offline face detection and ArcFace embedding service for PixBridge.",
    lifespan=lifespan,
)

# Allow .NET API on the same machine to call this service
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5000", "http://127.0.0.1:5000"],
    allow_methods=["GET", "POST"],
    allow_headers=["*"],
)


@app.middleware("http")
async def add_request_timing(request: Request, call_next):
    start = time.perf_counter()
    response = await call_next(request)
    duration_ms = (time.perf_counter() - start) * 1000
    response.headers["X-Process-Time-Ms"] = f"{duration_ms:.1f}"
    return response


app.include_router(face.router)
