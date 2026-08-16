export type MessageRole = 'user' | 'assistant' | 'system';

export interface ChatMessage {
  id: string;
  role: MessageRole;
  content: string;
  timestamp: Date;
  citedChunkIds?: string[];
  needsHuman?: boolean;
}

export interface SendMessageRequest {
  conversationId?: string | null;
  message: string;
  visitorRef?: string;
}

export interface SendMessageResponse {
  conversationId: string;
  assistantMessage: string;
  needsHuman: boolean;
  leadIntentDetected: boolean;
  citedChunkIds: string[];
}

export interface WidgetConfig {
  businessId: string;
  businessName?: string;
  widgetGreeting?: string;
  brandColor?: string;
  apiBaseUrl?: string;
}
