import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface FaceSearchEvents {
  onSearchStarted?: (data: { sessionToken: string; eventId: string; startedAt: string }) => void;
  onSearchProgress?: (data: { sessionToken: string; matchCount: number }) => void;
  onSearchCompleted?: (data: {
    sessionToken: string;
    matchCount: number;
    expiresAt: string;
    completedAt: string;
  }) => void;
  onFaceIndexCompleted?: (data: { eventId: string; photoId: string; faceCount: number }) => void;
}

/**
 * Connects to the SignalR PhotoHub and subscribes to face-search lifecycle events.
 * Automatically joins the private session group when `sessionToken` is provided.
 * Joins the event group when `eventId` is provided (for face-index-completed events).
 */
export function useFaceSearchHub(
  serverUrl: string,
  eventId?: string,
  sessionToken?: string,
  handlers?: FaceSearchEvents,
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  const connect = useCallback(async () => {
    const params = new URLSearchParams();
    if (eventId) params.set('eventId', eventId);
    if (sessionToken) params.set('sessionToken', sessionToken);

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${serverUrl}/hubs/photos?${params.toString()}`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    if (handlers?.onSearchStarted)
      connection.on('face-search-started', handlers.onSearchStarted);

    if (handlers?.onSearchProgress)
      connection.on('face-search-progress', handlers.onSearchProgress);

    if (handlers?.onSearchCompleted)
      connection.on('face-search-completed', handlers.onSearchCompleted);

    if (handlers?.onFaceIndexCompleted)
      connection.on('face-index-completed', handlers.onFaceIndexCompleted);

    try {
      await connection.start();
      connectionRef.current = connection;
    } catch (err) {
      console.warn('[FaceSearchHub] Connection failed:', err);
    }
  }, [serverUrl, eventId, sessionToken]); // handlers intentionally omitted (stable ref pattern)

  useEffect(() => {
    connect();
    return () => {
      connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, [connect]);

  return connectionRef;
}
