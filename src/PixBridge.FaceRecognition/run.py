import uvicorn
from app.core.config import settings

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host=settings.host,
        port=settings.port,
        reload=False,
        workers=1,  # single worker — InsightFace model is not multi-process safe
        log_level="info",
    )
