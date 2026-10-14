import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '@/contexts/AuthProvider';
import { AppProviders } from '@/contexts/AppProviders';
import { Layout } from '@/components/Layout';
import { StaffLayout } from '@/components/StaffLayout';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { LandingPage } from '@/pages/LandingPage';
import { ServicesPage } from '@/pages/ServicesPage';
import { GuestAppointmentPage } from '@/pages/GuestAppointmentPage';
import { CustomerAppointmentPage } from '@/pages/CustomerAppointmentPage';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { ResetPasswordPage } from '@/pages/ResetPasswordPage';
import { MyAppointmentsPage } from '@/pages/MyAppointmentsPage';
import { AdminPanelPage } from '@/pages/AdminPanelPage';
import { UstaPanelPage } from '@/pages/UstaPanelPage';
import { VeznedarPanelPage } from '@/pages/VeznedarPanelPage';
import { DepoPanelPage } from '@/pages/DepoPanelPage';
import UserProfilePage from '@/pages/UserProfilePage';
import { GuestTrackingPage } from '@/pages/GuestTrackingPage';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppProviders>
          <Routes>
            {/* GENEL / MÜŞTERİ YÖNLENDİRMELERİ */}
            <Route element={<Layout />}>
              <Route path="/" element={<LandingPage />} />
              <Route path="/hizmetler" element={<ServicesPage />} />
              <Route path="/randevu" element={<GuestAppointmentPage />} />
              <Route path="/giris" element={<LoginPage />} />
              <Route path="/kayit" element={<RegisterPage />} />
              <Route path="/sifremi-unuttum" element={<ForgotPasswordPage />} />
              <Route path="/sifre-sifirla" element={<ResetPasswordPage />} />
              
              {/* TÜM ROLLER İÇİN ORTAK PROFİL SAYFASI (Çakışma önlendi) */}
              <Route
                path="/profil"
                element={
                  <ProtectedRoute roles={['Musteri', 'Usta', 'Admin', 'SuperAdmin', 'Veznedar', 'DepoSorumlusu']}>
                    <UserProfilePage />
                  </ProtectedRoute>
                }
              />
             
              <Route path="/takip" element={<GuestTrackingPage />} /> {/* YENİ EKLENEN */}

              <Route
                path="/randevu-olustur"
                element={
                  <ProtectedRoute roles={['Musteri']}>
                    <CustomerAppointmentPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/randevularim"
                element={
                  <ProtectedRoute roles={['Musteri']}>
                    <MyAppointmentsPage />
                  </ProtectedRoute>
                }
              />
            </Route>

            {/* PERSONEL YÖNETİM PANELİ YÖNLENDİRMELERİ */}
            <Route
              element={
                <ProtectedRoute roles={['SuperAdmin', 'Admin', 'Usta', 'Veznedar', 'DepoSorumlusu']}>
                  <StaffLayout />
                </ProtectedRoute>
              }
            >
              <Route
                path="/panel"
                element={
                  <ProtectedRoute roles={['SuperAdmin', 'Admin']}>
                    <AdminPanelPage />
                  </ProtectedRoute>
                }
              />
               <Route
                path="/panel"
                element={
                  <ProtectedRoute roles={['SuperAdmin', 'Admin']}>
                    <AdminPanelPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/usta"
                element={
                  <ProtectedRoute roles={['SuperAdmin', 'Admin', 'Usta']}>
                    <UstaPanelPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/vezne"
                element={
                  <ProtectedRoute roles={['SuperAdmin', 'Veznedar']}>
                    <VeznedarPanelPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/depo"
                element={
                  <ProtectedRoute roles={['SuperAdmin', 'Admin', 'DepoSorumlusu', 'Usta']}>
                    <DepoPanelPage />
                  </ProtectedRoute>
                }
              />
            </Route>

            <Route path="/admin" element={<Navigate to="/panel" replace />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </AppProviders>
      </AuthProvider>
    </BrowserRouter>
  );
}