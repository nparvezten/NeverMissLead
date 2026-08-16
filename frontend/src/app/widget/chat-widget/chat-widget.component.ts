import {
  Component,
  Input,
  OnInit,
  signal,
  computed,
  inject,
  ElementRef,
  ViewChild,
  AfterViewChecked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatApiService } from '../services/chat-api.service';
import { ChatMessage } from '../models/message.model';

declare global {
  interface Window {
    NMLConfig?: {
      businessId?: string;
      apiBaseUrl?: string;
      brandColor?: string;
      greeting?: string;
    };
  }
}

@Component({
  selector: 'nml-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-widget.component.html',
  styleUrls: ['./chat-widget.component.css'],
})
export class ChatWidgetComponent implements OnInit, AfterViewChecked {
  private api = inject(ChatApiService);

  @Input() businessId = '';
  @Input() apiBaseUrl = 'http://localhost:5103';
  @Input() brandColor = '#6366f1';
  @Input() greeting = 'Hi there! How can I help you today?';

  // Component Signals
  isOpen = signal<boolean>(false);
  isTyping = signal<boolean>(false);
  conversationId = signal<string | null>(null);
  messages = signal<ChatMessage[]>([]);
  showContactForm = signal<boolean>(false);
  leadCaptured = signal<boolean>(false);
  isSubmittingContact = signal<boolean>(false);
  businessName = signal<string>('Support Assistant');

  // Input model state
  userInput = signal<string>('');
  contactName = signal<string>('');
  contactEmail = signal<string>('');
  contactPhone = signal<string>('');
  contactError = signal<string | null>(null);

  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;
  private shouldScrollToBottom = false;

  async ngOnInit(): Promise<void> {
    // Check global window config if inputs were not explicitly provided
    if (typeof window !== 'undefined' && window.NMLConfig) {
      if (!this.businessId && window.NMLConfig.businessId) {
        this.businessId = window.NMLConfig.businessId;
      }
      if (window.NMLConfig.apiBaseUrl) {
        this.apiBaseUrl = window.NMLConfig.apiBaseUrl;
      }
      if (window.NMLConfig.brandColor) {
        this.brandColor = window.NMLConfig.brandColor;
      }
      if (window.NMLConfig.greeting) {
        this.greeting = window.NMLConfig.greeting;
      }
    }

    // Try fetching business remote config (greeting, brandColor, name)
    if (this.businessId) {
      try {
        const config = await this.api.fetchConfig(this.apiBaseUrl, this.businessId);
        if (config.widgetGreeting) this.greeting = config.widgetGreeting;
        if (config.brandColor) this.brandColor = config.brandColor;
        if (config.businessName) this.businessName.set(config.businessName);
      } catch (err) {
        console.warn('Could not load remote widget config; using defaults.', err);
      }
    }

    // Add initial greeting message
    this.messages.set([
      {
        id: 'initial-greeting',
        role: 'assistant',
        content: this.greeting,
        timestamp: new Date(),
      },
    ]);
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  toggleWidget(): void {
    this.isOpen.update((v) => !v);
    if (this.isOpen()) {
      this.shouldScrollToBottom = true;
    }
  }

  async sendMessage(): Promise<void> {
    const text = this.userInput().trim();
    if (!text || this.isTyping()) return;

    // Add User message
    const userMsg: ChatMessage = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: text,
      timestamp: new Date(),
    };

    this.messages.update((msgs) => [...msgs, userMsg]);
    this.userInput.set('');
    this.isTyping.set(true);
    this.shouldScrollToBottom = true;

    try {
      const response = await this.api.sendMessage(this.apiBaseUrl, this.businessId, {
        conversationId: this.conversationId(),
        message: text,
      });

      this.conversationId.set(response.conversationId);

      const assistantMsg: ChatMessage = {
        id: `assistant-${Date.now()}`,
        role: 'assistant',
        content: response.assistantMessage,
        timestamp: new Date(),
        citedChunkIds: response.citedChunkIds,
        needsHuman: response.needsHuman,
      };

      this.messages.update((msgs) => [...msgs, assistantMsg]);

      // If buying intent detected and contact hasn't been captured yet, show contact form
      if (response.leadIntentDetected && !this.leadCaptured()) {
        this.showContactForm.set(true);
      }
    } catch (err) {
      console.error('Error sending message to chat API', err);
      const errorMsg: ChatMessage = {
        id: `error-${Date.now()}`,
        role: 'assistant',
        content: "Sorry, I'm having trouble connecting right now. Please try again in a moment.",
        timestamp: new Date(),
        needsHuman: true,
      };
      this.messages.update((msgs) => [...msgs, errorMsg]);
    } finally {
      this.isTyping.set(false);
      this.shouldScrollToBottom = true;
    }
  }

  async submitContact(): Promise<void> {
    const name = this.contactName().trim();
    const email = this.contactEmail().trim();
    const phone = this.contactPhone().trim();

    if (!name) {
      this.contactError.set('Please provide your name.');
      return;
    }

    if (!email && !phone) {
      this.contactError.set('Please provide either an email address or phone number.');
      return;
    }

    const currentConvId = this.conversationId();
    if (!currentConvId) {
      this.contactError.set('Active conversation session missing. Please send a message first.');
      return;
    }

    this.isSubmittingContact.set(true);
    this.contactError.set(null);

    try {
      await this.api.submitContact(this.apiBaseUrl, this.businessId, {
        conversationId: currentConvId,
        name,
        email: email || null,
        phone: phone || null,
        intentSummary: 'Captured via website widget',
      });

      this.leadCaptured.set(true);
      this.showContactForm.set(false);

      // Add confirmation message
      const confirmMsg: ChatMessage = {
        id: `lead-confirm-${Date.now()}`,
        role: 'assistant',
        content: `Thank you, ${name}! We have received your contact details and our team will get in touch with you shortly.`,
        timestamp: new Date(),
      };
      this.messages.update((msgs) => [...msgs, confirmMsg]);
    } catch (err) {
      console.error('Failed to submit contact', err);
      this.contactError.set('Failed to submit your details. Please try again.');
    } finally {
      this.isSubmittingContact.set(false);
      this.shouldScrollToBottom = true;
    }
  }

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
