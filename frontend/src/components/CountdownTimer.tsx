import React from 'react';
import { Timer } from 'lucide-react';
import { useCountdown } from '../hooks/useCountdown';

interface CountdownTimerProps {
  expiresAt: string;
  onExpire?: () => void;
}

export const CountdownTimer: React.FC<CountdownTimerProps> = ({ expiresAt, onExpire }) => {
  const { formatted, isExpired, secondsRemaining } = useCountdown(expiresAt, onExpire);

  if (isExpired) {
    return (
      <div className="flex items-center gap-1.5 text-rose-600 font-semibold text-sm">
        <Timer className="w-4 h-4 text-rose-500" />
        <span>Reservation Expired</span>
      </div>
    );
  }

  const isLowTime = secondsRemaining < 120;

  return (
    <div className="flex items-center gap-2">
      <Timer className={`w-4 h-4 ${isLowTime ? 'text-rose-500 animate-spin' : 'text-amber-500'}`} />
      <div className="flex flex-col">
        <span className="text-xs text-slate-500">Hold expires in</span>
        <span className={`font-mono text-base font-bold ${isLowTime ? 'text-rose-600' : 'text-amber-700'}`}>
          {formatted}
        </span>
      </div>
    </div>
  );
};
