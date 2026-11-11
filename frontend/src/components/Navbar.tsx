import React from 'react';
import { School, User, ShieldCheck } from 'lucide-react';

interface NavbarProps {
  currentTab: 'parent' | 'admin';
  onSelectTab: (tab: 'parent' | 'admin') => void;
}

export const Navbar: React.FC<NavbarProps> = ({ currentTab, onSelectTab }) => {
  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center shadow-md">
              <School className="w-6 h-6" />
            </div>
            <div>
              <h1 className="font-bold text-lg text-slate-900 tracking-tight leading-tight">
                School Admission System
              </h1>
              <p className="text-xs text-slate-500 font-medium">
                First-Come, First-Served Real-Time Allocation
              </p>
            </div>
          </div>

          <nav className="flex items-center gap-2 bg-slate-100 p-1.5 rounded-xl border border-slate-200">
            <button
              type="button"
              onClick={() => onSelectTab('parent')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-all ${
                currentTab === 'parent'
                  ? 'bg-white text-blue-600 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/50'
              }`}
            >
              <User className="w-4 h-4" />
              Parent / Student
            </button>
            <button
              type="button"
              onClick={() => onSelectTab('admin')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-all ${
                currentTab === 'admin'
                  ? 'bg-white text-blue-600 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/50'
              }`}
            >
              <ShieldCheck className="w-4 h-4" />
              School Admin
            </button>
          </nav>
        </div>
      </div>
    </header>
  );
};
