import { createContext, useContext, useState } from 'react';
import api from '../api/client';
import { saveSession, getUser, clearSession } from '../lib/auth';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => getUser());
  const [loading, setLoading] = useState(false);

  const login = async (email, password) => {
    setLoading(true);
    try {
      const { data } = await api.post('/auth/login', { email, password });
      saveSession(data);
      setUser(getUser());
      return data;
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    clearSession();
    setUser(null);
  };

  /**
   * Cambia la contraseña.
   * El backend ahora devuelve el LoginResponse completo (token + email + rol + socioId)
   * en vez de sólo {message, token}, así que preservamos todos los campos del usuario.
   * Para compatibilidad con backends antiguos que sólo devuelven {token}, hacemos
   * un merge defensivo con el usuario previo si faltan campos.
   */
  const cambiarPassword = async (nuevaPassword) => {
    const { data } = await api.post('/auth/cambiar-password', { nuevaPassword });

    const previous = getUser() || {};
    const merged = {
      token: data.token,
      email: data.email ?? previous.email,
      rol: data.rol ?? previous.rol,
      socioId: data.socioId ?? previous.socioId,
      primerLogin: data.primerLogin ?? false,
    };
    saveSession(merged);
    setUser(getUser());
    return data;
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, cambiarPassword, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
