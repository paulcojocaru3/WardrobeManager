import { useEffect } from 'react';
import { useNotifications } from '../contexts/NotificationContext';
import './Toast.css';

function ToastItem({ toast, onDismiss }) {
  useEffect(() => {
    const duration = toast.duration ?? 6000;
    const timer = setTimeout(() => onDismiss(toast.id), duration);
    return () => clearTimeout(timer);
  }, [toast, onDismiss]);

  return (
    <div className={`toast toast-${toast.type}`} role="status" aria-live="polite">
      <span className="toast-dot" />
      <div className="toast-body">
        {toast.title && <strong className="toast-title">{toast.title}</strong>}
        {toast.message && <span className="toast-message">{toast.message}</span>}
      </div>
      <button className="toast-close" onClick={() => onDismiss(toast.id)} aria-label="Dismiss">×</button>
    </div>
  );
}

export default function Toast() {
  const { toasts, dismissToast } = useNotifications();
  if (!toasts.length) return null;

  return (
    <div className="toast-stack">
      {toasts.map((t) => (
        <ToastItem key={t.id} toast={t} onDismiss={dismissToast} />
      ))}
    </div>
  );
}
