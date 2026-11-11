import { useState } from 'react';
import { Navbar } from './components/Navbar';
import { ParentPage } from './pages/ParentPage';
import { AdminPage } from './pages/AdminPage';

export function App() {
  const [currentTab, setCurrentTab] = useState<'parent' | 'admin'>('parent');

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col text-slate-800">
      <Navbar currentTab={currentTab} onSelectTab={setCurrentTab} />

      <main className="flex-1">
        {currentTab === 'parent' ? <ParentPage /> : <AdminPage />}
      </main>

      <footer className="bg-white border-t border-slate-200 py-6 mt-12 text-center text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4">
          <p className="font-medium">School Admission System &bull; First-Come, First-Served Real-Time Seat Allocation</p>
          <p className="mt-1 text-slate-400">Built with ASP.NET Core (.NET 8+), PostgreSQL, React 18+ TypeScript, and Docker</p>
        </div>
      </footer>
    </div>
  );
}

export default App;
