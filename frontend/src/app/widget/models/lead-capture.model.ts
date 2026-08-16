export interface ContactCaptureRequest {
  conversationId: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  intentSummary?: string | null;
}

export interface ContactCaptureResponse {
  leadId: string;
}
