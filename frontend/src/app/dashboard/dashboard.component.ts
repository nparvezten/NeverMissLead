import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from './services/auth.service';
import { DashboardService } from './services/dashboard.service';
import {
  Lead,
  ConversationSummary,
  ConversationDetail,
  UnansweredQuestion,
  FollowUpTask
} from './models/dashboard.model';

type DashboardTab = 'leads' | 'conversations' | 'unanswered' | 'followups';

@Component({
  selector: 'nml-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  authService = inject(AuthService);
  private dashboardService = inject(DashboardService);

  activeTab = signal<DashboardTab>('leads');
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  leads = signal<Lead[]>([]);
  conversations = signal<ConversationSummary[]>([]);
  selectedConversation = signal<ConversationDetail | null>(null);
  isLoadingConversation = signal<boolean>(false);
  unansweredQuestions = signal<UnansweredQuestion[]>([]);
  followUpTasks = signal<FollowUpTask[]>([]);

  // Computed stats
  totalLeads = computed(() => this.leads().length);
  highIntentLeads = computed(() => this.leads().filter(l => l.qualificationScore >= 60).length);
  unansweredCount = computed(() => this.unansweredQuestions().length);
  pendingFollowUps = computed(() => this.followUpTasks().filter(t => t.status === 'Pending').length);

  ngOnInit() {
    this.loadAllData();
  }

  loadAllData() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dashboardService.getLeads().subscribe({
      next: (data) => this.leads.set(data),
      error: (err) => this.handleError(err)
    });

    this.dashboardService.getConversations().subscribe({
      next: (data) => {
        this.conversations.set(data);
        if (data.length > 0 && !this.selectedConversation()) {
          this.selectConversation(data[0].id);
        }
      },
      error: (err) => this.handleError(err)
    });

    this.dashboardService.getUnansweredQuestions().subscribe({
      next: (data) => this.unansweredQuestions.set(data),
      error: (err) => this.handleError(err)
    });

    this.dashboardService.getFollowUpTasks().subscribe({
      next: (data) => {
        this.followUpTasks.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.handleError(err);
        this.isLoading.set(false);
      }
    });
  }

  setTab(tab: DashboardTab) {
    this.activeTab.set(tab);
  }

  selectConversation(id: string) {
    this.isLoadingConversation.set(true);
    this.dashboardService.getConversation(id).subscribe({
      next: (data) => {
        this.selectedConversation.set(data);
        this.isLoadingConversation.set(false);
      },
      error: (err) => {
        this.handleError(err);
        this.isLoadingConversation.set(false);
      }
    });
  }

  onUpdateLeadStatus(lead: Lead, newStatus: string) {
    this.dashboardService.updateLeadStatus(lead.id, newStatus).subscribe({
      next: () => {
        this.leads.update((current) =>
          current.map((l) => (l.id === lead.id ? { ...l, status: newStatus as any } : l))
        );
      },
      error: (err) => this.handleError(err)
    });
  }

  logout() {
    this.authService.logout().subscribe();
  }

  private handleError(err: any) {
    const detail = err.error?.detail || err.error?.title || 'An error occurred while loading dashboard data.';
    this.errorMessage.set(detail);
  }
}
