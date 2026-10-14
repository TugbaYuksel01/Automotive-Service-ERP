import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  Wrench,
  LogOut,
  Shield,
  Users,
  Package,
  Wallet,
  CalendarClock,
  LayoutDashboard,
} from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';

const NAV: { to: string; label: string; icon: typeof Shield; roles: string[] }[] = [
  { to: '/panel', label: 'Yönetim', icon: LayoutDashboard, roles: ['SuperAdmin', 'Admin'] },
  { to: '/usta', label: 'Usta Paneli', icon: Wrench, roles: ['SuperAdmin', 'Admin', 'Usta'] },
  { to: '/vezne', label: 'Vezne', icon: Wallet, roles: ['SuperAdmin', 'Veznedar'] },
  { to: '/depo', label: 'Depo', icon: Package, roles: ['SuperAdmin', 'Admin', 'DepoSorumlusu', 'Usta'] },
];

export function StaffLayout() {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();
  const links = NAV.filter((item) => hasRole(...item.roles));

  return (
    <div className="min-h-screen bg-ink-50 flex">
      <aside className="hidden md:flex w-64 flex-col border-r border-ink-200 bg-ink-950 text-ink-200">
        <div className="flex items-center gap-2 px-5 h-16 border-b border-white/10">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600">
            <Shield className="h-4 w-4 text-white" />
          </div>
          <div>
            <p className="font-display font-bold text-white text-sm">OtoServis</p>
            <p className="text-[11px] text-ink-400">{user?.Rol}</p>
          </div>
        </div>
        <nav className="flex-1 p-3 space-y-1">
          {links.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
                    isActive ? 'bg-brand-600 text-white' : 'hover:bg-white/10 text-ink-200'
                  }`
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}
          <NavLink
            to="/"
            className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium hover:bg-white/10"
          >
            <Users className="h-4 w-4" />
            Genel site
          </NavLink>
        </nav>
        <button
          className="m-3 flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm hover:bg-white/10"
          onClick={async () => {
            await logout();
            navigate('/');
          }}
        >
          <LogOut className="h-4 w-4" />
          Çıkış
        </button>
      </aside>
      <div className="flex-1 min-w-0">
        <header className="md:hidden flex items-center justify-between h-14 px-4 border-b bg-white">
          <span className="font-display font-bold">{user?.Rol}</span>
          <div className="flex gap-2 overflow-x-auto">
            {links.map((item) => (
              <NavLink key={item.to} to={item.to} className="text-xs btn-ghost">
                {item.label}
              </NavLink>
            ))}
          </div>
        </header>
        <div className="p-4 sm:p-8">
          <div className="mb-6 hidden md:flex items-center gap-2 text-ink-500 text-sm">
            <CalendarClock className="h-4 w-4" />
            Personel oturumu
            <span className="ml-auto font-medium text-ink-800">#{user?.Id}</span>
          </div>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
