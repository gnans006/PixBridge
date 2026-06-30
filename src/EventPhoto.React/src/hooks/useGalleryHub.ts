import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { authStore } from '../store/authStore';

export interface NewPhotoEvent {
  photoId: string;
  eventId: string;
  fileName: string;
  thumbnailUrl: string;
  capturedAt: string;
}

const HUB_URL = import.meta.env.VITE_HUB_BASE ?? '/hubs/photos';

export function useGalleryHub(eventId: string | null, onNewPhoto: (photo: NewPhotoEvent) => void) {
  const hubRef = useRef<signalR.HubConnection | null>(null);
  const onNewPhotoRef = useRef(onNewPhoto);
  const [isConnected, setIsConnected] = useState(false);

  onNewPhotoRef.current = onNewPhoto;

  const connect = useCallback(async () => {
    if (!eventId || hubRef.current) {
      return;
    }

    const token = authStore.getToken();
    const options = token ? { accessTokenFactory: () => token } : {};
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${HUB_URL}?eventId=${eventId}`, options)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('photo:new', (data: NewPhotoEvent) => onNewPhotoRef.current(data));
    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    try {
      await connection.start();
      hubRef.current = connection;
      setIsConnected(true);
    } catch (error) {
      console.warn('SignalR connection failed:', error);
    }
  }, [eventId]);

  useEffect(() => {
    void connect();

    return () => {
      const connection = hubRef.current;
      hubRef.current = null;
      setIsConnected(false);
      if (connection) {
        void connection.stop();
      }
    };
  }, [connect]);

  return { isConnected };
}
