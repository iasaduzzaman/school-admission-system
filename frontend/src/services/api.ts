import axios from 'axios';
import type {
  ClassItem,
  CreateClassPayload,
  HoldResponse,
  HoldDetailsResponse,
  ConfirmHoldResponse,
  StudentHold,
  ApiError
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

const client = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const formatErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    const serverError = error.response?.data as ApiError | undefined;
    if (serverError?.message) {
      return serverError.message;
    }
    if (error.response?.status === 409) {
      return 'Conflict: Seat no longer available or student already applied.';
    }
    if (error.response?.status === 404) {
      return 'Resource not found.';
    }
    if (error.response?.status === 403) {
      return 'Forbidden: You can only manage your own reservation.';
    }
    if (error.response?.status === 400) {
      return 'Bad request: Please verify your input details.';
    }
    if (error.code === 'ERR_NETWORK') {
      return `Unable to connect to server at ${API_BASE_URL}. Ensure backend is running.`;
    }
  }
  return error instanceof Error ? error.message : 'An unexpected error occurred.';
};

export const api = {
  // Classes
  async getClasses(): Promise<ClassItem[]> {
    const res = await client.get<ClassItem[]>('/classes');
    return res.data;
  },

  async getClass(classId: string): Promise<ClassItem> {
    const res = await client.get<ClassItem>(`/classes/${classId}`);
    return res.data;
  },

  async createClass(payload: CreateClassPayload): Promise<ClassItem> {
    const res = await client.post<ClassItem>('/classes', payload);
    return res.data;
  },

  // Holds
  async createHold(classId: string, studentId: string): Promise<HoldResponse> {
    const res = await client.post<HoldResponse>(
      `/classes/${classId}/holds`,
      {},
      { headers: { 'X-Student-Id': studentId } }
    );
    return res.data;
  },

  async getHold(holdId: string): Promise<HoldDetailsResponse> {
    const res = await client.get<HoldDetailsResponse>(`/holds/${holdId}`);
    return res.data;
  },

  async confirmHold(holdId: string, studentId: string): Promise<ConfirmHoldResponse> {
    const res = await client.post<ConfirmHoldResponse>(
      `/holds/${holdId}/confirm`,
      {},
      { headers: { 'X-Student-Id': studentId } }
    );
    return res.data;
  },

  async cancelHold(holdId: string, studentId: string): Promise<void> {
    await client.delete(`/holds/${holdId}`, {
      headers: { 'X-Student-Id': studentId },
    });
  },

  // Student applications
  async getStudentHolds(studentId: string): Promise<StudentHold[]> {
    const res = await client.get<StudentHold[]>('/me/holds', {
      headers: { 'X-Student-Id': studentId },
    });
    return res.data;
  },
};
