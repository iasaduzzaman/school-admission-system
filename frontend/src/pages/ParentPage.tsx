import React, { useState, useEffect, useCallback } from 'react';
import {
  CreditCard,
  Ban,
  GraduationCap,
  AlertCircle,
  Clock,
  History,
  CheckCircle,
  RefreshCw,
  Fingerprint,
  FileCheck,
  RotateCcw
} from 'lucide-react';
import type { ClassItem, StudentHold } from '../types';
import { api, formatErrorMessage } from '../services/api';
import { StatusBadge } from '../components/StatusBadge';
import { CountdownTimer } from '../components/CountdownTimer';
import { AlertMessage } from '../components/AlertMessage';

export const ParentPage: React.FC = () => {
  // Student ID state - persistent in localStorage
  const [studentId, setStudentId] = useState<string>(() => {
    return localStorage.getItem('school_admission_student_id') || '20152691234567890';
  });

  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [myHolds, setMyHolds] = useState<StudentHold[]>([]);
  const [loadingClasses, setLoadingClasses] = useState<boolean>(true);
  const [loadingHolds, setLoadingHolds] = useState<boolean>(false);
  const [refreshing, setRefreshing] = useState<boolean>(false);

  // Action states
  const [holdingClassId, setHoldingClassId] = useState<string | null>(null);
  const [confirmingHoldId, setConfirmingHoldId] = useState<string | null>(null);
  const [cancellingHoldId, setCancellingHoldId] = useState<string | null>(null);

  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');

  const isValidStudentId = /^\d{17}$/.test(studentId.trim());

  // Save studentId to localStorage
  const handleStudentIdChange = (val: string) => {
    setStudentId(val);
    localStorage.setItem('school_admission_student_id', val.trim());
  };

  // Fetch classes
  const fetchClasses = useCallback(async (isBackground = false) => {
    if (!isBackground) setLoadingClasses(true);
    else setRefreshing(true);

    try {
      const data = await api.getClasses();
      setClasses(data);
    } catch (err) {
      if (!isBackground) {
        setErrorMessage(formatErrorMessage(err));
      }
    } finally {
      setLoadingClasses(false);
      setRefreshing(false);
    }
  }, []);

  // Fetch current student holds
  const fetchMyHolds = useCallback(async (currentStudentId: string) => {
    if (!/^\d{17}$/.test(currentStudentId.trim())) {
      setMyHolds([]);
      return;
    }

    setLoadingHolds(true);
    try {
      const holds = await api.getStudentHolds(currentStudentId.trim());
      setMyHolds(holds);
    } catch {
      // Ignore initial lookup failure if student has no holds yet
    } finally {
      setLoadingHolds(false);
    }
  }, []);

  // Initial load and periodic polling
  useEffect(() => {
    fetchClasses();
    if (isValidStudentId) {
      fetchMyHolds(studentId);
    }

    const interval = setInterval(() => {
      fetchClasses(true);
      if (isValidStudentId) {
        fetchMyHolds(studentId);
      }
    }, 4000);

    return () => clearInterval(interval);
  }, [fetchClasses, fetchMyHolds, studentId, isValidStudentId]);

  // Handle Apply for Admission / Hold Seat
  const handleApply = async (classId: string) => {
    setErrorMessage('');
    setSuccessMessage('');

    if (!isValidStudentId) {
      setErrorMessage('Please enter a valid 17-digit birth registration number first.');
      return;
    }

    setHoldingClassId(classId);
    try {
      const hold = await api.createHold(classId, studentId.trim());
      setSuccessMessage(`Seat held successfully! Reservation ID: ${hold.holdId}. Please pay the admission fee within 10 minutes.`);
      await Promise.all([fetchClasses(true), fetchMyHolds(studentId)]);
    } catch (err) {
      setErrorMessage(formatErrorMessage(err));
    } finally {
      setHoldingClassId(null);
    }
  };

  // Handle Pay Admission Fee (Confirm Admission)
  const handleConfirm = async (holdId: string) => {
    setErrorMessage('');
    setSuccessMessage('');

    setConfirmingHoldId(holdId);
    try {
      await api.confirmHold(holdId, studentId.trim());
      setSuccessMessage('Payment successful! Admission confirmed.');
      await Promise.all([fetchClasses(true), fetchMyHolds(studentId)]);
    } catch (err) {
      setErrorMessage(formatErrorMessage(err));
    } finally {
      setConfirmingHoldId(null);
    }
  };

  // Handle Cancel Reservation
  const handleCancel = async (holdId: string) => {
    if (!window.confirm('Are you sure you want to cancel this reservation? The seat will be released immediately.')) {
      return;
    }

    setErrorMessage('');
    setSuccessMessage('');
    setCancellingHoldId(holdId);
    try {
      await api.cancelHold(holdId, studentId.trim());
      setSuccessMessage('Reservation cancelled. The seat has been released.');
      await Promise.all([fetchClasses(true), fetchMyHolds(studentId)]);
    } catch (err) {
      setErrorMessage(formatErrorMessage(err));
    } finally {
      setCancellingHoldId(null);
    }
  };

  // Check if student has active application for a class
  const getActiveHoldForClass = (classId: string) => {
    return myHolds.find(
      (h) => h.classId === classId && (h.status === 'HELD' || h.status === 'CONFIRMED')
    );
  };

  const activeHoldsCount = myHolds.filter((h) => h.status === 'HELD').length;
  const confirmedHoldsCount = myHolds.filter((h) => h.status === 'CONFIRMED').length;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Dashboard Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
            Student Admission Dashboard
          </h2>
          <p className="text-sm text-slate-500 mt-0.5">
            Real-time seat allocation &bull; First-Come, First-Served &bull; Instant Seat Reservation
          </p>
        </div>

        <div className="flex items-center gap-3">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 text-xs font-semibold text-slate-600 border border-slate-200">
            <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            Live Sync (4s)
          </span>

          <button
            type="button"
            onClick={() => {
              fetchClasses(false);
              if (isValidStudentId) fetchMyHolds(studentId);
            }}
            disabled={loadingClasses || refreshing}
            className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-xl text-xs font-semibold bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 shadow-sm transition-all"
          >
            <RefreshCw className={`w-3.5 h-3.5 text-slate-500 ${refreshing ? 'animate-spin' : ''}`} />
            Refresh
          </button>
        </div>
      </div>

      {/* Admin Dashboard Style Student Identification Section */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 mb-8">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6 pb-6 border-b border-slate-100">
          <div className="flex items-center gap-3.5">
            <div className="w-11 h-11 rounded-xl bg-blue-50 text-blue-600 border border-blue-100 flex items-center justify-center shadow-xs">
              <Fingerprint className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-bold text-base text-slate-900">Student Identity & Session</h3>
                <span className="px-2 py-0.5 rounded-md text-[11px] font-semibold bg-slate-100 text-slate-600 border border-slate-200">
                  No Login Required
                </span>
              </div>
              <p className="text-xs text-slate-500 mt-0.5">
                The 17-digit Birth Registration Number identifies the student and restores active applications automatically.
              </p>
            </div>
          </div>

          {/* Quick Metrics */}
          <div className="flex items-center gap-3 self-start lg:self-center">
            <div className="bg-slate-50 px-4 py-2 rounded-xl border border-slate-200 text-center min-w-[110px]">
              <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Active Holds</div>
              <div className="text-lg font-bold text-amber-600 font-mono">{activeHoldsCount}</div>
            </div>
            <div className="bg-slate-50 px-4 py-2 rounded-xl border border-slate-200 text-center min-w-[110px]">
              <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Confirmed</div>
              <div className="text-lg font-bold text-emerald-600 font-mono">{confirmedHoldsCount}</div>
            </div>
          </div>
        </div>

        {/* Input Control Row */}
        <div className="mt-6 flex flex-col md:flex-row items-stretch md:items-center gap-3">
          <div className="relative flex-1">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <FileCheck className="w-5 h-5 text-slate-400" />
            </div>
            <input
              type="text"
              maxLength={17}
              value={studentId}
              onChange={(e) => handleStudentIdChange(e.target.value)}
              placeholder="Enter 17-digit Birth Registration Number (e.g. 20152691234567890)"
              className="w-full pl-11 pr-24 py-3 rounded-xl border border-slate-300 bg-slate-50/50 text-slate-900 placeholder-slate-400 text-sm font-mono tracking-wider focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 focus:bg-white transition-all shadow-inner"
            />
            <div className="absolute inset-y-0 right-0 pr-3.5 flex items-center pointer-events-none text-xs font-mono font-medium text-slate-400">
              {studentId.trim().length} / 17
            </div>
          </div>

          <div className="flex items-center gap-2">
            {isValidStudentId ? (
              <span className="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200 shadow-xs">
                <CheckCircle className="w-4 h-4 text-emerald-500" />
                Valid 17-digit ID
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200 shadow-xs">
                <AlertCircle className="w-4 h-4 text-amber-500" />
                17 digits required
              </span>
            )}

            <button
              type="button"
              onClick={() => handleStudentIdChange('20152691234567890')}
              title="Reset to default test ID"
              className="inline-flex items-center gap-1 px-3 py-2.5 rounded-xl text-xs font-semibold bg-slate-100 hover:bg-slate-200 text-slate-600 transition-colors"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              Demo ID
            </button>
          </div>
        </div>
      </div>

      <AlertMessage type="error" message={errorMessage} onClose={() => setErrorMessage('')} />
      <AlertMessage type="success" message={successMessage} onClose={() => setSuccessMessage('')} />

      {/* Active Reservations & Applications Banner */}
      {isValidStudentId && myHolds.length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 mb-8">
          <div className="flex items-center justify-between gap-4 mb-4 pb-3 border-b border-slate-100">
            <div className="flex items-center gap-2">
              <History className="w-5 h-5 text-blue-600" />
              <h3 className="font-bold text-lg text-slate-900">My Applications</h3>
            </div>
            {loadingHolds && (
              <RefreshCw className="w-4 h-4 text-slate-400 animate-spin" />
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {myHolds.map((hold) => (
              <div
                key={hold.holdId}
                className="p-5 rounded-xl border border-slate-200 bg-slate-50/50 flex flex-col justify-between gap-4"
              >
                <div>
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <div>
                      <h4 className="font-bold text-base text-slate-900">{hold.className}</h4>
                      <p className="text-xs font-mono text-slate-400 mt-0.5">Hold ID: {hold.holdId}</p>
                    </div>
                    <StatusBadge status={hold.status} />
                  </div>

                  {hold.status === 'HELD' && (
                    <div className="mt-4 bg-white p-3 rounded-lg border border-amber-200 shadow-sm">
                      <CountdownTimer
                        expiresAt={hold.expiresAt}
                        onExpire={() => {
                          fetchClasses(true);
                          fetchMyHolds(studentId);
                        }}
                      />
                    </div>
                  )}
                </div>

                <div className="flex items-center gap-2 pt-2 border-t border-slate-200/60">
                  {hold.status === 'HELD' && (
                    <>
                      <button
                        type="button"
                        onClick={() => handleConfirm(hold.holdId)}
                        disabled={confirmingHoldId === hold.holdId}
                        className="flex-1 py-2 px-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs flex items-center justify-center gap-1.5 shadow-sm transition-all"
                      >
                        <CreditCard className="w-4 h-4" />
                        {confirmingHoldId === hold.holdId ? 'Processing...' : 'Pay Admission Fee'}
                      </button>

                      <button
                        type="button"
                        onClick={() => handleCancel(hold.holdId)}
                        disabled={cancellingHoldId === hold.holdId}
                        className="py-2 px-3 rounded-xl bg-white border border-slate-200 text-slate-700 hover:bg-slate-100 font-semibold text-xs flex items-center justify-center gap-1.5 transition-all"
                      >
                        <Ban className="w-3.5 h-3.5 text-rose-500" />
                        Cancel
                      </button>
                    </>
                  )}

                  {hold.status === 'CONFIRMED' && (
                    <div className="text-xs text-emerald-700 font-semibold flex items-center gap-1.5 py-1">
                      <CheckCircle className="w-4 h-4 text-emerald-600" />
                      Admission fee paid & seat secured.
                    </div>
                  )}

                  {(hold.status === 'EXPIRED' || hold.status === 'CANCELLED') && (
                    <div className="text-xs text-slate-400 flex items-center gap-1 py-1">
                      This reservation is closed.
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Available Classes Grid */}
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h3 className="text-xl font-bold text-slate-900 tracking-tight">Available Classes</h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Select a class and reserve your seat instantly before capacity is filled.
          </p>
        </div>
        <span className="text-xs text-slate-400 flex items-center gap-1.5">
          <Clock className="w-3.5 h-3.5" />
          Hold duration: 10 minutes (600s)
        </span>
      </div>

      {loadingClasses && classes.length === 0 ? (
        <div className="p-16 text-center text-slate-400 bg-white rounded-2xl border border-slate-200">
          <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-3 text-slate-300" />
          <p className="text-sm">Loading available classes...</p>
        </div>
      ) : classes.length === 0 ? (
        <div className="p-16 text-center text-slate-400 bg-white rounded-2xl border border-slate-200">
          <GraduationCap className="w-10 h-10 text-slate-300 mx-auto mb-3" />
          <p className="text-base font-semibold text-slate-600">No classes currently open for admission.</p>
          <p className="text-xs text-slate-400 mt-1">Please check back soon or contact the school administrator.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {classes.map((cls) => {
            const activeHold = getActiveHoldForClass(cls.id);
            const isFull = cls.availableSeats <= 0;
            const isApplying = holdingClassId === cls.id;

            return (
              <div
                key={cls.id}
                className={`bg-white rounded-2xl border p-6 shadow-sm hover:shadow-md transition-all flex flex-col justify-between ${
                  activeHold ? 'border-blue-300 ring-2 ring-blue-500/10' : 'border-slate-200'
                }`}
              >
                <div>
                  <div className="flex items-start justify-between gap-2 mb-3">
                    <h4 className="font-bold text-lg text-slate-900 leading-snug">{cls.name}</h4>
                    <span className="font-mono text-xs text-slate-400 px-2 py-0.5 bg-slate-50 rounded-md border border-slate-100">
                      {cls.id}
                    </span>
                  </div>

                  {/* Seat Breakdown Badges */}
                  <div className="grid grid-cols-3 gap-2 my-4 p-3 bg-slate-50/75 rounded-xl border border-slate-100 text-center">
                    <div>
                      <div className="text-[11px] text-slate-400 uppercase font-semibold">Total</div>
                      <div className="text-base font-bold text-slate-700">{cls.totalSeats}</div>
                    </div>
                    <div>
                      <div className="text-[11px] text-amber-600 uppercase font-semibold">Held</div>
                      <div className="text-base font-bold text-amber-600">{cls.heldSeats}</div>
                    </div>
                    <div>
                      <div className="text-[11px] text-emerald-600 uppercase font-semibold">Confirmed</div>
                      <div className="text-base font-bold text-emerald-600">{cls.confirmedSeats}</div>
                    </div>
                  </div>

                  {/* Available Seats Highlight */}
                  <div className="flex items-center justify-between text-sm py-2 px-1">
                    <span className="text-slate-600 font-medium">Available Seats:</span>
                    <span
                      className={`font-bold px-2.5 py-0.5 rounded-lg text-sm ${
                        cls.availableSeats > 0
                          ? 'bg-blue-50 text-blue-700 font-extrabold'
                          : 'bg-rose-50 text-rose-600'
                      }`}
                    >
                      {cls.availableSeats > 0 ? `${cls.availableSeats} Remaining` : 'Fully Booked'}
                    </span>
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-slate-100">
                  {activeHold ? (
                    <div className="flex flex-col gap-2">
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-slate-500">Your Status:</span>
                        <StatusBadge status={activeHold.status} />
                      </div>
                      <button
                        type="button"
                        disabled
                        className="w-full py-2.5 px-4 rounded-xl bg-slate-100 text-slate-400 font-semibold text-sm cursor-not-allowed"
                      >
                        Application Active
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => handleApply(cls.id)}
                      disabled={isFull || !isValidStudentId || isApplying}
                      className={`w-full py-2.5 px-4 rounded-xl font-semibold text-sm transition-all shadow-sm ${
                        isFull
                          ? 'bg-slate-100 text-slate-400 cursor-not-allowed'
                          : !isValidStudentId
                          ? 'bg-slate-200 text-slate-500 cursor-not-allowed'
                          : 'bg-blue-600 hover:bg-blue-700 text-white shadow-md hover:shadow-lg'
                      }`}
                    >
                      {isApplying
                        ? 'Reserving Seat...'
                        : isFull
                        ? 'No Seats Available'
                        : !isValidStudentId
                        ? 'Enter 17-digit ID Above'
                        : 'Apply for Admission'}
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
