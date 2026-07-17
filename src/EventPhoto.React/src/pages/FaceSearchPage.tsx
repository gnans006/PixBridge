import { useState, useRef, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { faceSearchApi, type GuestGalleryConfig } from '../api/faceSearch';

/**
 * FaceSearchPage — guest landing page for selfie-based photo search.
 *
 * Renders based on the event's gallery mode:
 *   GalleryOnly    → redirects straight to the gallery
 *   FaceSearchOnly → shows selfie upload only
 *   Hybrid         → shows both "Browse Gallery" and "Find My Photos" buttons
 */
export default function FaceSearchPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Load gallery config
  const { data: configData, isLoading: configLoading } = useQuery({
    queryKey: ['galleryConfig', eventId],
    queryFn: () => faceSearchApi.getGalleryConfig(eventId!).then(r => r.data.data!),
    enabled: !!eventId,
  });

  const config: GuestGalleryConfig | undefined = configData;

  // Redirect to plain gallery if GalleryOnly mode
  if (config?.galleryMode === 'GalleryOnly') {
    navigate(`/gallery/${eventId}`, { replace: true });
    return null;
  }

  // Start face-search mutation
  const searchMutation = useMutation({
    mutationFn: (file: File) => faceSearchApi.startSearch(eventId!, file).then(r => r.data.data!),
    onSuccess: (data) => {
      // Navigate to progress/results page with session token
      navigate(`/gallery/${eventId}/search/${data.sessionToken}`);
    },
    onError: (err: Error) => {
      setError(err.message || 'Search failed. Please try again.');
    },
  });

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSelectedFile(file);
    setPreviewUrl(URL.createObjectURL(file));
    setError(null);
  };

  const handleSearch = useCallback(() => {
    if (!selectedFile) {
      setError('Please select a selfie first.');
      return;
    }
    searchMutation.mutate(selectedFile);
  }, [selectedFile, searchMutation]);

  if (configLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="text-gray-500 text-lg">Loading event...</div>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50 px-4">
      <div className="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md">
        {/* Event name */}
        <h1 className="text-2xl font-bold text-center text-gray-800 mb-2">
          {config?.eventName ?? 'Event Gallery'}
        </h1>
        <p className="text-center text-gray-500 mb-6 text-sm">
          {config?.galleryMode === 'FaceSearchOnly'
            ? 'Upload your selfie to find photos of you.'
            : 'Browse all photos or find photos of yourself.'}
        </p>

        {/* Hybrid: Browse Gallery button */}
        {config?.allowGalleryBrowsing && (
          <button
            onClick={() => navigate(`/gallery/${eventId}`)}
            className="w-full mb-3 py-3 rounded-xl bg-indigo-600 text-white font-semibold hover:bg-indigo-700 transition"
          >
            📷 Browse Gallery
          </button>
        )}

        {/* Face search section */}
        {config?.allowFaceSearch && (
          <>
            <div className="relative border-t border-gray-200 my-4">
              <span className="absolute top-[-10px] left-1/2 -translate-x-1/2 bg-white px-3 text-xs text-gray-400">
                or
              </span>
            </div>

            <p className="text-center text-sm text-gray-600 mb-3 font-medium">
              🔍 Find My Photos
            </p>

            {/* Selfie preview */}
            {previewUrl && (
              <div className="mb-4 flex justify-center">
                <img
                  src={previewUrl}
                  alt="Selfie preview"
                  className="w-32 h-32 rounded-full object-cover border-4 border-indigo-200"
                />
              </div>
            )}

            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              capture="user"
              className="hidden"
              onChange={handleFileSelect}
            />

            <button
              onClick={() => fileInputRef.current?.click()}
              className="w-full mb-3 py-3 rounded-xl border-2 border-dashed border-indigo-300 text-indigo-600 hover:bg-indigo-50 transition text-sm font-medium"
            >
              {selectedFile ? `✓ ${selectedFile.name}` : '📸 Take / Choose Selfie'}
            </button>

            {selectedFile && (
              <button
                onClick={handleSearch}
                disabled={searchMutation.isPending}
                className="w-full py-3 rounded-xl bg-green-600 text-white font-semibold hover:bg-green-700 disabled:opacity-50 transition"
              >
                {searchMutation.isPending ? 'Searching...' : '🔍 Find My Photos'}
              </button>
            )}

            {error && (
              <p className="mt-3 text-center text-sm text-red-500">{error}</p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
