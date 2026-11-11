import React, { useState, useEffect, useCallback } from 'react';
import { PlusCircle, RefreshCw, Layers, Users, CheckCircle, Clock } from 'lucide-react';
import type { ClassItem } from '../types';
import { api, formatErrorMessage } from '../services/api';
import { AlertMessage } from '../components/AlertMessage';

export const AdminPage: React.FC = () => {
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');

  // Form state
  const [className, setClassName] = useState<string>('');
  const [totalSeats, setTotalSeats] = useState<string>('120');

  const fetchClasses = useCallback(async (isBackground = false) => {
    if (!isBackground) setLoading(true);
    else setRefreshing(true);

    try {
      const data = await api.getClasses();
      setClasses(data);
    } catch (err) {
      if (!isBackground) {
        setErrorMessage(formatErrorMessage(err));
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  // Initial fetch and automatic polling interval
  useEffect(() => {
    fetchClasses();
    const interval = setInterval(() => {
      fetchClasses(true);
    }, 4000);
    return () => clearInterval(interval);
  }, [fetchClasses]);

  const handleCreateClass = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');

    if (!className.trim()) {
      setErrorMessage('Class name is required (e.g., "Class 6 - Morning Shift").');
      return;
    }

    const seats = parseInt(totalSeats, 10);
    if (isNaN(seats) || seats <= 0) {
      setErrorMessage('Total seats must be a positive integer greater than 0.');
      return;
    }

    setSubmitting(true);
    try {
      const created = await api.createClass({
        name: className.trim(),
        totalSeats: seats,
      });
      setSuccessMessage(`Successfully created "${created.name}" with ${created.totalSeats} seats.`);
      setClassName('');
      setTotalSeats('120');
      await fetchClasses();
    } catch (err) {
      setErrorMessage(formatErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const totalCapacity = classes.reduce((sum, c) => sum + c.totalSeats, 0);
  const totalHeld = classes.reduce((sum, c) => sum + c.heldSeats, 0);
  const totalConfirmed = classes.reduce((sum, c) => sum + c.confirmedSeats, 0);
  const totalAvailable = classes.reduce((sum, c) => sum + c.availableSeats, 0);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
            School Administration Portal
          </h2>
          <p className="text-sm text-slate-500 mt-1">
            Manage class and shift capacities with real-time seat statistics.
          </p>
        </div>

        <button
          type="button"
          onClick={() => fetchClasses(false)}
          disabled={loading || refreshing}
          className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-sm font-semibold bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 shadow-sm transition-all"
        >
          <RefreshCw className={`w-4 h-4 text-slate-500 ${refreshing ? 'animate-spin' : ''}`} />
          {refreshing ? 'Updating...' : 'Refresh Stats'}
        </button>
      </div>

      <AlertMessage type="error" message={errorMessage} onClose={() => setErrorMessage('')} />
      <AlertMessage type="success" message={successMessage} onClose={() => setSuccessMessage('')} />

      {/* Aggregate Metrics Bar */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
            <Layers className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Total Seats</div>
            <div className="text-2xl font-bold text-slate-900">{totalCapacity}</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Currently Held</div>
            <div className="text-2xl font-bold text-amber-600">{totalHeld}</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
            <CheckCircle className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Confirmed</div>
            <div className="text-2xl font-bold text-emerald-600">{totalConfirmed}</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
            <Users className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Available</div>
            <div className="text-2xl font-bold text-indigo-600">{totalAvailable}</div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Class Creation Card */}
        <div className="lg:col-span-1">
          <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm sticky top-24">
            <div className="flex items-center gap-2 mb-4 pb-3 border-b border-slate-100">
              <PlusCircle className="w-5 h-5 text-blue-600" />
              <h3 className="font-bold text-base text-slate-900">Create New Class</h3>
            </div>

            <form onSubmit={handleCreateClass} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                  Class & Shift Name
                </label>
                <input
                  type="text"
                  value={className}
                  onChange={(e) => setClassName(e.target.value)}
                  placeholder="e.g. Class 6 - Morning Shift"
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition-all"
                  disabled={submitting}
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                  Total Seat Capacity
                </label>
                <input
                  type="number"
                  min="1"
                  max="10000"
                  value={totalSeats}
                  onChange={(e) => setTotalSeats(e.target.value)}
                  placeholder="e.g. 120"
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition-all"
                  disabled={submitting}
                />
              </div>

              <button
                type="submit"
                disabled={submitting || !className.trim()}
                className="w-full mt-2 py-2.5 px-4 rounded-xl bg-blue-600 text-white font-semibold text-sm hover:bg-blue-700 shadow-md hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all"
              >
                {submitting ? 'Creating Class...' : 'Create Class'}
              </button>
            </form>
          </div>
        </div>

        {/* Real-time Classes List Table */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <h3 className="font-bold text-base text-slate-900">Class Seat Allocation Overview</h3>
              <span className="text-xs text-slate-500 flex items-center gap-1.5 font-medium">
                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
                Auto-refreshes every 4s
              </span>
            </div>

            {loading && classes.length === 0 ? (
              <div className="p-12 text-center text-slate-400">
                <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-3 text-slate-300" />
                <p className="text-sm">Loading classes...</p>
              </div>
            ) : classes.length === 0 ? (
              <div className="p-12 text-center text-slate-400">
                <p className="text-base font-semibold text-slate-600">No classes registered yet.</p>
                <p className="text-xs text-slate-400 mt-1">Use the form on the left to add a class.</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs font-semibold text-slate-500 uppercase tracking-wider bg-slate-50/75">
                      <th className="py-3.5 px-4">Class ID</th>
                      <th className="py-3.5 px-4">Class / Shift</th>
                      <th className="py-3.5 px-3 text-center">Total</th>
                      <th className="py-3.5 px-3 text-center text-amber-600">Held</th>
                      <th className="py-3.5 px-3 text-center text-emerald-600">Confirmed</th>
                      <th className="py-3.5 px-3 text-center text-blue-600">Available</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {classes.map((cls) => {
                      const fillPercent = cls.totalSeats > 0
                        ? Math.min(100, Math.round(((cls.confirmedSeats + cls.heldSeats) / cls.totalSeats) * 100))
                        : 0;

                      return (
                        <tr key={cls.id} className="hover:bg-slate-50/75 transition-colors">
                          <td className="py-4 px-4 font-mono text-xs text-slate-500">{cls.id}</td>
                          <td className="py-4 px-4 font-semibold text-slate-900">
                            <div>{cls.name}</div>
                            {/* Capacity Visual Progress Bar */}
                            <div className="w-full bg-slate-100 rounded-full h-1.5 mt-2 overflow-hidden flex">
                              <div
                                style={{ width: `${(cls.confirmedSeats / cls.totalSeats) * 100}%` }}
                                className="bg-emerald-500 h-full"
                                title={`Confirmed: ${cls.confirmedSeats}`}
                              />
                              <div
                                style={{ width: `${(cls.heldSeats / cls.totalSeats) * 100}%` }}
                                className="bg-amber-400 h-full"
                                title={`Held: ${cls.heldSeats}`}
                              />
                            </div>
                            <span className="text-[11px] text-slate-400 font-normal mt-0.5 inline-block">
                              {fillPercent}% Occupied ({cls.confirmedSeats + cls.heldSeats} / {cls.totalSeats})
                            </span>
                          </td>
                          <td className="py-4 px-3 text-center font-bold text-slate-700">{cls.totalSeats}</td>
                          <td className="py-4 px-3 text-center font-bold text-amber-600">
                            <span className="inline-block px-2 py-0.5 rounded-md bg-amber-50">
                              {cls.heldSeats}
                            </span>
                          </td>
                          <td className="py-4 px-3 text-center font-bold text-emerald-600">
                            <span className="inline-block px-2 py-0.5 rounded-md bg-emerald-50">
                              {cls.confirmedSeats}
                            </span>
                          </td>
                          <td className="py-4 px-3 text-center font-bold text-blue-600">
                            <span className={`inline-block px-2.5 py-0.5 rounded-md font-bold ${
                              cls.availableSeats > 0 ? 'bg-blue-50 text-blue-700' : 'bg-rose-50 text-rose-600'
                            }`}>
                              {cls.availableSeats}
                            </span>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
