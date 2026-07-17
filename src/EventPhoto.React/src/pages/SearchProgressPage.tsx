import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { faceSearchApi } from '../api/faceSearch';
import { useFaceSearchHub } from '../hooks/useFaceSearchHub';

/**
 * SearchProgressPage — shown while the face-search session is in progress.
 * Connects to SignalR for real-time updates; falls back to polling every 2s.
 */
export default function SearchProgressPage() {
  const { eventId, sessionToken } = useParams<{ eventId: string; sessionToken: string }>();
  const navigate = useNavigate();
  const [signalRCompleted, setSignalRCompleted] = useState(false);
  const [serverUrl] = useState(() => window.location.origin);

  // SignalR real-time updates
  useFaceSearchHub(serverUrl, eventId, sessionToken, {
    onSearchCompleted: (data) => {
      setSignalRCompleted(true);
      navigate(`/gallery/${eventId}/results/${data.sessionToken}`);
    },
  });

  // Polling fallback
  const { data } = useQuery({
    queryKey: ['faceSearchStatus', sessionToken],
    queryFn: () => faceSearchApi.getStatus(sessionToken!).then(r => r.data.data!),
    enabled: !!sessionToken && !signalRCompleted,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === 'Completed' || status === 'Expired' ? false : 2000;
    },
  });

  // Navigate when polling detects completion
  useEffect(() => {
    if (data?.status === 'Completed') {
      navigate(`/gallery/${eventId}/results/${sessionToken}`);
    }
  }, [data, eventId, sessionToken, navigate]);

  const dots = ['⣾', '⣽', '⣻', '⢿', '⡿', '⣟', '⣯', '⣷'];
  const [dotIdx, setDotIdx] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setDotIdx(i => (i + 1) % dots.length), 150);
    return () => clearInterval(id);
  }, []);

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50 px-4">
      <div className="bg-white rounded-2xl shadow-lg p-10 w-full max-w-sm text-center">
        <div className="text-5xl mb-4 text-indigo-500">{dots[dotIdx]}</div>
        <h2 className="text-xl font-semibold text-gray-800 mb-2">Searching for you...</h2>
        <p className="text-sm text-gray-500">
          We're matching your face against {data?.matchCount ?? '...'} photos.
          This takes just a moment.
        </p>
        {data?.status === 'Expired' && (
          <p className="mt-4 text-sm text-red-500">Session expired. Please try again.</p>
        )}
      </div>
    </div>
  );
}
