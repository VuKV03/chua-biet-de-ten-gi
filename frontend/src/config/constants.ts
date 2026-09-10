export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1',
  TIMEOUT: parseInt(import.meta.env.VITE_API_TIMEOUT || '30000'),
  // .NET 10 thường dùng ProblemDetails format
  ERROR_RESPONSE_KEY: 'errors', // Not 'error' như NestJS
};

export const ENDPOINTS = {
  AUTH: '/auth',
  TASKS: '/tasks',
  USERS: '/users',
};