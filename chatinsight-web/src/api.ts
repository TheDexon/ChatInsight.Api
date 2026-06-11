import axios from "axios";
import type {
  ChatListItem, ImportResult, Report,
  AiInsight, PersonalityProfile, PeriodComparison,
} from "./types";

export const BASE_URL = "http://localhost:5201";

const api = axios.create({ baseURL: BASE_URL });

export async function importTelegram(file: File): Promise<ImportResult> {
  const form = new FormData();
  form.append("file", file);
  const { data } = await api.post("/api/import/telegram", form);
  return data;
}

export async function listChats(): Promise<ChatListItem[]> {
  const { data } = await api.get("/api/chats");
  return data;
}

export async function getReport(id: string): Promise<Report> {
  const { data } = await api.get(`/api/chats/${id}/report`);
  return data;
}

export async function getInsights(id: string, refresh = false): Promise<AiInsight> {
  const { data } = await api.get(`/api/chats/${id}/insights`, { params: { refresh } });
  return data;
}

export async function getPersonality(id: string, refresh = false): Promise<PersonalityProfile[]> {
  const { data } = await api.get(`/api/chats/${id}/personality`, { params: { refresh } });
  return data;
}

export async function getComparison(id: string): Promise<PeriodComparison> {
  const { data } = await api.get(`/api/chats/${id}/compare`);
  return data;
}

export function reportPdfUrl(id: string, ai = false): string {
  return `${BASE_URL}/api/chats/${id}/report.pdf${ai ? "?ai=true" : ""}`;
}
