/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { notificationsApi } from '../services/wardrobeApi';
import { API_BASE_URL } from '../config/api';

const NotificationContext = createContext(null);

// The hub is mounted at the server root (/hubs/...), not under the /api prefix the REST client uses.
const HUB_URL = `${API_BASE_URL.replace(/\/api\/?$/, '')}/hubs/notifications`;

let toastSeq = 0;

export function NotificationProvider({ children }) {
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [toasts, setToasts] = useState([]);
  const connectionRef = useRef(null);

  const dismissToast = useCallback((id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const pushToast = useCallback((toast) => {
    const id = ++toastSeq;
    setToasts((prev) => [...prev, { id, type: 'info', duration: 6000, ...toast }]);
    return id;
  }, []);

  const handleIncoming = useCallback((dto) => {
    setNotifications((prev) => [dto, ...prev.filter((n) => n.id !== dto.id)].slice(0, 50));
    setUnreadCount((c) => c + 1);

    pushToast({
      type: dto.type === 'WeatherAlert' ? 'warning' : 'success',
      title: dto.title,
      message: dto.message,
    });
  }, [pushToast]);

  // Seed from REST + open the live SignalR connection while a session cookie is present.
  useEffect(() => {
    const savedUser = localStorage.getItem('wardrobe_user');
    if (!savedUser) return undefined;

    let cancelled = false;

    (async () => {
      try {
        const [listRes, countRes] = await Promise.all([
          notificationsApi.list({ take: 30 }),
          notificationsApi.unreadCount(),
        ]);
        if (cancelled) return;
        setNotifications(listRes.data || []);
        setUnreadCount(countRes.data?.count || 0);
      } catch (e) {
        console.error('Notifications load failed:', e);
      }
    })();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (dto) => {
      if (!cancelled) handleIncoming(dto);
    });

    connection.start().catch((e) => console.error('SignalR connect failed:', e));
    connectionRef.current = connection;

    return () => {
      cancelled = true;
      connection.stop();
      connectionRef.current = null;
    };
  }, [handleIncoming]);

  const markRead = useCallback((id) => {
    let wasUnread = false;
    setNotifications((prev) => prev.map((n) => {
      if (n.id === id && !n.isRead) {
        wasUnread = true;
        return { ...n, isRead: true };
      }
      return n;
    }));
    if (wasUnread) {
      setUnreadCount((c) => Math.max(0, c - 1));
      notificationsApi.markRead(id).catch((e) => console.error('markRead failed:', e));
    }
  }, []);

  const markAllRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
    notificationsApi.markAllRead().catch((e) => console.error('markAllRead failed:', e));
  }, []);

  const value = { 
    notifications, 
    unreadCount, 
    toasts, 
    pushToast, 
    dismissToast, 
    markRead, 
    markAllRead
  };
  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
}

export function useNotifications() {
  const ctx = useContext(NotificationContext);
  if (!ctx) throw new Error('useNotifications must be used inside NotificationProvider');
  return ctx;
}
