import axios from "axios";
import type {
  ChatListItem, ImportResult, Report,
  AiInsight, PersonalityProfile, PeriodComparison, Job,
  LifeTimelineResult, PersonalityEvolutionResult, SearchResponse, TopicClusterResult, RollupResult,
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

export async function getComparison(id: string): Promise<PeriodComparison> {
  const { data } = await api.get(`/api/chats/${id}/compare`);
  return data;
}

export function reportPdfUrl(id: string, ai = false): string {
  return `${BASE_URL}/api/chats/${id}/report.pdf${ai ? "?ai=true" : ""}`;
}

// --- Асинхронный AI ---

export async function getJob<T>(jobId: string): Promise<Job<T>> {
  const { data } = await api.get(`/api/jobs/${jobId}`);
  return data;
}

export async function pollJob<T>(
  jobId: string, onTick?: (status: string) => void, intervalMs = 2000,
): Promise<T> {
  for (let i = 0; i < 180; i++) {
    const job = await getJob<T>(jobId);
    onTick?.(job.progress ? `progress:${job.progress}` : job.status);
    if (job.status === "done") return job.result as T;
    if (job.status === "failed") throw new Error(job.error || "Задача завершилась ошибкой.");
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  throw new Error("Превышено время ожидания задачи.");
}

async function startJob(id: string, kind: string, refresh = false): Promise<string> {
  const url = `/api/chats/${id}/${kind}/async${refresh ? "?refresh=true" : ""}`;
  const { data } = await api.post(url);
  return data.jobId;
}

export async function getInsightsAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<AiInsight> {
  return pollJob<AiInsight>(await startJob(id, "insights", refresh), onTick);
}
export async function getPersonalityAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<PersonalityProfile[]> {
  return pollJob<PersonalityProfile[]>(await startJob(id, "personality", refresh), onTick);
}
export async function getLifeTimelineAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<LifeTimelineResult> {
  return pollJob<LifeTimelineResult>(await startJob(id, "lifetimeline", refresh), onTick);
}
export async function getEvolutionAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<PersonalityEvolutionResult> {
  return pollJob<PersonalityEvolutionResult>(await startJob(id, "evolution", refresh), onTick);
}

export async function buildEmbeddingsAsync(id: string, onTick?: (s: string) => void): Promise<{ built: number }> {
  return pollJob<{ built: number }>(await startJob(id, "embeddings"), onTick);
}

export async function semanticSearch(id: string, q: string, limit = 10): Promise<SearchResponse> {
  const { data } = await api.get(`/api/chats/${id}/search`, { params: { q, limit } });
  return data;
}

export async function getClustersAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<TopicClusterResult> {
  return pollJob<TopicClusterResult>(await startJob(id, "clusters", refresh), onTick);
}

export async function getRollupAsync(id: string, onTick?: (s: string) => void, refresh = false): Promise<RollupResult> {
  return pollJob<RollupResult>(await startJob(id, "rollup", refresh), onTick);
}
