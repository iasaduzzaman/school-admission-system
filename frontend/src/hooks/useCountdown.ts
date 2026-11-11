import { useState, useEffect } from 'react';

export const useCountdown = (expiresAt: string, onExpire?: () => void) => {
  const calculateRemaining = () => {
    const target = new Date(expiresAt).getTime();
    const now = Date.now();
    return Math.max(0, Math.floor((target - now) / 1000));
  };

  const [secondsRemaining, setSecondsRemaining] = useState<number>(calculateRemaining);

  useEffect(() => {
    setSecondsRemaining(calculateRemaining());

    const timer = setInterval(() => {
      const remaining = calculateRemaining();
      setSecondsRemaining(remaining);

      if (remaining <= 0) {
        clearInterval(timer);
        onExpire?.();
      }
    }, 1000);

    return () => clearInterval(timer);
  }, [expiresAt]);

  const minutes = Math.floor(secondsRemaining / 60);
  const seconds = secondsRemaining % 60;
  const formatted = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

  return {
    secondsRemaining,
    formatted,
    isExpired: secondsRemaining <= 0,
  };
};
