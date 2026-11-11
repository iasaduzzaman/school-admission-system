import React from 'react';
import { AlertCircle, CheckCircle2, X } from 'lucide-react';

interface AlertMessageProps {
  type: 'error' | 'success';
  message: string;
  onClose?: () => void;
}

export const AlertMessage: React.FC<AlertMessageProps> = ({ type, message, onClose }) => {
  if (!message) return null;

  const isError = type === 'error';

  return (
    <div
      className={`flex items-start gap-3 p-4 rounded-xl border mb-6 shadow-sm transition-all ${
        isError
          ? 'bg-rose-50 border-rose-200 text-rose-900'
          : 'bg-emerald-50 border-emerald-200 text-emerald-900'
      }`}
    >
      {isError ? (
        <AlertCircle className="w-5 h-5 text-rose-600 mt-0.5 flex-shrink-0" />
      ) : (
        <CheckCircle2 className="w-5 h-5 text-emerald-600 mt-0.5 flex-shrink-0" />
      )}
      <div className="flex-1 text-sm font-medium leading-relaxed">{message}</div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          className="text-slate-400 hover:text-slate-600 p-1 rounded-md transition-colors"
          title="Dismiss"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
};
