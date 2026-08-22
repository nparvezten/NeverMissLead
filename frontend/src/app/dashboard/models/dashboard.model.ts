export interface AuthUser {
  businessId: string;
  businessName: string;
  email: string;
}

export interface Lead {
  id: string;
  conversationId: string;
  businessId: string;
  name: string | null;
  phone: string | null;
  email: string | null;
  intentSummary: string | null;
  qualificationScore: number;
  status: 'New' | 'Contacted' | 'Converted' | 'Lost';
  createdAt: string;
}

export interface ConversationSummary {
  id: string;
  visitorRef: string;
  startedAt: string;
  status: string;
  needsHuman: boolean;
  handoffReason: string;
  messageCount: number;
  lastMessage: string | null;
  lastMessageAt: string | null;
}

export interface MessageDetail {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  citedChunkIds: string[];
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  businessId: string;
  visitorRef: string;
  startedAt: string;
  status: string;
  needsHuman: boolean;
  handoffReason: string;
  messages: MessageDetail[];
}

export interface UnansweredQuestion {
  conversationId: string;
  visitorRef: string;
  question: string;
  handoffReason: string;
  askedAt: string;
}

export interface FollowUpTask {
  id: string;
  businessId: string;
  leadId: string | null;
  conversationId: string | null;
  scheduledFor: string;
  channel: string;
  messageDraft: string;
  status: 'Pending' | 'Sent' | 'Skipped';
  sentAt: string | null;
}
