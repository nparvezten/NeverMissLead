import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Lead,
  ConversationSummary,
  ConversationDetail,
  UnansweredQuestion,
  FollowUpTask
} from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5103/api/v1/dashboard';

  getLeads(): Observable<Lead[]> {
    return this.http.get<Lead[]>(`${this.baseUrl}/leads`, { withCredentials: true });
  }

  updateLeadStatus(leadId: string, status: string): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(
      `${this.baseUrl}/leads/${leadId}/status`,
      { status },
      { withCredentials: true }
    );
  }

  getConversations(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>(`${this.baseUrl}/conversations`, { withCredentials: true });
  }

  getConversation(id: string): Observable<ConversationDetail> {
    return this.http.get<ConversationDetail>(`${this.baseUrl}/conversations/${id}`, { withCredentials: true });
  }

  getUnansweredQuestions(): Observable<UnansweredQuestion[]> {
    return this.http.get<UnansweredQuestion[]>(`${this.baseUrl}/unanswered-questions`, { withCredentials: true });
  }

  getFollowUpTasks(): Observable<FollowUpTask[]> {
    return this.http.get<FollowUpTask[]>(`${this.baseUrl}/follow-ups`, { withCredentials: true });
  }
}
