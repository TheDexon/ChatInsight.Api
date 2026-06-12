export interface ChatListItem {
  id: string; name: string; type: string; messageCount: number; importedAt: string;
}
export interface ImportResult {
  chatId: string; chatName: string; chatType: string;
  messagesCount: number; newMessages: number; isNewChat: boolean;
  firstMessageDate: string; lastMessageDate: string; participants: string[];
}
export interface Relationship {
  activityBalance: number; initiativeBalance: number; responseBalance: number;
  dominantParticipant: string; secondaryParticipant: string; summary: string;
}
export interface Report {
  statistics: {
    totalMessages: number; averageMessageLength: number; mostActiveHour: number;
    messagesByAuthor: Record<string, number>;
    messagesByHour: Record<string, number>;
    messagesByDay: Record<string, number>;
  };
  emotion: {
    positiveMessages: number; negativeMessages: number;
    profanityMessages: number; toxicityScore: number;
  };
  topics: { topics: { name: string; count: number }[] };
  timeline: { title: string; description: string; date: string }[];
  relationship: Relationship | null;
  summary: string;
}
export interface AiInsight {
  summary: string; emotionalTone: string;
  topics: string[]; dynamics: string[]; model: string;
}
export interface PersonalityProfile {
  participant: string; summary: string; communicationStyle: string;
  traits: string[]; model: string;
}
export interface PeriodSummary {
  from: string; to: string; messages: number; avgMessageLength: number;
  positiveMessages: number; negativeMessages: number; toxicityScore: number;
  avgResponseMinutes: number; topTopics: string[];
}
export interface PeriodComparison {
  first: PeriodSummary; second: PeriodSummary;
  messagesDelta: number; toxicityDelta: number; responseMinutesDelta: number;
  newTopics: string[]; fadedTopics: string[]; summary: string;
}
export interface LifeTimelineEvent {
  period: string; title: string; description: string; category: string;
}
export interface LifeTimelineResult {
  events: LifeTimelineEvent[]; summary: string; model: string;
}
export interface EvolutionEntry {
  participant: string;
  before: PersonalityProfile;
  after: PersonalityProfile;
  change: string;
}
export interface PersonalityEvolutionResult {
  entries: EvolutionEntry[];
  summary: string;
  model: string;
}
export interface SearchHit {
  date: string; author: string | null; text: string; score: number;
}
export interface SearchResponse {
  embeddingsReady: boolean; hits: SearchHit[];
}
export interface TopicCluster {
  label: string; size: number; share: number; samples: string[];
}
export interface TopicClusterResult {
  clusters: TopicCluster[]; summary: string; model: string;
}
export interface Job<T> {
  id: string; type: string;
  status: "pending" | "running" | "done" | "failed";
  result: T | null; error: string | null;
}
